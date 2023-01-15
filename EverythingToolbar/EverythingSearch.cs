using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using NLog;
using System;
using System.Collections.ObjectModel;
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

namespace EverythingToolbar
{
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

                lock (_lock)
                    SearchResults.Clear();
                QueryBatch();

                NotifyPropertyChanged();
            }
        }

        private Filter _currentFilter = FilterLoader.Instance.GetLastFilter();
        public Filter CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (_currentFilter == value)
                    return;

                _currentFilter = value;

                lock (_lock)
                    SearchResults.Clear();
                QueryBatch();

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

        public ObservableCollection<SearchResult> SearchResults = new ObservableCollection<SearchResult>();

        public static readonly EverythingSearch Instance = new EverythingSearch();

        private readonly object _lock = new object();
        private readonly ILogger _logger = ToolbarLogger.GetLogger<EverythingSearch>();
        private CancellationTokenSource _cancellationTokenSource;

        private EverythingSearch()
        {
            Properties.Settings.Default.PropertyChanged += OnSettingChanged;
            BindingOperations.EnableCollectionSynchronization(SearchResults, _lock);
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
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
                lock (_lock)
                    SearchResults.Clear();
                QueryBatch();
            }
        }

        public void Initialize()
        {
            uint major = Everything_GetMajorVersion();
            uint minor = Everything_GetMinorVersion();
            uint revision = Everything_GetRevision();

            if ((major > 1) || ((major == 1) && (minor > 4)) || ((major == 1) && (minor == 4) && (revision >= 1)))
            {
                _logger.Info("Everything version: {major}.{minor}.{revision}", major, minor, revision);
                SetInstanceName(Properties.Settings.Default.instanceName);
            }
            else if (major == 0 && minor == 0 && revision == 0 && (ErrorCode)Everything_GetLastError() == ErrorCode.EVERYTHING_ERROR_IPC)
            {
                HandleError((ErrorCode)Everything_GetLastError());
                _logger.Error("Failed to get Everything version number. Is Everything running?");
                return;
            }
            else
            {
                _logger.Error("Everything version {major}.{minor}.{revision} is not supported.", major, minor, revision);
                return;
            }
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

        public void QueryBatch()
        {
            _cancellationTokenSource?.Cancel();

            if (SearchTerm == null || (SearchTerm == "" && Properties.Settings.Default.isHideEmptySearchResults))
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
                    uint flags = EVERYTHING_FULL_PATH_AND_FILE_NAME | EVERYTHING_HIGHLIGHTED_PATH | EVERYTHING_HIGHLIGHTED_FILE_NAME;
                    bool regEx = CurrentFilter.IsRegExEnabled ?? Properties.Settings.Default.isRegExEnabled;

                    string search = CurrentFilter.Search + (CurrentFilter.Search.Length > 0 && !regEx ? " " : "") + SearchTerm;
                    foreach (Filter filter in FilterLoader.Instance.DefaultUserFilters)
                    {
                        search = search.Replace(filter.Macro + ":", filter.Search + " ");
                    }

                    Everything_SetSearchW(search);
                    Everything_SetRequestFlags(flags);
                    Everything_SetSort((uint)Properties.Settings.Default.sortBy);
                    Everything_SetMatchCase(CurrentFilter.IsMatchCase ?? Properties.Settings.Default.isMatchCase);
                    Everything_SetMatchPath(CurrentFilter.IsMatchPath ?? Properties.Settings.Default.isMatchPath);
                    Everything_SetMatchWholeWord(CurrentFilter.IsMatchWholeWord ?? Properties.Settings.Default.isMatchWholeWord);
                    Everything_SetRegex(regEx);
                    Everything_SetMax(BATCH_SIZE);
                    lock (_lock)
                        Everything_SetOffset((uint)SearchResults.Count);

                    if (!Everything_QueryW(true))
                    {
                        HandleError((ErrorCode)Everything_GetLastError());
                        return;
                    }

                    uint resultsCount = Everything_GetNumResults();
                    lock (_lock)
                        TotalResultsNumber = (int)Everything_GetTotResults();

                    for (uint i = 0; i < resultsCount; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string highlightedPath = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i)).ToString();
                        string highlightedFileName = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i)).ToString();
                        bool isFile = Everything_IsFileResult(i);
                        StringBuilder fullPathAndFilename = new StringBuilder(4096);
                        Everything_GetResultFullPathNameW(i, fullPathAndFilename, 4096);

                        lock (_lock)
                        {
                            SearchResults.Add(new SearchResult()
                            {
                                HighlightedPath = highlightedPath,
                                HighlightedFileName = highlightedFileName,
                                FullPathAndFileName = fullPathAndFilename.ToString(),
                                IsFile = isFile
                            });
                        }
                    }
                }
                catch (OperationCanceledException) { }
            }, cancellationToken);
        }

        public void Reset()
        {
            if (Properties.Settings.Default.isHideEmptySearchResults)
                SearchTerm = null;
            else
                SearchTerm = "";

            if (!Properties.Settings.Default.isRememberFilter)
                _currentFilter = FilterLoader.Instance.DefaultFilters[0];

            lock (_lock)
                SearchResults.Clear();
            QueryBatch();
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

        public void OpenLastSearchInEverything(string highlighted_file = "")
        {
            if(!File.Exists(Properties.Settings.Default.everythingPath))
            {
                MessageBox.Show(Properties.Resources.MessageBoxSelectEverythingExe);
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Everything.exe|Everything.exe|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Properties.Settings.Default.everythingPath = openFileDialog.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            string args = "";
            if (!string.IsNullOrEmpty(highlighted_file)) args += " -select \"" + highlighted_file + "\"";
            if (Properties.Settings.Default.sortBy <= 2) args += " -sort \"Name\"";
            else if (Properties.Settings.Default.sortBy <= 4) args += " -sort \"Path\"";
            else if (Properties.Settings.Default.sortBy <= 6) args += " -sort \"Size\"";
            else if (Properties.Settings.Default.sortBy <= 8) args += " -sort \"Extension\"";
            else if (Properties.Settings.Default.sortBy <= 10) args += " -sort \"Type name\"";
            else if (Properties.Settings.Default.sortBy <= 12) args += " -sort \"Date created\"";
            else if (Properties.Settings.Default.sortBy <= 14) args += " -sort \"Date modified\"";
            else if (Properties.Settings.Default.sortBy <= 16) args += " -sort \"Attributes\"";
            else if (Properties.Settings.Default.sortBy <= 18) args += " -sort \"File list highlightedFileName\"";
            else if (Properties.Settings.Default.sortBy <= 20) args += " -sort \"Run count\"";
            else if (Properties.Settings.Default.sortBy <= 22) args += " -sort \"Date recently changed\"";
            else if (Properties.Settings.Default.sortBy <= 24) args += " -sort \"Date accessed\"";
            else if (Properties.Settings.Default.sortBy <= 26) args += " -sort \"Date run\"";
            args += Properties.Settings.Default.sortBy % 2 > 0 ? " -sort-ascending" : " -sort-descending";
            args += Properties.Settings.Default.isMatchCase ? " -case" : " -nocase";
            args += Properties.Settings.Default.isMatchPath ? " -matchpath" : " -nomatchpath";
            args += Properties.Settings.Default.isMatchWholeWord ? " -ww" : " -noww";
            args += Properties.Settings.Default.isRegExEnabled ? " -regex" : " -noregex";
            args += " -s \"" + (CurrentFilter.Search + " " + SearchTerm).Replace("\"", "\"\"") + "\"";

            Process.Start(Properties.Settings.Default.everythingPath, args);
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
                case ErrorCode.EVERYTHING_ERROR_MEMORY:
                    _logger.Error("Failed to allocate memory for the search query.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_IPC:
                    _logger.Error("IPC is not available.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_REGISTERCLASSEX:
                    _logger.Error("Failed to register the search query window class.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_CREATEWINDOW:
                    _logger.Error("Failed to create the search query window.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_CREATETHREAD:
                    _logger.Error("Failed to create the search query thread.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_INVALIDINDEX:
                    _logger.Error("Invalid index.");
                    break;
                case ErrorCode.EVERYTHING_ERROR_INVALIDCALL:
                    _logger.Error("Invalid call.");
                    break;
            }
        }

        [Flags]
        private enum ErrorCode
        {
            EVERYTHING_OK,
            EVERYTHING_ERROR_MEMORY,
            EVERYTHING_ERROR_IPC,
            EVERYTHING_ERROR_REGISTERCLASSEX,
            EVERYTHING_ERROR_CREATEWINDOW,
            EVERYTHING_ERROR_CREATETHREAD,
            EVERYTHING_ERROR_INVALIDINDEX,
            EVERYTHING_ERROR_INVALIDCALL
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
