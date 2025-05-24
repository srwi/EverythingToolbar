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
    public sealed class VirtualizingCollection<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;

            LoadCount();
        }

        private int _providerVersion;

        private int PageSize { get; }

        private int _count;
        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
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

        public void UpdateProvider(IItemsProvider<T> newProvider)
        {
            if (ItemsProvider == newProvider)
                return;

            _pages = new Dictionary<int, List<T>?>();

            ItemsProvider = newProvider;
            _providerVersion++;

            LoadCount();
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
            var currentProviderVersion = _providerVersion;

            if (IsAsync)
            {
                ItemsProvider.FetchCount(PageSize, isAsync: true).ContinueWith(task =>
                {
                    if (currentProviderVersion != _providerVersion || task.IsCanceled)
                        return;

                    Count = task.Result;
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                Count = ItemsProvider.FetchCount(PageSize, isAsync: false).GetAwaiter().GetResult();
            }
        }

        private List<T> LoadPage(int index)
        {
            var items = ItemsProvider.FetchRange(index * PageSize, PageSize, isAsync: false).GetAwaiter().GetResult();
            var page = new List<T>(items);
            return page;
        }

        private void LoadPageAsync(int index)
        {
            var currentProviderVersion = _providerVersion;

            ItemsProvider.FetchRange(index * PageSize, PageSize, isAsync: true).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    _pages.Remove(index);  // Page needs to be loaded again in the future
                    return;
                }

                if (currentProviderVersion != _providerVersion)
                    return;

                IList<T> newItems = task.Result;
                _pages[index] = newItems.ToList();

                try
                {
                    for (int i = 0; i < newItems.Count; i++)
                    {
                        var itemIndex = index * PageSize + i;

                        if (_displayedItems.TryGetValue(itemIndex, out var oldItem))
                        {
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems[i], oldItem, itemIndex));
                        }
                    }
                }
                catch (Exception)
                {
                    // For various internal reasons, the collection changed event can throw exceptions.
                    // Whenever this happens, we reset the collection to recover from the error.
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
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
                if (page != null && pageOffset < page.Count)
                {
                    return page[pageOffset];
                }

                // Page is null (is currently loading)
                if (_displayedItems.TryGetValue(index, out var displayedItem))
                {
                    return displayedItem;
                }

                return default!;
            }

            if (IsAsync)
            {
                _pages[pageIndex] = null;  // Mark page as loading

                LoadPageAsync(pageIndex);

                // Return the old item and let the async operation update it later
                if (_displayedItems.TryGetValue(index, out var displayedItem))
                    return displayedItem;

                return default!;
            }
            else
            {
                var loadedPage = LoadPage(pageIndex);
                _pages[pageIndex] = loadedPage;
                if (pageOffset < loadedPage.Count)
                {
                    return loadedPage[pageOffset];
                }

                return default!;
            }
        }

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            // We return an empty enumerator to prevent WPF internals from iterating through the collection.
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
            // We want to prevent WPF internals from searching for an item by iterating through the collection.
            // Returning -1 would trigger a full collection scan, but returning 0 is sufficient to prevent that.
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
    }
}
