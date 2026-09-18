using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Core.Search;

internal sealed class FakeClient : IEverythingClient
{
    public async Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken token)
    {
        await Task.Delay(35, token);
        return 1000000;
    }

    public int QueryCountSync(SearchQuery query, int pageSize, CancellationToken token) => 1000000;

    public async Task<IList<SearchResult>> QueryRangeAsync(
        SearchQuery query,
        int start,
        int size,
        CancellationToken token
    )
    {
        await Task.Yield();
        return QueryRangeSync(query, start, size, token);
    }

    public IList<SearchResult> QueryRangeSync(SearchQuery query, int start, int size, CancellationToken token)
    {
        var result = new List<SearchResult>();
        for (int i = 0; i < size; i++)
            result.Add(
                new SearchResult(
                    "C:\\",
                    "fixture " + (start + i),
                    "C:\\fixture-" + (start + i),
                    false,
                    0,
                    default(FILETIME)
                )
            );
        return result;
    }

    public bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> result)
    {
        result = QueryRangeSync(query, 0, 256, default);
        return true;
    }

    public Version GetEverythingVersion() => new(1, 4, 1);

    public void SetInstanceName(string name) { }

    public void IncrementRunCount(string path) { }

    public bool GetIsFastSort(SortBy by, bool descending) => true;
}
