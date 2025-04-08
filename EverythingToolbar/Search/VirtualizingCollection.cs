using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EverythingToolbar.Search
{
    public sealed class VirtualizingCollection<T> : IList<T>, IList, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
            SynchronizationContext = SynchronizationContext.Current;
        }

        private IItemsProvider<T> ItemsProvider { get; }

        private int PageSize { get; }

        private const long PageTimeout = 60000;

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
            if (IsAsync)
            {
                Count = 0;
                ThreadPool.QueueUserWorkItem(LoadCountWork);
            }
            else
            {
                Count = FetchCount();
            }
        }

        private void LoadCountWork(object args)
        {
            var count = FetchCount();
            SynchronizationContext.Send(LoadCountCompleted, count);
        }

        private void LoadCountCompleted(object args)
        {
            Count = (int)args;
            FireCollectionReset();
        }

        private void LoadPage(int index)
        {
            if (IsAsync)
            {
                ThreadPool.QueueUserWorkItem(LoadPageWork, index);
            }
            else
            {
                PopulatePage(index, FetchPage(index));
            }
        }

        private void LoadPageWork(object args)
        {
            var pageIndex = (int)args;
            var page = FetchPage(pageIndex);
            SynchronizationContext.Send(LoadPageCompleted, new object[]{ pageIndex, page });
        }

        private void LoadPageCompleted(object args)
        {
            var pageIndex = (int)((object[]) args)[0];
            var page = (IList<T>)((object[])args)[1];

            PopulatePage(pageIndex, page);
            FireCollectionReset();
        }

        public T this[int index]
        {
            get
            {
                var pageIndex = index / PageSize;
                var pageOffset = index % PageSize;

                RequestPage(pageIndex);
                CleanUpPages();

                // Return default if async load is in progress
                if (_pages[pageIndex] == null)
                    return default;

                return _pages[pageIndex][pageOffset];
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
            return IndexOf((T) value);
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

        private readonly Dictionary<int, IList<T>> _pages = new Dictionary<int, IList<T>>();
        private readonly Dictionary<int, DateTime> _pageTouchTimes = new Dictionary<int, DateTime>();

        private void CleanUpPages()
        {
            var keys = new List<int>(_pageTouchTimes.Keys);
            foreach (var key in keys)
            {
                // Page 0 gets accessed frequently, so we don't remove it
                if ( key != 0 && (DateTime.Now - _pageTouchTimes[key]).TotalMilliseconds > PageTimeout )
                {
                    _pages.Remove(key);
                    _pageTouchTimes.Remove(key);
                }
            }
        }

        private void PopulatePage(int pageIndex, IList<T> page)
        {
            if ( _pages.ContainsKey(pageIndex) )
                _pages[pageIndex] = page;
        }

        private void RequestPage(int pageIndex)
        {
            if (!_pages.ContainsKey(pageIndex))
            {
                _pages.Add(pageIndex, null);
                _pageTouchTimes.Add(pageIndex, DateTime.Now);
                LoadPage(pageIndex);
            }
            else
            {
                _pageTouchTimes[pageIndex] = DateTime.Now;
            }
        }

        private IList<T> FetchPage(int pageIndex)
        {
            return ItemsProvider.FetchRange(pageIndex * PageSize, PageSize);
        }

        private int FetchCount()
        {
            return ItemsProvider.FetchCount(PageSize);
        }
    }
}
