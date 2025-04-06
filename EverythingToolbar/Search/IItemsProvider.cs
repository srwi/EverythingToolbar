using System.Collections.Generic;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T>
    {
        int FetchCount();

        IList<T> FetchRange(int startIndex, int count);
    }
}
