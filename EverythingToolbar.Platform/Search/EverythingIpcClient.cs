using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EverythingToolbar.Core.Data;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Platform.Search
{
    public sealed class EverythingIpcClient : IEverythingClient
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const int PathBufferLength = 4096;

        private readonly object _gate = new();
        private readonly Queue<PendingQuery> _queue = new();
        private SynchronizationContext? _synchronizationContext;
        private PendingQuery? _current;
        private uint _nextReplyId;
        private IntPtr _replyWindowHandle;
        private bool _initialized;

        // Keep the delegate alive so the GC can't collect it while native code holds the function pointer.
        private readonly WNDPROC _wndProc;

        // The count query's first page is left behind in the SDK's result list, so page 0 can be
        // read from it without a second IPC roundtrip.
        private SearchQuery? _resultListQuery;

        public EverythingIpcClient()
        {
            _wndProc = HandleWindowMessage;
        }

        // Captured lazily on first query since the client may be constructed before a WPF dispatcher exists.
        private SynchronizationContext GetSynchronizationContext()
        {
            if (_synchronizationContext == null)
            {
                var context =
                    SynchronizationContext.Current
                    ?? throw new InvalidOperationException(
                        "EverythingIpcClient must be used on a thread with a synchronization context."
                    );
                Interlocked.CompareExchange(ref _synchronizationContext, context, null);
            }

            return _synchronizationContext;
        }

        public Task<int> QueryCountAsync(SearchQuery query, int pageSize, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            lock (_gate)
            {
                EnsureInitialized();

                var pending = new PendingCountQuery(query, pageSize, NextReplyId(), cancellationToken);
                _queue.Enqueue(pending);
                GetSynchronizationContext().Post(_ => ProcessNextQuery(), null);
                return pending.CompletionSource.Task;
            }
        }

        public int QueryCountSync(SearchQuery query, int pageSize)
        {
            lock (_gate)
            {
                EnsureInitialized();

                if (!ExecuteQueryBlocking(query, pageSize, offset: 0))
                    return 0;

                _resultListQuery = query;
                return (int)Everything_GetTotResults();
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

            lock (_gate)
            {
                if (startIndex == 0 && query == _resultListQuery)
                    return Task.FromResult(ReadResultsFromResultList());

                var pending = new PendingRangeQuery(query, startIndex, pageSize, NextReplyId(), cancellationToken);
                _queue.Enqueue(pending);
                GetSynchronizationContext().Post(_ => ProcessNextQuery(), null);
                return pending.CompletionSource.Task;
            }
        }

        public IList<SearchResult> QueryRangeSync(SearchQuery query, int startIndex, int pageSize)
        {
            lock (_gate)
            {
                if (startIndex == 0 && query == _resultListQuery)
                    return ReadResultsFromResultList();

                if (!ExecuteQueryBlocking(query, pageSize, (uint)startIndex))
                    return new List<SearchResult>();

                return ReadResultsFromResultList();
            }
        }

        public bool TryReadCachedFirstPage(SearchQuery query, out IList<SearchResult> results)
        {
            lock (_gate)
            {
                if (query != _resultListQuery)
                {
                    results = Array.Empty<SearchResult>();
                    return false;
                }

                results = ReadResultsFromResultList();
                return true;
            }
        }

        private uint NextReplyId()
        {
            // Unique reply ID so a stale reply can never complete the wrong pending query.
            return ++_nextReplyId;
        }

        private void ProcessNextQuery()
        {
            lock (_gate)
            {
                if (_current != null)
                {
                    if (!_current.IsCanceled)
                        return;

                    // Abandon the canceled query so a missing reply can't block the queue.
                    _current.Dispose();
                    _current = null;
                }

                while (_queue.Count > 0)
                {
                    var next = _queue.Dequeue();
                    if (next.IsCanceled)
                    {
                        next.Dispose();
                        continue;
                    }

                    _current = next;
                    break;
                }

                if (_current == null)
                    return;

                ApplySearchParameters(_current.Query, _current.PageSize);
                Everything_SetOffset(_current.Offset);
                Everything_SetReplyID(_current.ReplyId);

                if (!ExecuteQuery(isAsync: true))
                {
                    _current.CompleteEmpty();
                    _current.Dispose();
                    _current = null;

                    GetSynchronizationContext().Post(_ => ProcessNextQuery(), null);
                }
            }
        }

        private LRESULT HandleWindowMessage(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            lock (_gate)
            {
                if (
                    _current != null
                    && Everything_IsQueryReply(
                        msg,
                        (IntPtr)(nuint)wParam,
                        (IntPtr)(nint)lParam,
                        _current.ReplyId
                    )
                )
                {
                    var completed = _current;
                    _current = null;

                    // RunContinuationsAsynchronously guarantees these can't run inline while _gate is held.
                    switch (completed)
                    {
                        case PendingCountQuery countQuery:
                            _resultListQuery = countQuery.Query;
                            countQuery.CompletionSource.TrySetResult((int)Everything_GetTotResults());
                            break;
                        case PendingRangeQuery rangeQuery:
                            rangeQuery.CompletionSource.TrySetResult(ReadResultsFromResultList());
                            break;
                    }

                    completed.Dispose();
                    GetSynchronizationContext().Post(_ => ProcessNextQuery(), null);
                    return (LRESULT)1;
                }
            }

            return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void ApplySearchParameters(SearchQuery query, int pageSize)
        {
            Everything_SetSearchW(query.SearchText);
            Everything_SetRequestFlags(
                (uint)(
                    RequestFlags.FullPathAndFileName
                    | RequestFlags.HighlightedPath
                    | RequestFlags.HighlightedFileName
                    | RequestFlags.RequestSize
                    | RequestFlags.RequestDateModified
                )
            );
            Everything_SetSort(query.SortBy.ToEverythingSortType(query.SortDescending));
            Everything_SetMatchCase(query.MatchCase);
            Everything_SetMatchPath(query.MatchPath);
            Everything_SetMatchWholeWord(query is { MatchWholeWord: true, UseRegex: false });
            Everything_SetRegex(query.UseRegex);
            Everything_SetMax((uint)pageSize);
        }

        private bool ExecuteQueryBlocking(SearchQuery query, int pageSize, uint offset)
        {
            ApplySearchParameters(query, pageSize);
            Everything_SetOffset(offset);
            return ExecuteQuery(isAsync: false);
        }

        private bool ExecuteQuery(bool isAsync)
        {
            if (isAsync)
                EnsureReplyWindow();

            _resultListQuery = null;

            if (!Everything_QueryW(!isAsync))
            {
                LogLastError();
                return false;
            }

            return true;
        }

        private unsafe void EnsureReplyWindow()
        {
            if (_replyWindowHandle == IntPtr.Zero)
            {
                // Create a message-only window to receive IPC messages
                _replyWindowHandle = PInvoke.CreateWindowEx(
                    0,
                    "STATIC",
                    null,
                    0,
                    0,
                    0,
                    0,
                    0,
                    (HWND)(-3),
                    null,
                    null,
                    null
                );

                if (_replyWindowHandle != IntPtr.Zero)
                {
                    PInvoke.SetWindowLongPtr(
                        (HWND)_replyWindowHandle,
                        WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC,
                        (nint)Marshal.GetFunctionPointerForDelegate(_wndProc)
                    );
                }
                else
                {
                    Logger.Error("Failed to create IPC response window.");
                    return;
                }
            }

            Everything_SetReplyWindow(_replyWindowHandle);
        }

        private static unsafe IList<SearchResult> ReadResultsFromResultList()
        {
            var count = Everything_GetNumResults();
            var results = new List<SearchResult>((int)count);
            char* fullPathAndFilename = stackalloc char[PathBufferLength];

            for (uint i = 0; i < count; i++)
            {
                var highlightedPath = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i));
                var highlightedFileName = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i));
                var isFile = Everything_IsFileResult(i);
                var pathLength = Everything_GetResultFullPathNameW(i, fullPathAndFilename, PathBufferLength);
                Everything_GetResultSize(i, out var fileSize);
                Everything_GetResultDateModified(i, out var dateModified);
                results.Add(
                    new SearchResult(
                        highlightedPath ?? "<invalid>",
                        highlightedFileName ?? "<invalid>",
                        new string(fullPathAndFilename, 0, (int)Math.Min(pathLength, PathBufferLength - 1)),
                        isFile,
                        fileSize,
                        dateModified
                    )
                );
            }
            return results;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = Initialize();
        }

        private bool Initialize()
        {
            Version version = GetEverythingVersion();

            if (
                version.Major > 1
                || version is { Major: 1, Minor: > 4 }
                || version is { Major: 1, Minor: 4, Build: >= 1 }
            )
            {
                Logger.Info("Everything version: {major}.{minor}.{build}", version.Major, version.Minor, version.Build);
                return true;
            }

            if (
                version is { Major: 0, Minor: 0, Build: 0 }
                && (ErrorCode)Everything_GetLastError() == ErrorCode.ErrorIpc
            )
            {
                LogLastError();
                Logger.Error("Failed to get Everything version number.");
            }
            else
            {
                Logger.Error(
                    "Everything version {major}.{minor}.{build} is not supported.",
                    version.Major,
                    version.Minor,
                    version.Build
                );
            }

            return false;
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
                    Logger.Error(
                        "IPC is not available. Is Everything running? If not, go to www.voidtools.com and download Everything."
                    );
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
                    throw new ArgumentOutOfRangeException(
                        nameof(lastError),
                        lastError,
                        "Got invalid Everything error code."
                    );
            }
        }

        public Version GetEverythingVersion()
        {
            uint major = Everything_GetMajorVersion();
            uint minor = Everything_GetMinorVersion();
            uint revision = Everything_GetRevision();
            return new Version((int)major, (int)minor, (int)revision);
        }

        public void SetInstanceName(string name)
        {
            if (name != string.Empty)
                Logger.Info("Setting Everything instance name: " + name);

            Everything_SetInstanceName(name);
        }

        public void IncrementRunCount(string path)
        {
            Everything_IncRunCountFromFileName(path);
        }

        public bool GetIsFastSort(SortBy sortBy, bool descending)
        {
            var everythingSortType = sortBy.ToEverythingSortType(descending);
            return Everything_IsFastSort(everythingSortType);
        }

        private abstract class PendingQuery : IDisposable
        {
            private readonly CancellationToken _cancellationToken;
            private readonly CancellationTokenRegistration _cancellationRegistration;

            protected PendingQuery(
                SearchQuery query,
                int pageSize,
                uint offset,
                uint replyId,
                CancellationToken cancellationToken
            )
            {
                Query = query;
                PageSize = pageSize;
                Offset = offset;
                ReplyId = replyId;
                _cancellationToken = cancellationToken;

                // Last statement of the base constructor: the completion sources it cancels have already run.
                _cancellationRegistration = cancellationToken.Register(CancelCompletionSource);
            }

            public SearchQuery Query { get; }
            public int PageSize { get; }
            public uint Offset { get; }
            public uint ReplyId { get; }
            public bool IsCanceled => _cancellationToken.IsCancellationRequested;

            protected CancellationToken CancellationToken => _cancellationToken;

            protected abstract void CancelCompletionSource();
            public abstract void CompleteEmpty();

            public void Dispose() => _cancellationRegistration.Dispose();
        }

        private sealed class PendingCountQuery(
            SearchQuery query,
            int pageSize,
            uint replyId,
            CancellationToken cancellationToken
        ) : PendingQuery(query, pageSize, offset: 0, replyId, cancellationToken)
        {
            public TaskCompletionSource<int> CompletionSource { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            protected override void CancelCompletionSource() => CompletionSource.TrySetCanceled(CancellationToken);

            public override void CompleteEmpty() => CompletionSource.TrySetResult(0);
        }

        private sealed class PendingRangeQuery(
            SearchQuery query,
            int startIndex,
            int pageSize,
            uint replyId,
            CancellationToken cancellationToken
        ) : PendingQuery(query, pageSize, (uint)startIndex, replyId, cancellationToken)
        {
            public TaskCompletionSource<IList<SearchResult>> CompletionSource { get; } =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            protected override void CancelCompletionSource() => CompletionSource.TrySetCanceled(CancellationToken);

            public override void CompleteEmpty() => CompletionSource.TrySetResult(new List<SearchResult>());
        }

        [Flags]
        private enum RequestFlags : uint
        {
            FullPathAndFileName = 0x00000004,
            HighlightedFileName = 0x00002000,
            HighlightedPath = 0x00004000,
            RequestSize = 0x00000010,
            RequestDateModified = 0x00000040,
        }

        private enum ErrorCode
        {
            Ok,
            ErrorMemory,
            ErrorIpc,
            ErrorRegisterClassEx,
            ErrorCreateWindow,
            ErrorCreateThread,
            ErrorInvalidIndex,
            ErrorInvalidCall,
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
        private static extern unsafe uint Everything_GetResultFullPathNameW(
            uint nIndex,
            char* lpString,
            uint nMaxCount
        );

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetSort(uint dwSortType);

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint dwRequestFlags);

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr Everything_GetResultHighlightedFileName(uint nIndex);

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
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
