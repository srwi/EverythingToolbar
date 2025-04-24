using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Search
{
    public class SearchResultProvider : IItemsProvider<SearchResult>
    {
        private readonly SearchState _searchState;
        private bool _firstPageQueried;
        private static bool _initialized;

        public SearchResultProvider(SearchState searchState)
        {
            _searchState = searchState;

            if (!_initialized)
                _initialized = Initialize();
        }

        public int FetchCount(int pageSize)
        {
            if (ToolbarSettings.User.IsHideEmptySearchResults && string.IsNullOrEmpty(_searchState.SearchTerm))
                return 0;

            var search = _searchState.Filter.GetSearchPrefix() + _searchState.SearchTerm;
            Everything_SetSearchW(search);

            const Flags flags = Flags.FullPathAndFileName | Flags.HighlightedPath |
                               Flags.HighlightedFileName | Flags.RequestSize |
                               Flags.RequestDateModified;
            Everything_SetRequestFlags((uint)flags);

            SetSortType(_searchState.SortBy, _searchState.IsSortDescending);
            Everything_SetMatchCase(_searchState.IsMatchCase);
            Everything_SetMatchPath(_searchState.IsMatchPath);
            Everything_SetMatchWholeWord(_searchState.IsMatchWholeWord && !_searchState.IsRegExEnabled);
            Everything_SetRegex(_searchState.IsRegExEnabled);

            // First query is required to get the correct number of results.
            Everything_SetMax((uint)pageSize);
            Everything_SetOffset(0);
            if (!Everything_QueryW(true))
            {
                var lastError = (ErrorCode)Everything_GetLastError();
                LogError(lastError);
                return 0;
            }

            // We remember the first page query to avoid querying it again when actually fetching data
            _firstPageQueried = true;

            return (int)Everything_GetTotResults();
        }

        public IList<SearchResult> FetchRange(int startIndex, int pageSize)
        {
            // We can skip querying the first page again
            if (!_firstPageQueried || startIndex > 0)
            {
                _firstPageQueried = false;
                Everything_SetOffset((uint)startIndex);
                Everything_SetMax((uint)pageSize);
                if (!Everything_QueryW(true))
                {
                    var lastError = (ErrorCode)Everything_GetLastError();
                    LogError(lastError);
                    return new List<SearchResult>();
                }
            }


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

        private static bool Initialize()
        {
            SetInstanceName(ToolbarSettings.User.InstanceName);

            var major = Everything_GetMajorVersion();
            var minor = Everything_GetMinorVersion();
            var revision = Everything_GetRevision();

            if (major > 1 || (major == 1 && minor > 4) || (major == 1 && minor == 4 && revision >= 1))
            {
                Logger.Info("Everything version: {major}.{minor}.{revision}", major, minor, revision);
                return true;
            }

            if (major == 0 && minor == 0 && revision == 0 && (ErrorCode)Everything_GetLastError() == ErrorCode.ErrorIpc)
            {
                LogError((ErrorCode)Everything_GetLastError());
                Logger.Error("Failed to get Everything version number.");
            }
            else
            {
                Logger.Error("Everything version {major}.{minor}.{revision} is not supported.", major, minor, revision);
            }

            return false;
        }

        public static void SetInstanceName(string name)
        {
            if (name != string.Empty)
                Logger.Info("Setting Everything instance name: " + name);

            Everything_SetInstanceName(name);
        }

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchResultProvider>();

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

        private static void LogError(ErrorCode code)
        {
            switch (code)
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
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
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
    }
}