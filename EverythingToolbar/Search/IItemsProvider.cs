using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace EverythingToolbar.Search
{
    public interface IItemsProvider<T> : INotifyPropertyChanged
    {
        bool IsBusy { get; }

        Task<int> FetchCount(int pageSize, bool isAsync);

        Task<IList<T>> FetchRange(int startIndex, int pageSize, bool isAsync);
    }
}