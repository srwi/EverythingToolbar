using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingToolbar.Core.Search
{
    public interface IItemsProvider<T> : INotifyPropertyChanged
    {
        bool IsBusy { get; }

        Task<int> FetchCount(int pageSize, bool isAsync, CancellationToken cancellationToken);

        Task<IList<T>> FetchRange(int startIndex, int pageSize, bool isAsync, CancellationToken cancellationToken);
    }
}
