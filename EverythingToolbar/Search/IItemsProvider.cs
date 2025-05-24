using System.Collections.Generic;
using System.Threading.Tasks;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T>
    {
        Task<int> FetchCount(int pageSize, bool isAsync);

        Task<IList<T>> FetchRange(int startIndex, int pageSize, bool isAsync);
    }
}