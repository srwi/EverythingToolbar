using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;
using NLog;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Platform.Search
{
    public sealed class EverythingPipeClient : IEverythingClient
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const int PathBufferLength = 4096;

        private const string AlphaInstanceName = "1.5a";

        // How long a query waits for the gate before rechecking whether it has been superseded.
        private static readonly TimeSpan GateWaitSlice = TimeSpan.FromMilliseconds(50);

        private const uint PropertyIdName = 0;
        private const uint PropertyIdPath = 1;
        private const uint PropertyIdSize = 2;
        private const uint PropertyIdDateModified = 5;
        private const uint PropertyIdPathAndName = 240;

        private readonly object _gate = new();
        private string _instanceName = string.Empty;
        private IntPtr _client;
        private IntPtr _resultList;

        private SearchQuery? _resultListQuery;
        private int _resultListOffset;

        public bool TryConnect()
        {
            // An established connection needs no lock. Taking _gate here would queue the caller
            // behind a running query, which holds it for the entire search.
            if (Volatile.Read(ref _client) != IntPtr.Zero)
                return true;

            lock (_gate)
            {
                return TryConnectLocked();
            }
        }

        private bool TryConnectLocked()
        {
            if (_client != IntPtr.Zero)
                return true;

            if (_instanceName.Length > 0)
            {
                _client = Everything3_ConnectW(_instanceName);
            }
            else
            {
                _client = Everything3_ConnectW(null);
                if (_client == IntPtr.Zero)
                    _client = Everything3_ConnectW(AlphaInstanceName);
            }

            return _client != IntPtr.Zero;
        }

        private void DisconnectLocked()
        {
            if (_resultList != IntPtr.Zero)
            {
                Everything3_DestroyResultList(_resultList);
                _resultList = IntPtr.Zero;
            }
            _resultListQuery = null;
            _resultListOffset = 0;

            if (_client != IntPtr.Zero)
            {
                Everything3_DestroyClient(_client);
                _client = IntPtr.Zero;
            }
        }

        public Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            return Task.Run(() => QueryCountSync(query, pageSize, cancellationToken), cancellationToken);
        }

        public int QueryCountSync(SearchQuery query, int pageSize, CancellationToken cancellationToken)
        {
            if (!TryEnterGate(cancellationToken))
                return 0;

            try
            {
                if (!ExecuteQueryLocked(query, pageSize, offset: 0))
                    return 0;

                return (int)Everything3_GetResultListCount(_resultList);
            }
            finally
            {
                Monitor.Exit(_gate);
            }
        }

        public Task<IList<SearchResult>> QueryRangeAsync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<IList<SearchResult>>(cancellationToken);

            return Task.Run(() => QueryRangeSync(query, startIndex, pageSize, cancellationToken), cancellationToken);
        }

        public IList<SearchResult> QueryRangeSync(
            SearchQuery query,
            int startIndex,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            if (!TryEnterGate(cancellationToken))
                return Array.Empty<SearchResult>();

            try
            {
                if (query == _resultListQuery)
                {
                    if (startIndex == _resultListOffset)
                        return ReadResultsFromResultListLocked();

                    if (TryMoveViewportLocked(query, pageSize, (uint)startIndex))
                        return ReadResultsFromResultListLocked();
                }

                if (!ExecuteQueryLocked(query, pageSize, (uint)startIndex))
                    return new List<SearchResult>();

                return ReadResultsFromResultListLocked();
            }
            finally
            {
                Monitor.Exit(_gate);
            }
        }

        private bool TryEnterGate(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!Monitor.TryEnter(_gate, GateWaitSlice))
                    continue;

                if (!cancellationToken.IsCancellationRequested)
                    return true;

                Monitor.Exit(_gate);
                break;
            }

            return false;
        }

        public bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> results)
        {
            if (!Monitor.TryEnter(_gate))
            {
                results = Array.Empty<SearchResult>();
                return false;
            }

            try
            {
                if (query != _resultListQuery || _resultListOffset != 0)
                {
                    results = Array.Empty<SearchResult>();
                    return false;
                }

                results = ReadResultsFromResultListLocked();
                return true;
            }
            finally
            {
                Monitor.Exit(_gate);
            }
        }

        private bool ExecuteQueryLocked(SearchQuery query, int pageSize, uint offset)
        {
            _resultListQuery = null;

            if (!TryConnectLocked())
                return false;

            var resultList = RunSearchLocked(query, pageSize, offset, reuseResultSet: false);

            if (resultList == IntPtr.Zero)
            {
                // The pipe may have broken since the last query (e.g. Everything restarted);
                // reconnect and retry once before giving up.
                Logger.Warn("Search failed with error 0x{error:X8}, reconnecting.", Everything3_GetLastError());
                DisconnectLocked();

                if (TryConnectLocked())
                    resultList = RunSearchLocked(query, pageSize, offset, reuseResultSet: false);

                if (resultList == IntPtr.Zero)
                {
                    Logger.Error("Search failed with error 0x{error:X8}.", Everything3_GetLastError());
                    DisconnectLocked();
                    return false;
                }
            }

            AdoptResultListLocked(resultList, query, (int)offset);
            return true;
        }

        private bool TryMoveViewportLocked(SearchQuery query, int pageSize, uint offset)
        {
            if (_client == IntPtr.Zero || query != _resultListQuery)
                return false;

            var resultList = RunSearchLocked(query, pageSize, offset, reuseResultSet: true);
            if (resultList == IntPtr.Zero)
            {
                // Everything no longer holds a result set for us; fall back to a full search.
                _resultListQuery = null;
                return false;
            }

            AdoptResultListLocked(resultList, query, (int)offset);
            return true;
        }

        private IntPtr RunSearchLocked(SearchQuery query, int pageSize, uint offset, bool reuseResultSet)
        {
            var searchState = Everything3_CreateSearchState();
            if (searchState == IntPtr.Zero)
            {
                Logger.Error("Failed to allocate the search state.");
                return IntPtr.Zero;
            }

            try
            {
                Everything3_SetSearchTextW(searchState, query.SearchText);
                Everything3_SetSearchMatchCase(searchState, query.MatchCase);
                Everything3_SetSearchMatchPath(searchState, query.MatchPath);
                Everything3_SetSearchMatchWholeWords(searchState, query is { MatchWholeWord: true, UseRegex: false });
                Everything3_SetSearchRegex(searchState, query.UseRegex);
                Everything3_AddSearchSort(searchState, ToPropertyId(query.SortBy), !query.SortDescending);
                Everything3_AddSearchPropertyRequest(searchState, PropertyIdPathAndName);
                Everything3_AddSearchPropertyRequestHighlighted(searchState, PropertyIdName);
                Everything3_AddSearchPropertyRequestHighlighted(searchState, PropertyIdPath);
                Everything3_AddSearchPropertyRequest(searchState, PropertyIdSize);
                Everything3_AddSearchPropertyRequest(searchState, PropertyIdDateModified);
                Everything3_SetSearchViewportOffset(searchState, offset);
                Everything3_SetSearchViewportCount(searchState, (nuint)pageSize);

                return reuseResultSet
                    ? Everything3_GetResults(_client, searchState)
                    : Everything3_Search(_client, searchState);
            }
            finally
            {
                Everything3_DestroySearchState(searchState);
            }
        }

        private void AdoptResultListLocked(IntPtr resultList, SearchQuery query, int offset)
        {
            if (_resultList != IntPtr.Zero)
                Everything3_DestroyResultList(_resultList);

            _resultList = resultList;
            _resultListQuery = query;
            _resultListOffset = offset;
        }

        private unsafe IList<SearchResult> ReadResultsFromResultListLocked()
        {
            if (_resultList == IntPtr.Zero)
                return new List<SearchResult>();

            var count = (int)Everything3_GetResultListViewportCount(_resultList);
            var results = new List<SearchResult>(count);
            char* buffer = stackalloc char[PathBufferLength];

            for (nuint i = 0; (int)i < count; i++)
            {
                var highlightedFileName = ReadTextProperty(i, PropertyIdName, buffer);
                var highlightedPath = ReadTextProperty(i, PropertyIdPath, buffer);
                var isFile = !Everything3_IsFolderResult(_resultList, i);

                var pathLength = Everything3_GetResultFullPathNameW(_resultList, i, buffer, PathBufferLength);
                var fullPathAndFileName = new string(buffer, 0, (int)Math.Min(pathLength, PathBufferLength - 1));

                var fileSize = Everything3_GetResultSize(_resultList, i);
                var dateModified = Everything3_GetResultDateModified(_resultList, i);

                results.Add(
                    new SearchResult(
                        highlightedPath,
                        highlightedFileName,
                        fullPathAndFileName,
                        isFile,
                        fileSize == ulong.MaxValue ? -1 : (long)fileSize,
                        ToFileTime(dateModified)
                    )
                );
            }
            return results;
        }

        private unsafe string ReadTextProperty(nuint index, uint propertyId, char* buffer)
        {
            var length = Everything3_GetResultPropertyTextHighlightedW(
                _resultList,
                index,
                propertyId,
                buffer,
                PathBufferLength
            );
            return new string(buffer, 0, (int)Math.Min(length, PathBufferLength - 1));
        }

        private static FILETIME ToFileTime(ulong fileTime)
        {
            // The SDK returns EVERYTHING3_UINT64_MAX when the property is unavailable.
            if (fileTime == ulong.MaxValue)
                fileTime = 0;

            return new FILETIME
            {
                dwLowDateTime = (int)(fileTime & 0xFFFFFFFF),
                dwHighDateTime = (int)(fileTime >> 32),
            };
        }

        private static uint ToPropertyId(SortBy sortBy) =>
            sortBy switch
            {
                SortBy.Name => 0,
                SortBy.Path => 1,
                SortBy.Size => 2,
                SortBy.Extension => 3,
                SortBy.TypeName => 4,
                SortBy.DateModified => 5,
                SortBy.DateCreated => 6,
                SortBy.DateAccessed => 7,
                SortBy.Attributes => 8,
                SortBy.DateRecentlyChanged => 9,
                SortBy.RunCount => 10,
                SortBy.DateRun => 11,
                SortBy.FileListFilename => 12,
                _ => 0,
            };

        public Version GetEverythingVersion()
        {
            lock (_gate)
            {
                if (!TryConnectLocked())
                    return new Version(0, 0, 0);

                return new Version(
                    (int)Everything3_GetMajorVersion(_client),
                    (int)Everything3_GetMinorVersion(_client),
                    (int)Everything3_GetRevision(_client)
                );
            }
        }

        public void SetInstanceName(string name)
        {
            lock (_gate)
            {
                if (name == _instanceName)
                    return;

                if (name != string.Empty)
                    Logger.Info("Setting Everything instance name: " + name);

                // The instance determines the pipe name, so the connection is rebuilt lazily.
                _instanceName = name;
                DisconnectLocked();
            }
        }

        public void IncrementRunCount(string path)
        {
            lock (_gate)
            {
                if (TryConnectLocked())
                    Everything3_IncRunCountFromFilenameW(_client, path);
            }
        }

        public bool GetIsFastSort(SortBy sortBy, bool descending)
        {
            lock (_gate)
            {
                return TryConnectLocked() && Everything3_IsPropertyFastSort(_client, ToPropertyId(sortBy));
            }
        }

        [DllImport("Everything3.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Everything3_ConnectW(string? lpInstanceName);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_DestroyClient(IntPtr client);

        [DllImport("Everything3.dll")]
        private static extern uint Everything3_GetLastError();

        [DllImport("Everything3.dll")]
        private static extern uint Everything3_GetMajorVersion(IntPtr client);

        [DllImport("Everything3.dll")]
        private static extern uint Everything3_GetMinorVersion(IntPtr client);

        [DllImport("Everything3.dll")]
        private static extern uint Everything3_GetRevision(IntPtr client);

        [DllImport("Everything3.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything3_IncRunCountFromFilenameW(IntPtr client, string lpFilename);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_IsPropertyFastSort(IntPtr client, uint propertyId);

        [DllImport("Everything3.dll")]
        private static extern IntPtr Everything3_CreateSearchState();

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_DestroySearchState(IntPtr searchState);

        [DllImport("Everything3.dll", CharSet = CharSet.Unicode)]
        private static extern bool Everything3_SetSearchTextW(IntPtr searchState, string lpSearchText);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchMatchCase(IntPtr searchState, bool matchCase);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchMatchPath(IntPtr searchState, bool matchPath);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchMatchWholeWords(IntPtr searchState, bool matchWholeWords);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchRegex(IntPtr searchState, bool matchRegex);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_AddSearchSort(IntPtr searchState, uint propertyId, bool ascending);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_AddSearchPropertyRequest(IntPtr searchState, uint propertyId);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_AddSearchPropertyRequestHighlighted(IntPtr searchState, uint propertyId);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchViewportOffset(IntPtr searchState, nuint offset);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_SetSearchViewportCount(IntPtr searchState, nuint count);

        [DllImport("Everything3.dll")]
        private static extern IntPtr Everything3_Search(IntPtr client, IntPtr searchState);

        [DllImport("Everything3.dll")]
        private static extern IntPtr Everything3_GetResults(IntPtr client, IntPtr searchState);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_DestroyResultList(IntPtr resultList);

        [DllImport("Everything3.dll")]
        private static extern nuint Everything3_GetResultListCount(IntPtr resultList);

        [DllImport("Everything3.dll")]
        private static extern nuint Everything3_GetResultListViewportCount(IntPtr resultList);

        [DllImport("Everything3.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe nuint Everything3_GetResultFullPathNameW(
            IntPtr resultList,
            nuint resultIndex,
            char* lpString,
            nuint maxCount
        );

        [DllImport("Everything3.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe nuint Everything3_GetResultPropertyTextHighlightedW(
            IntPtr resultList,
            nuint resultIndex,
            uint propertyId,
            char* lpString,
            nuint maxCount
        );

        [DllImport("Everything3.dll")]
        private static extern ulong Everything3_GetResultSize(IntPtr resultList, nuint resultIndex);

        [DllImport("Everything3.dll")]
        private static extern ulong Everything3_GetResultDateModified(IntPtr resultList, nuint resultIndex);

        [DllImport("Everything3.dll")]
        private static extern bool Everything3_IsFolderResult(IntPtr resultList, nuint resultIndex);
    }
}
