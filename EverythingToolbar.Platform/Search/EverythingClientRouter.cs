using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;
using NLog;

namespace EverythingToolbar.Platform.Search
{
    public sealed class EverythingClientRouter(EverythingPipeClient pipeClient, EverythingIpcClient ipcClient)
        : IEverythingClient
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);

        private volatile IEverythingClient? _active;
        private volatile bool _resolveAttempted;
        private volatile bool _pipeClientUnavailable;
        private int _retryRunning;

        private IEverythingClient Active
        {
            get
            {
                var active = _active;
                if (active != null)
                    return active;

                if (!_resolveAttempted)
                {
                    _resolveAttempted = true;

                    active = TryResolve();
                    if (active != null)
                        return active;
                }

                RetryUntilResolved();
                return ipcClient;
            }
        }

        private IEverythingClient? TryResolve()
        {
            IEverythingClient? resolved = null;

            if (TryConnectPipeClient())
                resolved = pipeClient;
            else if (ipcClient.GetEverythingVersion().Major > 0)
                resolved = ipcClient;

            if (resolved == null)
                return null;

            Logger.Info(
                ReferenceEquals(resolved, pipeClient)
                    ? "Using the Everything 1.5 SDK3 pipe client."
                    : "Using the Everything 1.4 SDK2 IPC client."
            );

            _active = resolved;
            return resolved;
        }

        private bool TryConnectPipeClient()
        {
            if (_pipeClientUnavailable)
                return false;

            try
            {
                return pipeClient.TryConnect();
            }
            catch (Exception e)
            {
                _pipeClientUnavailable = true;
                Logger.Error(e, "The Everything 1.5 SDK3 client could not be loaded.");
                return false;
            }
        }

        private void RetryUntilResolved()
        {
            if (Interlocked.Exchange(ref _retryRunning, 1) == 1)
                return;

            Task.Run(async () =>
            {
                try
                {
                    while (_active == null)
                    {
                        await Task.Delay(RetryInterval).ConfigureAwait(false);
                        TryResolve();
                    }
                }
                catch (Exception e)
                {
                    Logger.Debug(e, "Gave up looking for a running Everything instance.");
                }
                finally
                {
                    Volatile.Write(ref _retryRunning, 0);
                }
            });
        }

        public Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken cancellationToken) =>
            Active.QueryCountAsync(query, pageSize, cancellationToken);

        public int QueryCountSync(SearchQuery query, int pageSize, CancellationToken cancellationToken) =>
            Active.QueryCountSync(query, pageSize, cancellationToken);

        public Task<IList<SearchResult>> QueryRangeAsync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        ) => Active.QueryRangeAsync(query, startIndex, pageSize, cancellationToken);

        public IList<SearchResult> QueryRangeSync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        ) => Active.QueryRangeSync(query, startIndex, pageSize, cancellationToken);

        public bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> results) =>
            Active.TryReadCachedFirstPage(query, out results);

        public Version GetEverythingVersion() => Active.GetEverythingVersion();

        public void SetInstanceName(string name)
        {
            pipeClient.SetInstanceName(name);
            ipcClient.SetInstanceName(name);

            _active = null;
            _resolveAttempted = false;
        }

        public void IncrementRunCount(string path) => Active.IncrementRunCount(path);

        public bool GetIsFastSort(SortBy sortBy, bool descending) => Active.GetIsFastSort(sortBy, descending);
    }
}
