using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EverythingToolbar.Search
{
    public enum Status
    {
        NoData,
        HasValidData,
        HasOutdatedData
    }

    public class Page<T>
    {
        private IList<T> _items;
        public IList<T> Items
        {
            get => _items;
            set
            {
                _items = value;
                Status = Status.HasValidData;
            }
        }
        public Status Status { get; private set; } = Status.NoData;

        public void Invalidate()
        {
            if (Status == Status.HasValidData)
            {
                Status = Status.HasOutdatedData;
            }
        }

        public void MarkAsValid()
        {
            if (Status == Status.HasOutdatedData)
            {
                Status = Status.HasValidData;
            }
        }
    }

    public sealed class VirtualizingCollection<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private int _providerVersion;
        private int _currentVersion;

        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
            SynchronizationContext = SynchronizationContext.Current;
        }

        private IItemsProvider<T> ItemsProvider { get; set; }

        public void UpdateProvider(IItemsProvider<T> newProvider)
        {
            if (ItemsProvider == newProvider)
                return;

            ItemsProvider = newProvider;
            _providerVersion++;

            foreach (var page in _pages.Values)
            {
                page.Invalidate();
            }
            LoadCount();
        }

        private int PageSize { get; }

        private int _count = -1;
        public int Count
        {
            get
            {
                if (_count == -1)
                {
                    LoadCount();
                }
                return _count;
            }
            private set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged();
                }
            }
        }

        private SynchronizationContext SynchronizationContext { get; }

        private bool _isAsync = true;
        public bool IsAsync
        {
            get => _isAsync;
            set
            {
                if (_isAsync != value)
                {
                    _isAsync = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        private void FireCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void LoadCount()
        {
            _currentVersion = _providerVersion;
            
            if (IsAsync)
            {
                Count = 0;
                ItemsProvider.FetchCount(PageSize, isAsync: true).ContinueWith(task =>
                {
                    if (_currentVersion != _providerVersion)
                        return;

                    Count = task.Result;
                    FireCollectionReset();
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                Count = ItemsProvider.FetchCount(PageSize, isAsync: false).GetAwaiter().GetResult();
                FireCollectionReset();
            }
        }

        private void LoadPage(int index)
        {
            _currentVersion = _providerVersion;
            
            if (IsAsync)
            {
                ItemsProvider.FetchRange(index * PageSize, PageSize, isAsync: true).ContinueWith(task =>
                {
                    if (_currentVersion != _providerVersion)
                        return;

                    var pageItems = task.Result;
                    var page = new Page<T> { Items = pageItems };
                    PopulatePage(index, page);
                    FireCollectionReset();
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                var items = ItemsProvider.FetchRange(index * PageSize, PageSize, isAsync: false).GetAwaiter().GetResult();
                PopulatePage(index, new Page<T> { Items = items });
                FireCollectionReset();
            }
        }

        public T this[int index]
        {
            get
            {
                var pageIndex = index / PageSize;
                var pageOffset = index % PageSize;

                if (!_pages.TryGetValue(pageIndex, out var page))
                {
                    RequestPage(pageIndex);
                    return default;
                }

                if (page.Status == Status.HasOutdatedData)
                {
                    RequestPage(pageIndex);

                    if (pageOffset < page.Items?.Count)
                        return page.Items[pageOffset];

                    return default;
                }

                if (page.Status == Status.NoData)
                    return default;

                if (pageOffset < page.Items.Count)
                    return page.Items[pageOffset];

                return default;
            }
            set => throw new NotSupportedException();
        }

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        public bool Contains(T item)
        {
            return false;
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        int IList.IndexOf(object value)
        {
            return IndexOf((T)value);
        }

        public int IndexOf(T item)
        {
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object value)
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

        private readonly Dictionary<int, Page<T>> _pages = new Dictionary<int, Page<T>>();

        private void PopulatePage(int pageIndex, Page<T> page)
        {
            _pages[pageIndex] = page;
        }

        private void RequestPage(int pageIndex)
        {
            if (!_pages.ContainsKey(pageIndex))
            {
                _pages.Add(pageIndex, new Page<T>());
                LoadPage(pageIndex);
            }
            else if (_pages[pageIndex].Status == Status.HasOutdatedData)
            {
                _pages[pageIndex].MarkAsValid();  // We mark old data as valid until it's updated
                LoadPage(pageIndex);
            }
        }
    }
}