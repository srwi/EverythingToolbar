using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Core.Search;

namespace EverythingToolbar.App.Search
{
    public sealed class EverythingItemsProvider : IItemsProvider<SearchResult>
    {
        private readonly IEverythingClient _client;
        private readonly SearchQuery _query;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public EverythingItemsProvider(IEverythingClient client, SearchQuery query)
        {
            _client = client;
            _query = query;
        }

        public Task<int> FetchCount(int pageSize, bool isAsync, CancellationToken cancellationToken)
        {
            // The synchronous paths ignore the token: a blocking SDK query cannot be interrupted
            if (!isAsync)
                return Task.FromResult(_client.QueryCountSync(_query, pageSize));

            return TrackBusyState(_client.QueryCountAsync(_query, pageSize, cancellationToken));
        }

        public async Task<IList<SearchResult>> FetchRange(
            int startIndex,
            int pageSize,
            bool isAsync,
            CancellationToken cancellationToken
        )
        {
            IList<SearchResult> data;
            if (!isAsync)
                data = _client.QueryRangeSync(_query, startIndex, pageSize);
            else
                data = await TrackBusyState(_client.QueryRangeAsync(_query, startIndex, pageSize, cancellationToken));

            return data;
        }

        private async Task<T> TrackBusyState<T>(Task<T> task)
        {
            if (task.IsCompleted)
                return await task;

            IsBusy = true;
            try
            {
                return await task;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
