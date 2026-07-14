using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingToolbar.Search
{
    public sealed class VirtualizingCollection<T>
        : IList<T>,
            IList,
            INotifyCollectionChanged,
            INotifyPropertyChanged,
            IDisposable
    {
        public VirtualizingCollection(
            IItemsProvider<T> itemsProvider,
            int pageSize,
            SynchronizationContext currentSynchronizationContext
        )
        {
            _taskScheduler = new SynchronizationContextTaskScheduler(currentSynchronizationContext);

            PageSize = pageSize;

            ItemsProvider = itemsProvider;
            ItemsProvider.PropertyChanged += OnItemsProviderPropertyChanged;

            LoadCount();
        }

        private readonly TaskScheduler _taskScheduler;

        // Canceled when the provider is replaced or the collection disposed, so abandoned fetches can't deliver stale results.
        private CancellationTokenSource _cancellationTokenSource = new();

        private int PageSize { get; }

        private int _count;
        public int Count
        {
            get => _count;
            private set
            {
                _count = value;

                // Drop placeholders beyond the new count; a page load will never replace them.
                if (_displayedItems.Count > 0)
                {
                    var staleIndices = _displayedItems.Keys.Where(index => index >= _count).ToList();
                    foreach (var index in staleIndices)
                        _displayedItems.Remove(index);
                }

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                OnPropertyChanged();
            }
        }

        private bool _isAsync = true;
        public bool IsAsync
        {
            get => _isAsync;
            set
            {
                _isAsync = value;
                OnPropertyChanged();
            }
        }

        private IItemsProvider<T> ItemsProvider { get; set; }

        public bool IsBusy => ItemsProvider.IsBusy;

        public void UpdateProvider(IItemsProvider<T> newProvider)
        {
            if (ItemsProvider == newProvider)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _pages = new Dictionary<int, List<T>?>();
            _pageAccessOrder.Clear();
            _pageAccessNodes.Clear();


            ItemsProvider.PropertyChanged -= OnItemsProviderPropertyChanged;
            ItemsProvider = newProvider;
            ItemsProvider.PropertyChanged += OnItemsProviderPropertyChanged;

            LoadCount();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        private void OnItemsProviderPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsBusy))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void LoadCount()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            if (IsAsync)
            {
                ItemsProvider
                    .FetchCount(PageSize, isAsync: true, cancellationToken)
                    .ContinueWith(
                        task =>
                        {
                            // A canceled token means this fetch belongs to an abandoned search.
                            if (cancellationToken.IsCancellationRequested || task.IsCanceled)
                                return;

                            Count = task.Result;
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.None,
                        _taskScheduler
                    );
            }
            else
            {
                Count = ItemsProvider
                    .FetchCount(PageSize, isAsync: false, cancellationToken)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private List<T> LoadPage(int index)
        {
            var items = ItemsProvider
                .FetchRange(index * PageSize, PageSize, isAsync: false, _cancellationTokenSource.Token)
                .GetAwaiter()
                .GetResult();
            var page = new List<T>(items);
            return page;
        }

        private void LoadPageAsync(int index)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            ItemsProvider
                .FetchRange(index * PageSize, PageSize, isAsync: true, cancellationToken)
                .ContinueWith(
                    task =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        if (task.IsCanceled)
                        {
                            _pages.Remove(index); // Page needs to be loaded again in the future
                            RemovePageTracking(index);
                            return;
                        }

                        List<T>? newItems = task.Result as List<T>;
                        _pages[index] = newItems;
                        TouchPage(index);
                        TrimPages();

                        try
                        {
                            for (int i = 0; i < newItems?.Count; i++)
                            {
                                var itemIndex = index * PageSize + i;

                                if (_displayedItems.TryGetValue(itemIndex, out var oldItem))
                                {
                                    // Keep in sync so a later Replace reports the correct oldItem.
                                    _displayedItems[itemIndex] = newItems[i];

                                    OnCollectionChanged(
                                        new NotifyCollectionChangedEventArgs(
                                            NotifyCollectionChangedAction.Replace,
                                            newItems[i],
                                            oldItem,
                                            itemIndex
                                        )
                                    );
                                }
                            }
                        }
                        catch (Exception)
                        {
                            OnCollectionChanged(
                                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
                            );
                        }
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    _taskScheduler
                );
        }

        public T this[int index]
        {
            get
            {
                var item = GetItemAtIndex(index);

                _displayedItems[index] = item;
                return item;
            }
            set => throw new NotSupportedException();
        }

        private T GetItemAtIndex(int index)
        {
            var pageIndex = index / PageSize;
            var pageOffset = index % PageSize;

            if (_pages.TryGetValue(pageIndex, out var page))
            {
                TouchPage(pageIndex);
                if (page != null && pageOffset < page.Count)
                {
                    return page[pageOffset];
                }

                if (_displayedItems.TryGetValue(index, out var displayedItem))
                {
                    return displayedItem;
                }

                return default!;
            }

            if (IsAsync)
            {
                _pages[pageIndex] = null; // Mark page as loading
                TouchPage(pageIndex);
                TrimPages();

                LoadPageAsync(pageIndex);

                if (_displayedItems.TryGetValue(index, out var displayedItem))
                    return displayedItem;

                return default!;
            }
            else
            {
                var loadedPage = LoadPage(pageIndex);
                _pages[pageIndex] = loadedPage;
                TouchPage(pageIndex);
                TrimPages();
                if (pageOffset < loadedPage.Count)
                {
                    return loadedPage[pageOffset];
                }

                return default!;
            }
        }

        private void TouchPage(int pageIndex)
        {
            if (_pageAccessNodes.TryGetValue(pageIndex, out var node))
            {
                _pageAccessOrder.Remove(node);
                _pageAccessOrder.AddFirst(node);
            }
            else
            {
                _pageAccessNodes[pageIndex] = _pageAccessOrder.AddFirst(pageIndex);
            }
        }

        private void RemovePageTracking(int pageIndex)
        {
            if (_pageAccessNodes.TryGetValue(pageIndex, out var node))
            {
                _pageAccessOrder.Remove(node);
                _pageAccessNodes.Remove(pageIndex);
            }
        }

        private void TrimPages()
        {
            while (_pages.Count > MaxResidentPages && _pageAccessOrder.Last != null)
            {
                var lruPageIndex = _pageAccessOrder.Last.Value;
                _pageAccessOrder.RemoveLast();
                _pageAccessNodes.Remove(lruPageIndex);
                _pages.Remove(lruPageIndex);

                // Drop placeholder references for the evicted page so they can be collected too
                var firstItemIndex = lruPageIndex * PageSize;
                for (var offset = 0; offset < PageSize; offset++)
                    _displayedItems.Remove(firstItemIndex + offset);
            }
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object? value)
        {
            return Contains((T)value!);
        }

        public bool Contains(T item)
        {
            return false;
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        int IList.IndexOf(object? value)
        {
            return IndexOf((T)value!);
        }

        public int IndexOf(T item)
        {
            return 0;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList.Insert(int index, object? value)
        {
            Insert(index, (T)value!);
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public bool IsReadOnly => true;

        public bool IsFixedSize => false;

        private Dictionary<int, List<T>?> _pages = new();
        private readonly Dictionary<int, T> _displayedItems = new();

        private const int MaxResidentPages = 40;
        private readonly LinkedList<int> _pageAccessOrder = new(); // Most-recently-used first, least-recently-used last
        private readonly Dictionary<int, LinkedListNode<int>> _pageAccessNodes = new();
    }
}