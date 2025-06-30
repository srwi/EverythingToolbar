using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Search
{
    public class SearchResultProvider : IItemsProvider<SearchResult>, INotifyPropertyChanged
    {
        private class QueryQueueItem
        {
            public QueryType Type { get; }
            public int StartIndex { get; }
            public TaskCompletionSource<int> CountCompletionSource { get; } = null!;
            public TaskCompletionSource<IList<SearchResult>> RangeCompletionSource { get; } = null!;

            public QueryQueueItem(TaskCompletionSource<int> completionSource)
            {
                Type = QueryType.Count;
                CountCompletionSource = completionSource;
            }

            public QueryQueueItem(TaskCompletionSource<IList<SearchResult>> completionSource, int startIndex)
            {
                Type = QueryType.Range;
                StartIndex = startIndex;
                RangeCompletionSource = completionSource;
            }

            public void Cancel()
            {
                if (Type == QueryType.Count)
                    CountCompletionSource.TrySetCanceled();
                else
                    RangeCompletionSource.TrySetCanceled();
            }
        }

        private readonly SearchState _searchState;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchResultProvider>();
        private static readonly object SyncLock = new();
        private static readonly Queue<QueryQueueItem> QueryQueue = new();
        private static QueryQueueItem? _currentQuery;
        private static IntPtr _responseWindowHandle;
        private static bool _initialized;
        private static bool _firstPageAvailable;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public SearchResultProvider(SearchState searchState)
        {
            _searchState = searchState;

            if (!_initialized)
                _initialized = Initialize();
        }

        private static void InitializeAsyncResponseWindow()
        {
            if (_responseWindowHandle == IntPtr.Zero)
            {
                const int gwlpWndproc = -4;
                const IntPtr hwndMessage = -3;

                // Create a message-only window to receive IPC messages
                _responseWindowHandle = NativeMethods.CreateWindowEx(
                    0,
                    "STATIC",
                    null!,
                    0,
                    0, 0, 0, 0,
                    hwndMessage,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (_responseWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.SetWindowLongPtr(_responseWindowHandle, gwlpWndproc,
                        Marshal.GetFunctionPointerForDelegate<NativeMethods.WndProcDelegate>(HandleWindowMessage));
                }
                else
                {
                    Logger.Error("Failed to create IPC response window.");
                    return;
                }
            }

            Everything_SetReplyWindow(_responseWindowHandle);
        }

        private void ClearQueryQueue()
        {
            if (_currentQuery != null)
            {
                _currentQuery.Cancel();
                _currentQuery = null;
            }

            while (QueryQueue.Count > 0)
            {
                QueryQueue.Dequeue().Cancel();
            }
        }

        private static void ProcessNextQuery()
        {
            lock (SyncLock)
            {
                if (_currentQuery != null || QueryQueue.Count == 0)
                    return;

                _currentQuery = QueryQueue.Dequeue();

                switch (_currentQuery.Type)
                {
                    case QueryType.Count:
                        Everything_SetOffset(0);
                        Everything_SetReplyID((uint)QueryType.Count);
                        break;
                    case QueryType.Range:
                        Everything_SetOffset((uint)_currentQuery.StartIndex);
                        Everything_SetReplyID((uint)QueryType.Range);
                        break;
                }

                if (!Query(isAsync: true))
                {
                    if (_currentQuery.Type == QueryType.Count)
                        _currentQuery.CountCompletionSource.TrySetResult(0);
                    else
                        _currentQuery.RangeCompletionSource.TrySetResult(new List<SearchResult>());

                    _currentQuery = null;

                    // Try to process the next query in queue
                    Dispatcher.CurrentDispatcher.BeginInvoke(ProcessNextQuery);
                }
            }
        }

        private static IntPtr HandleWindowMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            lock (SyncLock)
            {
                if (Everything_IsQueryReply(msg, wParam, lParam, (uint)QueryType.Count))
                {
                    // We remember the first page query to avoid querying it again when actually fetching data
                    _firstPageAvailable = true;

                    var resultsCount = (int)Everything_GetTotResults();

                    if (_currentQuery?.Type == QueryType.Count)
                    {
                        var completionSource = _currentQuery.CountCompletionSource;
                        _currentQuery = null;

                        Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                        {
                            completionSource.TrySetResult(resultsCount);
                            ProcessNextQuery();
                        });
                    }
                    return 1;
                }

                if (Everything_IsQueryReply(msg, wParam, lParam, (uint)QueryType.Range))
                {
                    IList<SearchResult> results = GetResultsFromEverythingQuery();
                    if (_currentQuery?.Type == QueryType.Range)
                    {
                        var completionSource = _currentQuery.RangeCompletionSource;
                        _currentQuery = null;

                        Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                        {
                            completionSource.TrySetResult(results);
                            ProcessNextQuery();
                        });
                    }
                    return 1;
                }
            }

            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private static IList<SearchResult> GetResultsFromEverythingQuery()
        {
            var results = new List<SearchResult>();
            var fullPathAndFilename = new StringBuilder(4096);
            for (uint i = 0; i < Everything_GetNumResults(); i++)
            {
                var highlightedPath = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i));
                var highlightedFileName = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i));
                var isFile = Everything_IsFileResult(i);
                Everything_GetResultFullPathNameW(i, fullPathAndFilename.Clear(), 4096);
                Everything_GetResultSize(i, out var fileSize);
                Everything_GetResultDateModified(i, out var dateModified);
                results.Add(new SearchResult
                {
                    HighlightedPath = highlightedPath ?? "<invalid>",
                    HighlightedFileName = highlightedFileName ?? "<invalid>",
                    FullPathAndFileName = fullPathAndFilename.ToString(),
                    IsFile = isFile,
                    DateModified = dateModified,
                    FileSize = fileSize
                });
            }
            return results;
        }

        public Task<int> FetchCount(int pageSize, bool isAsync)
        {
            lock (SyncLock)
            {
                var search = _searchState.Filter.GetSearchPrefix() + _searchState.SearchTerm;
                Everything_SetSearchW(search);
                Everything_SetRequestFlags((uint)(Flags.FullPathAndFileName | Flags.HighlightedPath |
                                                  Flags.HighlightedFileName | Flags.RequestSize |
                                                  Flags.RequestDateModified));
                SetSortType(_searchState.SortBy, _searchState.IsSortDescending);
                Everything_SetMatchCase(_searchState.IsMatchCase);
                Everything_SetMatchPath(_searchState.IsMatchPath);
                Everything_SetMatchWholeWord(_searchState is { IsMatchWholeWord: true, IsRegExEnabled: false });
                Everything_SetRegex(_searchState.IsRegExEnabled);
                Everything_SetMax((uint)pageSize);

                // Clear all existing queries since we're starting a new search
                ClearQueryQueue();

                if (isAsync)
                {
                    IsBusy = true;
                    var countCompletionSource = new TaskCompletionSource<int>();
                    var countQuery = new QueryQueueItem(countCompletionSource);

                    QueryQueue.Enqueue(countQuery);
                    Dispatcher.CurrentDispatcher.BeginInvoke(ProcessNextQuery);

                    countCompletionSource.Task.ContinueWith(_ => IsBusy = false, TaskScheduler.FromCurrentSynchronizationContext());

                    return countCompletionSource.Task;
                }
                else
                {
                    IsBusy = true;
                    Everything_SetOffset(0);

                    int result = 0;
                    if (Query(isAsync: false))
                        result = (int)Everything_GetTotResults();

                    IsBusy = false;
                    return Task.FromResult(result);
                }
            }
        }

        public Task<IList<SearchResult>> FetchRange(int startIndex, int pageSize, bool isAsync)
        {
            lock (SyncLock)
            {
                if (_firstPageAvailable && startIndex == 0)
                {
                    return Task.FromResult(GetResultsFromEverythingQuery());
                }

                if (isAsync)
                {
                    IsBusy = true;
                    var rangeCompletionSource = new TaskCompletionSource<IList<SearchResult>>();
                    var rangeQuery = new QueryQueueItem(rangeCompletionSource, startIndex);

                    QueryQueue.Enqueue(rangeQuery);
                    Dispatcher.CurrentDispatcher.BeginInvoke(ProcessNextQuery);

                    rangeCompletionSource.Task.ContinueWith(_ => IsBusy = false, TaskScheduler.FromCurrentSynchronizationContext());

                    return rangeCompletionSource.Task;
                }
                else
                {
                    IsBusy = true;
                    Everything_SetOffset((uint)startIndex);

                    IList<SearchResult> result;
                    if (Query(isAsync: false))
                        result = GetResultsFromEverythingQuery();
                    else
                        result = new List<SearchResult>();

                    IsBusy = false;
                    return Task.FromResult(result);
                }
            }
        }

        private static bool Query(bool isAsync)
        {
            if (isAsync)
                InitializeAsyncResponseWindow();

            _firstPageAvailable = false;

            if (!Everything_QueryW(!isAsync))
            {
                LogLastError();
                return false;
            }

            return true;
        }

        private static void LogLastError()
        {
            ErrorCode lastError = (ErrorCode)Everything_GetLastError();

            switch (lastError)
            {
                case ErrorCode.ErrorMemory:
                    Logger.Error("Failed to allocate memory for the search query.");
                    break;
                case ErrorCode.ErrorIpc:
                    Logger.Error("IPC is not available. Is Everything running? If not, go to www.voidtools.com and download Everything.");
                    break;
                case ErrorCode.ErrorRegisterClassEx:
                    Logger.Error("Failed to register the search query window class.");
                    break;
                case ErrorCode.ErrorCreateWindow:
                    Logger.Error("Failed to create the search query window.");
                    break;
                case ErrorCode.ErrorCreateThread:
                    Logger.Error("Failed to create the search query thread.");
                    break;
                case ErrorCode.ErrorInvalidIndex:
                    Logger.Error("Invalid index.");
                    break;
                case ErrorCode.ErrorInvalidCall:
                    Logger.Error("Invalid call.");
                    break;
                case ErrorCode.Ok:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lastError), lastError, "Got invalid Everything error code.");
            }
        }

        private static bool Initialize()
        {
            SetInstanceName(ToolbarSettings.User.InstanceName);

            Version version = GetEverythingVersion();

            if (version.Major > 1 || version is { Major: 1, Minor: > 4 } || version is { Major: 1, Minor: 4, Build: >= 1 })
            {
                Logger.Info("Everything version: {major}.{minor}.{build}", version.Major, version.Minor, version.Build);
                return true;
            }

            if (version is { Major: 0, Minor: 0, Build: 0 } && (ErrorCode)Everything_GetLastError() == ErrorCode.ErrorIpc)
            {
                LogLastError();
                Logger.Error("Failed to get Everything version number.");
            }
            else
            {
                Logger.Error("Everything version {major}.{minor}.{build} is not supported.", version.Major, version.Minor, version.Build);
            }

            return false;
        }

        public static Version GetEverythingVersion()
        {
            uint major = Everything_GetMajorVersion();
            uint minor = Everything_GetMinorVersion();
            uint revision = Everything_GetRevision();
            return new Version((int)major, (int)minor, (int)revision);
        }

        public static void SetInstanceName(string name)
        {
            if (name != string.Empty)
                Logger.Info("Setting Everything instance name: " + name);

            Everything_SetInstanceName(name);
        }

        [Flags]
        private enum Flags : uint
        {
            FullPathAndFileName = 0x00000004,
            HighlightedFileName = 0x00002000,
            HighlightedPath = 0x00004000,
            RequestSize = 0x00000010,
            RequestDateModified = 0x00000040
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

        private static void SetSortType(int sortBy, bool descending)
        {
            var sortType = CalculateEverythingSortType(sortBy, descending);
            Everything_SetSort(sortType);
        }

        public static bool GetIsFastSort(int sortBy, bool descending)
        {
            var everythingSortType = CalculateEverythingSortType(sortBy, descending);
            return Everything_IsFastSort(everythingSortType);
        }

        public static void OpenSearchInEverything(SearchState searchState, string filenameToHighlight = "")
        {
            if (!File.Exists(ToolbarSettings.User.EverythingPath))
            {
                MessageBox.Show(Resources.MessageBoxSelectEverythingExe);

                using var openFileDialog = new OpenFileDialog();
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

            var searchTerm = searchState.Filter.GetSearchPrefix() + searchState.SearchTerm;
            var args = "";
            if (!string.IsNullOrEmpty(ToolbarSettings.User.InstanceName)) args += " -instance \"" + ToolbarSettings.User.InstanceName + "\"";
            if (!string.IsNullOrEmpty(filenameToHighlight)) args += " -select \"" + filenameToHighlight + "\"";
            if (searchState.SortBy == 0) args += " -sort \"Name\"";
            else if (searchState.SortBy == 1) args += " -sort \"Path\"";
            else if (searchState.SortBy == 2) args += " -sort \"Size\"";
            else if (searchState.SortBy == 3) args += " -sort \"Extension\"";
            else if (searchState.SortBy == 4) args += " -sort \"Type name\"";
            else if (searchState.SortBy == 5) args += " -sort \"Date created\"";
            else if (searchState.SortBy == 6) args += " -sort \"Date modified\"";
            else if (searchState.SortBy == 7) args += " -sort \"Attributes\"";
            else if (searchState.SortBy == 8) args += " -sort \"File list filename\"";
            else if (searchState.SortBy == 9) args += " -sort \"Run count\"";
            else if (searchState.SortBy == 10) args += " -sort \"Date recently changed\"";
            else if (searchState.SortBy == 11) args += " -sort \"Date accessed\"";
            else if (searchState.SortBy == 12) args += " -sort \"Date run\"";
            args += searchState.IsSortDescending ? " -sort-descending" : " -sort-ascending";
            args += searchState.IsMatchCase ? " -case" : " -nocase";
            args += searchState.IsMatchPath ? " -matchpath" : " -nomatchpath";
            args += searchState.IsMatchWholeWord && !searchState.IsRegExEnabled ? " -ww" : " -noww";
            args += searchState.IsRegExEnabled ? " -regex" : " -noregex";
            args += " -s \"" + searchTerm.Replace("\"", "\"\"") + "\"";

            Logger.Debug("Showing in Everything with args: " + args);
            Process.Start(ToolbarSettings.User.EverythingPath, args);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private enum QueryType : uint
        {
            Count = 0,
            Range = 1
        }

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
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetReplyWindow(IntPtr hwnd);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetReplyID(uint id);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_IsQueryReply(uint message, IntPtr wParam, IntPtr lParam, long nId);
    }
}