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

        int QueryCountSync(SearchQuery query, int pageSize);

        Task<IList<SearchResult>> QueryRangeAsync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        );

        IList<SearchResult> QueryRangeSync(SearchQuery query, int startIndex, int pageSize);

        Version GetEverythingVersion();

        void SetInstanceName(string name);

        void IncrementRunCount(string path);

        bool GetIsFastSort(SortBy sortBy, bool descending);
    }
}
