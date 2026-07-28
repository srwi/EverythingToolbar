using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Everything can be started, stopped or upgraded while we run, so the choice has to be
        // revisited — but off the caller's thread and rarely, never once per keystroke.
        private static readonly TimeSpan RecheckInterval = TimeSpan.FromSeconds(5);

        private volatile IEverythingClient? _active;
        private long _lastCheckTimestamp;
        private int _recheckRunning;

        private IEverythingClient Active
        {
            get
            {
                // Deciding means connecting, and connecting takes the pipe client's lock — which a
                // running query holds for its entire duration. Doing that on every call put the
                // caller in line behind every search still in flight.
                var active = _active;
                if (active == null)
                    return Resolve();

                RecheckInBackground();
                return active;
            }
        }

        private IEverythingClient Resolve()
        {
            var usePipeClient = pipeClient.TryConnect();
            IEverythingClient active = usePipeClient ? pipeClient : ipcClient;

            if (!ReferenceEquals(_active, active))
            {
                Logger.Info(
                    usePipeClient
                        ? "Using the Everything 1.5 SDK3 pipe client."
                        : "Using the Everything 1.4 SDK2 IPC client."
                );
            }

            _active = active;
            Volatile.Write(ref _lastCheckTimestamp, Stopwatch.GetTimestamp());
            return active;
        }

        private void RecheckInBackground()
        {
            if (Stopwatch.GetElapsedTime(Volatile.Read(ref _lastCheckTimestamp)) < RecheckInterval)
                return;

            if (Interlocked.Exchange(ref _recheckRunning, 1) == 1)
                return;

            Task.Run(() =>
            {
                try
                {
                    Resolve();
                }
                catch (Exception e)
                {
                    Logger.Debug(e, "Failed to re-check which Everything client to use.");
                }
                finally
                {
                    Volatile.Write(ref _recheckRunning, 0);
                }
            });
        }

        public Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken cancellationToken) =>
            Active.QueryCountAsync(query, pageSize, cancellationToken);

        public int QueryCountSync(SearchQuery query, int pageSize) => Active.QueryCountSync(query, pageSize);

        public Task<IList<SearchResult>> QueryRangeAsync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        ) => Active.QueryRangeAsync(query, startIndex, pageSize, cancellationToken);

        public IList<SearchResult> QueryRangeSync(SearchQuery query, int startIndex, int pageSize) =>
            Active.QueryRangeSync(query, startIndex, pageSize);

        public bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> results) =>
            Active.TryReadCachedFirstPage(query, out results);

        public Version GetEverythingVersion() => Active.GetEverythingVersion();

        public void SetInstanceName(string name)
        {
            pipeClient.SetInstanceName(name);
            ipcClient.SetInstanceName(name);

            // The instance decides which pipe to talk to, so the choice has to be made again.
            _active = null;
        }

        public void IncrementRunCount(string path) => Active.IncrementRunCount(path);

        public bool GetIsFastSort(SortBy sortBy, bool descending) => Active.GetIsFastSort(sortBy, descending);
    }
}
