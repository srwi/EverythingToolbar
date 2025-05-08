using System;
using System.Collections.Generic;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T>
    {
        int FetchCount(int pageSize = 0);

        void FetchCountAsync(int pageSize = 0, Action<int> callback = null);

        IList<T> FetchRange(int startIndex, int pageSize);

        void FetchRangeAsync(int startIndex, int pageSize, Action<IList<T>> callback = null);
    }
}