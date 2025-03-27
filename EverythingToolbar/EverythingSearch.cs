using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar
{
    public interface IItemsProvider<T>
    {
        int FetchCount();
        IList<T> FetchRange(int startIndex, int count);
    }

    public sealed class VirtualizingCollection<T> : IList<T>, IList
    {
        private int _count = -1;

        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize, int pageTimeout)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
            PageTimeout = pageTimeout;
        }

        public VirtualizingCollection(IItemsProvider<T> itemsProvider, int pageSize)
        {
            ItemsProvider = itemsProvider;
            PageSize = pageSize;
        }

        public VirtualizingCollection(IItemsProvider<T> itemsProvider)
        {
            ItemsProvider = itemsProvider;
        }


        private IItemsProvider<T> ItemsProvider { get; }

        private int PageSize { get; } = 100;

        private long PageTimeout { get; } = 10000;


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
            private set => _count = value;
        }

        public T this[int index]
        {
            get
            {
                // determine which page and offset within page
                int pageIndex = index / PageSize;
                int pageOffset = index % PageSize;

                // request primary page
                RequestPage(pageIndex);

                // if accessing upper 50% then request next page
                if ( pageOffset > PageSize/2 && pageIndex < Count / PageSize)
                    RequestPage(pageIndex + 1);

                // if accessing lower 50% then request prev page
                if (pageOffset < PageSize/2 && pageIndex > 0)
                    RequestPage(pageIndex - 1);

                // remove stale pages
                CleanUpPages();

                // defensive check in case of async load
                if (_pages[pageIndex] == null)
                    return default(T);

                // return requested item
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
            for (int i = 0; i < Count; i++)
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
            List<int> keys = new List<int>(_pageTouchTimes.Keys);
            foreach (int key in keys)
            {
                // page 0 is a special case, since WPF ItemsControl access the first item frequently
                if ( key != 0 && (DateTime.Now - _pageTouchTimes[key]).TotalMilliseconds > PageTimeout )
                {
                    _pages.Remove(key);
                    _pageTouchTimes.Remove(key);
                    Trace.WriteLine("Removed Page: " + key);
                }
            }
        }

        private void PopulatePage(int pageIndex, IList<T> page)
        {
            Trace.WriteLine("Page populated: "+pageIndex);
            if ( _pages.ContainsKey(pageIndex) )
                _pages[pageIndex] = page;
        }

        private void RequestPage(int pageIndex)
        {
            if (!_pages.ContainsKey(pageIndex))
            {
                _pages.Add(pageIndex, null);
                _pageTouchTimes.Add(pageIndex, DateTime.Now);
                Trace.WriteLine("Added page: " + pageIndex);
                LoadPage(pageIndex);
            }
            else
            {
                _pageTouchTimes[pageIndex] = DateTime.Now;
            }
        }

        private void LoadCount()
        {
            Count = FetchCount();
        }

        private void LoadPage(int pageIndex)
        {
            PopulatePage(pageIndex, FetchPage(pageIndex));
        }

        private IList<T> FetchPage(int pageIndex)
        {
            return ItemsProvider.FetchRange(pageIndex*PageSize, PageSize);
        }

        private int FetchCount()
        {
            return ItemsProvider.FetchCount();
        }
    }

    public class SearchResultsCollection<T> : ObservableCollection<T>
    {
        public void AddSilent(T item)
        {
            Items.Add(item);
        }

        public void ClearSilent()
        {
            Items.Clear();
        }

        public void NotifyCollectionChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class EverythingSearch : INotifyPropertyChanged, IItemsProvider<SearchResult>
    {
        private string _searchTerm = "";
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value)
                    return;

                _searchTerm = value;
                // QueryBatch(append: false);
                NotifyPropertyChanged();
            }
        }

        private Filter _currentFilter = FilterLoader.Instance.GetLastFilter();
        public Filter CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (_currentFilter.Equals(value))
                    return;

                _currentFilter = value;
                ToolbarSettings.User.LastFilter = value.Name;
                // QueryBatch(append: false);
                NotifyPropertyChanged();
            }
        }

        private int? _totalResultsNumber;
        public int? TotalResultsNumber
        {
            get => _totalResultsNumber;
            set
            {
                _totalResultsNumber = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public VirtualizingCollection<SearchResult> SearchResults { get; private set; }

        public static readonly EverythingSearch Instance = new EverythingSearch();

        private readonly object _lock = new object();
        private readonly ILogger _logger = ToolbarLogger.GetLogger<EverythingSearch>();
        private CancellationTokenSource _cancellationTokenSource;

        private EverythingSearch()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingChanged;
            SearchResults = new VirtualizingCollection<SearchResult>(this, (int)BATCH_SIZE);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsRegExEnabled) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsImportFilters))
            {
                CurrentFilter = FilterLoader.Instance.DefaultFilters[0];
            }

            if (e.PropertyName == nameof(ToolbarSettings.User.IsMatchCase) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsRegExEnabled) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsMatchPath) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsMatchWholeWord) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsHideEmptySearchResults) ||
                e.PropertyName == nameof(ToolbarSettings.User.SortBy) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsSortDescending) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsThumbnailsEnabled) ||
                e.PropertyName == nameof(ToolbarSettings.User.IsImportFilters))
            {
                // QueryBatch(append: false);
                NotifyPropertyChanged();
            }
        }

        public bool Initialize()
        {
            SetInstanceName(ToolbarSettings.User.InstanceName);
            
            var major = Everything_GetMajorVersion();
            var minor = Everything_GetMinorVersion();
            var revision = Everything_GetRevision();

            if (major > 1 || (major == 1 && minor > 4) || (major == 1 && minor == 4 && revision >= 1))
            {
                _logger.Info("Everything version: {major}.{minor}.{revision}", major, minor, revision);
                return true;
            }
            
            if (major == 0 && minor == 0 && revision == 0 && (ErrorCode)Everything_GetLastError() == ErrorCode.ErrorIpc)
            {
                HandleError((ErrorCode)Everything_GetLastError());
                _logger.Error("Failed to get Everything version number. Is Everything running?");
            }
            else
            {
                _logger.Error("Everything version {major}.{minor}.{revision} is not supported.", major, minor, revision);
            }

            return false;
        }

        public void SetInstanceName(string name)
        {
            if (name != string.Empty)
                _logger.Info("Setting Everything instance name: " + name);
            
            Everything_SetInstanceName(name);
        }

        private string ExpandDefaultMacros(string search)
        {
            foreach (var filter in FilterLoader.Instance.DefaultUserFilters)
            {
                search = search.Replace(filter.Macro + ":", filter.Search + " ");
            }
            return search;
        }

        private string BuildFinalSearchTerm()
        {
            return CurrentFilter.GetSearchPrefix() + ExpandDefaultMacros(SearchTerm);
        }

        public void Reset()
        {
            if (ToolbarSettings.User.IsEnableHistory)
                HistoryManager.Instance.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            if (!ToolbarSettings.User.IsRememberFilter && CurrentFilter != FilterLoader.Instance.DefaultFilters[0])
            {
                CurrentFilter = FilterLoader.Instance.DefaultFilters[0];
                return;
            }

            // QueryBatch(append: false);
            NotifyPropertyChanged();
        }

        public void CycleFilters(int offset = 1)
        {
            var defaultSize = FilterLoader.Instance.DefaultFilters.Count;
            var userSize = FilterLoader.Instance.UserFilters.Count;
            var defaultIndex = FilterLoader.Instance.DefaultFilters.IndexOf(CurrentFilter);
            var userIndex = FilterLoader.Instance.UserFilters.IndexOf(CurrentFilter);

            var d = defaultIndex >= 0 ? defaultIndex : defaultSize;
            var u = userIndex >= 0 ? userIndex : 0;
            var i = (d + u + offset + defaultSize + userSize) % (defaultSize + userSize);

            if (i < defaultSize)
                CurrentFilter = FilterLoader.Instance.DefaultFilters[i];
            else
                CurrentFilter = FilterLoader.Instance.UserFilters[i - defaultSize];
        }

        public void SelectFilterFromIndex(int index)
        {
            var defaultCount = FilterLoader.Instance.DefaultFilters.Count;
            var userCount = FilterLoader.Instance.UserFilters.Count;

            if (index < defaultCount)
                CurrentFilter = FilterLoader.Instance.DefaultFilters[index];
            else if (index - defaultCount < userCount)
                CurrentFilter = FilterLoader.Instance.UserFilters[index - defaultCount];
        }

        public void OpenLastSearchInEverything(string highlightedFile = "")
        {
            if(!File.Exists(ToolbarSettings.User.EverythingPath))
            {
                MessageBox.Show(Resources.MessageBoxSelectEverythingExe);
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Everything.exe|Everything.exe|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ToolbarSettings.User.EverythingPath = openFileDialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var args = "";
            if (!string.IsNullOrEmpty(ToolbarSettings.User.InstanceName)) args += " -instance \"" + ToolbarSettings.User.InstanceName + "\"";
            if (!string.IsNullOrEmpty(highlightedFile)) args += " -select \"" + highlightedFile + "\"";
            if (ToolbarSettings.User.SortBy == 0) args += " -sort \"Name\"";
            else if (ToolbarSettings.User.SortBy == 1) args += " -sort \"Path\"";
            else if (ToolbarSettings.User.SortBy == 2) args += " -sort \"Size\"";
            else if (ToolbarSettings.User.SortBy == 3) args += " -sort \"Extension\"";
            else if (ToolbarSettings.User.SortBy == 4) args += " -sort \"Type name\"";
            else if (ToolbarSettings.User.SortBy == 5) args += " -sort \"Date created\"";
            else if (ToolbarSettings.User.SortBy == 6) args += " -sort \"Date modified\"";
            else if (ToolbarSettings.User.SortBy == 7) args += " -sort \"Attributes\"";
            else if (ToolbarSettings.User.SortBy == 8) args += " -sort \"File list highlightedFileName\"";
            else if (ToolbarSettings.User.SortBy == 9) args += " -sort \"Run count\"";
            else if (ToolbarSettings.User.SortBy == 10) args += " -sort \"Date recently changed\"";
            else if (ToolbarSettings.User.SortBy == 11) args += " -sort \"Date accessed\"";
            else if (ToolbarSettings.User.SortBy == 12) args += " -sort \"Date run\"";
            args += ToolbarSettings.User.IsSortDescending ? " -sort-descending" : " -sort-ascending";
            args += ToolbarSettings.User.IsMatchCase ? " -case" : " -nocase";
            args += ToolbarSettings.User.IsMatchPath ? " -matchpath" : " -nomatchpath";
            args += ToolbarSettings.User.IsMatchWholeWord && !ToolbarSettings.User.IsRegExEnabled ? " -ww" : " -noww";
            args += ToolbarSettings.User.IsRegExEnabled ? " -regex" : " -noregex";
            args += " -s \"" + BuildFinalSearchTerm().Replace("\"", "\"\"") + "\"";

            _logger.Debug("Showing in Everything with args: " + args);
            Process.Start(ToolbarSettings.User.EverythingPath, args);
        }

        public static void IncrementRunCount(string path)
        {
            Everything_IncRunCountFromFileName(path);
        }

        private static uint CalculateEverythingSortType(int sortBy, bool descending)
        {
            var descendingOffset = descending ? 1 : 0;
            var sortType = sortBy * 2 + descendingOffset + 1;
            return (uint)sortType;
        }

        public static bool GetIsFastSort(int sortBy, bool descending)
        {
            var everythingSortType = CalculateEverythingSortType(sortBy, descending);
            return Everything_IsFastSort(everythingSortType);
        }

        private void HandleError(ErrorCode code)
        {
            switch(code)
            {
                case ErrorCode.ErrorMemory:
                    _logger.Error("Failed to allocate memory for the search query.");
                    break;
                case ErrorCode.ErrorIpc:
                    _logger.Error("IPC is not available.");
                    break;
                case ErrorCode.ErrorRegisterClassEx:
                    _logger.Error("Failed to register the search query window class.");
                    break;
                case ErrorCode.ErrorCreateWindow:
                    _logger.Error("Failed to create the search query window.");
                    break;
                case ErrorCode.ErrorCreateThread:
                    _logger.Error("Failed to create the search query thread.");
                    break;
                case ErrorCode.ErrorInvalidIndex:
                    _logger.Error("Invalid index.");
                    break;
                case ErrorCode.ErrorInvalidCall:
                    _logger.Error("Invalid call.");
                    break;
                case ErrorCode.Ok:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        [Flags]
        private enum ErrorCode
        {
            Ok,
            ErrorMemory,
            ErrorIpc,
            ErrorRegisterClassEx,
            ErrorCreateWindow,
            ErrorCreateThread,
            ErrorInvalidIndex,
            ErrorInvalidCall
        }

        private const uint BATCH_SIZE = 100;

        private const int EVERYTHING_FULL_PATH_AND_FILE_NAME = 0x00000004;
        private const int EVERYTHING_HIGHLIGHTED_FILE_NAME = 0x00002000;
        private const int EVERYTHING_HIGHLIGHTED_PATH = 0x00004000;
        private const int EVERYTHING_REQUEST_SIZE = 0x00000010;
        private const int EVERYTHING_REQUEST_DATE_MODIFIED = 0x00000040;

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_SetSearchW(string lpSearchString);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_SetInstanceName(string lpInstanceName);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMatchPath(bool bEnable);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMatchCase(bool bEnable);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMatchWholeWord(bool bEnable);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRegex(bool bEnable);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetMax(uint dwMax);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetOffset(uint dwOffset);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_QueryW(bool bWait);
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetNumResults();
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetTotResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathNameW(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetSort(uint dwSortType);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Everything_GetResultHighlightedFileName(uint nIndex);
        [DllImport("Everything64.dll")]
        private static extern uint Everything_IncRunCountFromFileName(string lpFileName);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Everything_GetResultHighlightedPath(uint nIndex);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_IsFileResult(uint nIndex);
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetLastError();
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetMajorVersion();
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetMinorVersion();
        [DllImport("Everything64.dll")]
        private static extern uint Everything_GetRevision();
        [DllImport("Everything64.dll")]
        private static extern bool Everything_IsFastSort(uint sortType);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultSize(UInt32 nIndex, out long lpFileSize);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_GetResultDateModified(UInt32 nIndex, out FILETIME lpFileTime);

        public int FetchCount()
        {
            var search = BuildFinalSearchTerm();
            Everything_SetSearchW(search);
            Everything_SetRequestFlags(EVERYTHING_REQUEST_SIZE | EVERYTHING_REQUEST_DATE_MODIFIED);
            Everything_SetSort(CalculateEverythingSortType(ToolbarSettings.User.SortBy, ToolbarSettings.User.IsSortDescending));
            Everything_SetMatchCase(ToolbarSettings.User.IsMatchCase);
            Everything_SetMatchPath(ToolbarSettings.User.IsMatchPath);
            Everything_SetMatchWholeWord(ToolbarSettings.User.IsMatchWholeWord && !ToolbarSettings.User.IsRegExEnabled);
            Everything_SetRegex(ToolbarSettings.User.IsRegExEnabled);
            Everything_SetMax(BATCH_SIZE);
            Everything_SetOffset(0);

            if (!Everything_QueryW(true))
            {
                HandleError((ErrorCode)Everything_GetLastError());
                return 0;
            }

            return (int)Everything_GetTotResults();
        }

        public IList<SearchResult> FetchRange(int startIndex, int count)
        {
            var search = BuildFinalSearchTerm();
            Everything_SetSearchW(search);
            const uint flags = EVERYTHING_FULL_PATH_AND_FILE_NAME | EVERYTHING_HIGHLIGHTED_PATH |
                               EVERYTHING_HIGHLIGHTED_FILE_NAME | EVERYTHING_REQUEST_SIZE |
                               EVERYTHING_REQUEST_DATE_MODIFIED;
            Everything_SetRequestFlags(flags);
            Everything_SetSort(CalculateEverythingSortType(ToolbarSettings.User.SortBy, ToolbarSettings.User.IsSortDescending));
            Everything_SetMatchCase(ToolbarSettings.User.IsMatchCase);
            Everything_SetMatchPath(ToolbarSettings.User.IsMatchPath);
            Everything_SetMatchWholeWord(ToolbarSettings.User.IsMatchWholeWord && !ToolbarSettings.User.IsRegExEnabled);
            Everything_SetRegex(ToolbarSettings.User.IsRegExEnabled);
            Everything_SetMax((uint)count);
            Everything_SetOffset((uint)startIndex);

            if (!Everything_QueryW(true))
            {
                HandleError((ErrorCode)Everything_GetLastError());
                return new List<SearchResult>();
            }

            var results = new List<SearchResult>();
            var batchResultsCount = Everything_GetNumResults();

            for (uint i = 0; i < batchResultsCount; i++)
            {
                var highlightedPath = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i));
                var highlightedFileName = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i));
                var isFile = Everything_IsFileResult(i);
                var fullPathAndFilename = new StringBuilder(4096);
                Everything_GetResultFullPathNameW(i, fullPathAndFilename, 4096);
                Everything_GetResultSize(i, out var fileSize);
                Everything_GetResultDateModified(i, out var dateModified);

                results.Add(new SearchResult
                {
                    HighlightedPath = highlightedPath,
                    HighlightedFileName = highlightedFileName,
                    FullPathAndFileName = fullPathAndFilename.ToString(),
                    IsFile = isFile,
                    DateModified = dateModified,
                    FileSize = fileSize
                });
            }

            return results;
        }
    }
}
