using System.Collections.Generic;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T>
    {
        int FetchCount(int pageSize = 0);

        IList<T> FetchRange(int startIndex, int pageSize);
    }
}