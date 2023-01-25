using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;

namespace EverythingToolbar
{
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

    public class EverythingSearch : INotifyPropertyChanged
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

                QueryBatch(append: false);

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
                
                lock (_lock)
                    SearchResults.Clear();
                QueryBatch(append: false);

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

        public readonly SearchResultsCollection<SearchResult> SearchResults = new SearchResultsCollection<SearchResult>();

        public static readonly EverythingSearch Instance = new EverythingSearch();

        private readonly object _lock = new object();
        private readonly ILogger _logger = ToolbarLogger.GetLogger<EverythingSearch>();
        private CancellationTokenSource _cancellationTokenSource;

        private EverythingSearch()
        {
            Settings.Default.PropertyChanged += OnSettingChanged;
            BindingOperations.EnableCollectionSynchronization(SearchResults, _lock);
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled")
                CurrentFilter = FilterLoader.Instance.DefaultFilters[0];

            if (e.PropertyName == "isMatchCase" ||
                e.PropertyName == "isRegExEnabled" ||
                e.PropertyName == "isMatchPath" ||
                e.PropertyName == "isMatchWholeWord" ||
                e.PropertyName == "isHideEmptySearchResults" ||
                e.PropertyName == "sortBy" ||
                e.PropertyName == "isThumbnailsEnabled")
            {
                QueryBatch(append: false);
            }
        }

        public bool Initialize()
        {
            var major = Everything_GetMajorVersion();
            var minor = Everything_GetMinorVersion();
            var revision = Everything_GetRevision();

            if (major > 1 || (major == 1 && minor > 4) || (major == 1 && minor == 4 && revision >= 1))
            {
                _logger.Info("Everything version: {major}.{minor}.{revision}", major, minor, revision);
                SetInstanceName(Settings.Default.instanceName);
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
            if (name == "")
            {
                Everything_SetInstanceName("");
                return;
            }

            _logger.Info("Setting Everything instance name: " + name);
            Everything_SetInstanceName(name);
        }

        public void QueryBatch(bool append)
        {
            _cancellationTokenSource?.Cancel();

            if (SearchTerm.Length == 0 && Settings.Default.isHideEmptySearchResults)
            {
                lock (_lock)
                {
                    SearchResults.Clear();
                    TotalResultsNumber = null;
                }
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            Task.Run(() =>
            {
                try
                {
                    lock (_lock)
                    {
                        if (!append)
                            SearchResults.ClearSilent();                        
                    }    
                    
                    uint flags = EVERYTHING_FULL_PATH_AND_FILE_NAME | EVERYTHING_HIGHLIGHTED_PATH | EVERYTHING_HIGHLIGHTED_FILE_NAME;
                    bool regEx = CurrentFilter.IsRegExEnabled ?? Settings.Default.isRegExEnabled;

                    string search = CurrentFilter.Search + (CurrentFilter.Search.Length > 0 && !regEx ? " " : "") + SearchTerm;
                    foreach (Filter filter in FilterLoader.Instance.DefaultUserFilters)
                    {
                        search = search.Replace(filter.Macro + ":", filter.Search + " ");
                    }

                    Everything_SetSearchW(search);
                    Everything_SetRequestFlags(flags);
                    Everything_SetSort((uint)Settings.Default.sortBy);
                    Everything_SetMatchCase(CurrentFilter.IsMatchCase ?? Settings.Default.isMatchCase);
                    Everything_SetMatchPath(CurrentFilter.IsMatchPath ?? Settings.Default.isMatchPath);
                    Everything_SetMatchWholeWord(CurrentFilter.IsMatchWholeWord ?? Settings.Default.isMatchWholeWord);
                    Everything_SetRegex(regEx);
                    Everything_SetMax(BATCH_SIZE);
                    lock (_lock)
                        Everything_SetOffset((uint)SearchResults.Count);

                    if (!Everything_QueryW(true))
                    {
                        HandleError((ErrorCode)Everything_GetLastError());
                        return;
                    }

                    uint batchResultsCount = Everything_GetNumResults();
                    lock (_lock)
                        TotalResultsNumber = (int)Everything_GetTotResults();

                    for (uint i = 0; i < batchResultsCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string highlightedPath = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i));
                        string highlightedFileName = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i));
                        bool isFile = Everything_IsFileResult(i);
                        StringBuilder fullPathAndFilename = new StringBuilder(4096);
                        Everything_GetResultFullPathNameW(i, fullPathAndFilename, 4096);

                        lock (_lock)
                        {
                            SearchResults.AddSilent(new SearchResult()
                            {
                                HighlightedPath = highlightedPath,
                                HighlightedFileName = highlightedFileName,
                                FullPathAndFileName = fullPathAndFilename.ToString(),
                                IsFile = isFile
                            });
                        }
                    }
                    
                    if (!append || batchResultsCount > 0)
                        lock (_lock)
                            SearchResults.NotifyCollectionChanged();
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
        }

        public void Reset()
        {
            if (Settings.Default.isEnableHistory)
                HistoryManager.Instance.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            if (!Settings.Default.isRememberFilter)
            {
                CurrentFilter = FilterLoader.Instance.DefaultFilters[0];
                return;
            }

            QueryBatch(append: false);
        }

        public void CycleFilters(int offset = 1)
        {
            int defaultSize = FilterLoader.Instance.DefaultFilters.Count;
            int userSize = FilterLoader.Instance.UserFilters.Count;
            int defaultIndex = FilterLoader.Instance.DefaultFilters.IndexOf(CurrentFilter);
            int userIndex = FilterLoader.Instance.UserFilters.IndexOf(CurrentFilter);

            int d = defaultIndex >= 0 ? defaultIndex : defaultSize;
            int u = userIndex >= 0 ? userIndex : 0;
            int i = (d + u + offset + defaultSize + userSize) % (defaultSize + userSize);

            if (i < defaultSize)
                CurrentFilter = FilterLoader.Instance.DefaultFilters[i];
            else
                CurrentFilter = FilterLoader.Instance.UserFilters[i - defaultSize];
        }

        public void SelectFilterFromIndex(int index)
        {
            int defaultCount = FilterLoader.Instance.DefaultFilters.Count;
            int userCount = FilterLoader.Instance.UserFilters.Count;

            if (index < defaultCount)
                CurrentFilter = FilterLoader.Instance.DefaultFilters[index];
            else if (index - defaultCount < userCount)
                CurrentFilter = FilterLoader.Instance.UserFilters[index - defaultCount];
        }

        public void OpenLastSearchInEverything(string highlightedFile = "")
        {
            if(!File.Exists(Settings.Default.everythingPath))
            {
                MessageBox.Show(Resources.MessageBoxSelectEverythingExe);
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Everything.exe|Everything.exe|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Settings.Default.everythingPath = openFileDialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var args = "";
            if (!string.IsNullOrEmpty(highlightedFile)) args += " -select \"" + highlightedFile + "\"";
            if (Settings.Default.sortBy <= 2) args += " -sort \"Name\"";
            else if (Settings.Default.sortBy <= 4) args += " -sort \"Path\"";
            else if (Settings.Default.sortBy <= 6) args += " -sort \"Size\"";
            else if (Settings.Default.sortBy <= 8) args += " -sort \"Extension\"";
            else if (Settings.Default.sortBy <= 10) args += " -sort \"Type name\"";
            else if (Settings.Default.sortBy <= 12) args += " -sort \"Date created\"";
            else if (Settings.Default.sortBy <= 14) args += " -sort \"Date modified\"";
            else if (Settings.Default.sortBy <= 16) args += " -sort \"Attributes\"";
            else if (Settings.Default.sortBy <= 18) args += " -sort \"File list highlightedFileName\"";
            else if (Settings.Default.sortBy <= 20) args += " -sort \"Run count\"";
            else if (Settings.Default.sortBy <= 22) args += " -sort \"Date recently changed\"";
            else if (Settings.Default.sortBy <= 24) args += " -sort \"Date accessed\"";
            else if (Settings.Default.sortBy <= 26) args += " -sort \"Date run\"";
            args += Settings.Default.sortBy % 2 > 0 ? " -sort-ascending" : " -sort-descending";
            args += Settings.Default.isMatchCase ? " -case" : " -nocase";
            args += Settings.Default.isMatchPath ? " -matchpath" : " -nomatchpath";
            args += Settings.Default.isMatchWholeWord ? " -ww" : " -noww";
            args += Settings.Default.isRegExEnabled ? " -regex" : " -noregex";
            args += " -s \"" + (CurrentFilter.Search + " " + SearchTerm).Replace("\"", "\"\"") + "\"";

            Process.Start(Settings.Default.everythingPath, args);
        }

        public void IncrementRunCount(string path)
        {
            Everything_IncRunCountFromFileName(path);
        }

        public bool GetIsFastSort(int sortBy)
        {
            return Everything_IsFastSort((uint)sortBy);
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
    }
}
