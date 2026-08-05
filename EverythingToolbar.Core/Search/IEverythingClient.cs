using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Core.Search
{
    public interface IEverythingClient
    {
        Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken cancellationToken);

        // Canceling means the query was superseded: it returns 0 rather than throwing, and callers
        // are expected to discard the result once they notice the token is canceled.
        int QueryCountSync(SearchQuery query, int pageSize, CancellationToken cancellationToken);

        Task<IList<SearchResult>> QueryRangeAsync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        );

        IList<SearchResult> QueryRangeSync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        );

        bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> results);

        Version GetEverythingVersion();

        void SetInstanceName(string name);

        void IncrementRunCount(string path);

        bool GetIsFastSort(SortBy sortBy, bool descending);
    }
}
