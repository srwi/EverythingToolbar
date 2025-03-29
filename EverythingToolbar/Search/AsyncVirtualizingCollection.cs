using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace EverythingToolbar.Search
{
    public class AsyncVirtualizingCollection<T> : VirtualizingCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public AsyncVirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
            : base(itemsProvider, pageSize)
        {
            _synchronizationContext = SynchronizationContext.Current;
        }

        private readonly SynchronizationContext _synchronizationContext;

        protected SynchronizationContext SynchronizationContext
        {
            get { return _synchronizationContext; }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            NotifyCollectionChangedEventHandler h = CollectionChanged;
            if (h != null)
                h(this, e);
        }

        private void FireCollectionReset()
        {
            NotifyCollectionChangedEventArgs e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler h = PropertyChanged;
            if (h != null)
                h(this, e);
        }

        private void FirePropertyChanged(string propertyName)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
        }

        private bool _isLoading;

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if ( value != _isLoading )
                {
                    _isLoading = value;
                }
                FirePropertyChanged("IsLoading");
            }
        }

        private bool _isAsync = true;
        
        public bool IsAsync
        {
            get { return _isAsync; }
            set
            {
                if (_isAsync != value)
                {
                    _isAsync = value;
                    FirePropertyChanged("IsAsync");
                    
                    // If we're currently loading and switching to sync mode,
                    // we should cancel any async operations and reload synchronously
                    if (!_isAsync && _isLoading)
                    {
                        IsLoading = false;
                        // Force reload of current data synchronously
                        // RefreshPages();
                    }
                }
            }
        }

        protected override void LoadCount()
        {
            if (IsAsync)
            {
                Count = 0;
                IsLoading = true;
                ThreadPool.QueueUserWorkItem(LoadCountWork);
            }
            else
            {
                base.LoadCount();
            }
        }

        private void LoadCountWork(object args)
        {
            int count = FetchCount();
            SynchronizationContext.Send(LoadCountCompleted, count);
        }

        private void LoadCountCompleted(object args)
        {
            Count = (int)args;
            IsLoading = false;
            FireCollectionReset();
        }

        protected override void LoadPage(int index)
        {
            if (IsAsync)
            {
                IsLoading = true;
                ThreadPool.QueueUserWorkItem(LoadPageWork, index);
            }
            else
            {
                base.LoadPage(index);
            }
        }

        private void LoadPageWork(object args)
        {
            int pageIndex = (int)args;
            IList<T> page = FetchPage(pageIndex);
            SynchronizationContext.Send(LoadPageCompleted, new object[]{ pageIndex, page });
        }

        private void LoadPageCompleted(object args)
        {
            int pageIndex = (int)((object[]) args)[0];
            IList<T> page = (IList<T>)((object[])args)[1];

            PopulatePage(pageIndex, page);
            IsLoading = false;
            FireCollectionReset();
        }
    }
}
