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

        private bool? _usingPipeClient;

        private IEverythingClient Active
        {
            get
            {
                var usePipeClient = pipeClient.TryConnect();

                if (usePipeClient != _usingPipeClient)
                {
                    _usingPipeClient = usePipeClient;
                    Logger.Info(
                        usePipeClient
                            ? "Using the Everything 1.5 SDK3 pipe client."
                            : "Using the Everything 1.4 SDK2 IPC client."
                    );
                }

                return usePipeClient ? pipeClient : ipcClient;
            }
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
        }

        public void IncrementRunCount(string path) => Active.IncrementRunCount(path);

        public bool GetIsFastSort(SortBy sortBy, bool descending) => Active.GetIsFastSort(sortBy, descending);
    }
}
