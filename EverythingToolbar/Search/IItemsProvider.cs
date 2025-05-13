using System.Collections.Generic;
using System.Threading.Tasks;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T>
    {
        Task<int> FetchCount(int pageSize = 0, bool isAsync = true);

        Task<IList<T>> FetchRange(int startIndex, int pageSize, bool isAsync = true);
    }
}