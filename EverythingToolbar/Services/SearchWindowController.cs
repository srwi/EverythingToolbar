using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar.Services
{
    public sealed class SearchWindowController : ObservableObject, ISearchWindowController
    {
        private enum WindowState
        {
            Hidden,
            Visible,
            HidingAnimation,
        }

        private const double DebounceMs = 500;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchWindowController>();

        private SearchWindow? _window;
        private WindowState _state = WindowState.Hidden;
        private bool _structuralIconMode;
        private bool _temporaryPopupMode;
        private DateTime _lastHideStart = DateTime.MinValue;

        private WINEVENTPROC? _foregroundCallback;
        private HWINEVENTHOOK _foregroundHook;

        private Func<bool>? _toolbarBoxIsFocused;
        private Action? _toolbarBoxFocus;
        private Action? _focusSelectedResult;

        public event EventHandler? Showing;
        public event EventHandler? Hiding;
        public event EventHandler? Hidden;
        public event EventHandler<bool>? ActiveChanged;
        public event EventHandler? SearchBoxFocused;

        public bool IsIconMode => _structuralIconMode || _temporaryPopupMode;

        private SearchWindow Window
        {
            get
            {
                if (_window == null)
                {
                    _window = Ioc.Default.GetRequiredService<SearchWindow>();
                    _window.Activated += OnWindowActivated;
                    _window.Deactivated += OnWindowDeactivated;
                    _window.Showing += OnWindowShowing;
                    _window.Hidden += OnWindowHidden;
                }

                return _window;
            }
        }

        public void SetIconMode(bool isIcon)
        {
            if (_structuralIconMode == isIcon)
                return;

            _structuralIconMode = isIcon;
            OnPropertyChanged(nameof(IsIconMode));
        }

        public void Show() => RunOnUi(() => ShowInternal(atCursor: false));

        public void Hide() => RunOnUi(HideInternal);

        public void Toggle() => RunOnUi(ToggleInternal);

        public void Dismiss() =>
            RunOnUi(() =>
            {
                NativeMethods.FocusTaskbarWindow();
                HideInternal();
            });

        public void ToggleSearchUi() =>
            RunOnUi(() =>
            {
                if (IsIconMode)
                    ToggleInternal();
                else if (_toolbarBoxIsFocused?.Invoke() == true)
                    HideInternal();
                else
                    _toolbarBoxFocus?.Invoke();
            });

        public void TogglePopupAtCursor() =>
            RunOnUi(() =>
            {
                if (_state == WindowState.Visible)
                {
                    HideInternal();
                    return;
                }

                // Ignore a toggle arriving right after a hide (e.g. clicking the icon to close reopens otherwise).
                if ((DateTime.Now - _lastHideStart).TotalMilliseconds < DebounceMs)
                    return;

                ShowStandaloneInternal();
            });

        public void ShowStandalone() => RunOnUi(ShowStandaloneInternal);

        public void FocusSearchBox() =>
            RunOnUi(() =>
            {
                if (IsIconMode)
                    Window.FocusSearchBox();
                else
                    _toolbarBoxFocus?.Invoke();
            });

        public void FocusSelectedResult() => RunOnUi(() => _focusSelectedResult?.Invoke());

        public void PreWarm() => RunOnUi(() => Window.PreWarm());

        public void NotifyFocusLostToOutside() =>
            RunOnUi(() =>
            {
                // Keyboard focus leaving the window reports NewFocus == null even when it moves to our own
                // attached toolbar box, which lives in a separate top-level window. Defer the hide so focus
                // can settle (the box's GotKeyboardFocus runs right after this), then skip it if focus landed
                // in the toolbar box — the user is still interacting with us, so the window must stay open.
                Window.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        if (_toolbarBoxIsFocused?.Invoke() == true)
                            return;

                        HideInternal();
                    }),
                    DispatcherPriority.Input
                );
            });

        public void NotifySearchBoxFocused() => SearchBoxFocused?.Invoke(this, EventArgs.Empty);

        public void RegisterToolbarSearchBox(Func<bool> isFocused, Action focus)
        {
            if (_toolbarBoxFocus != null)
                Logger.Warn("A toolbar search box is already registered; overwriting.");

            _toolbarBoxIsFocused = isFocused;
            _toolbarBoxFocus = focus;
        }

        public void UnregisterToolbarSearchBox(Action focus)
        {
            if (!ReferenceEquals(_toolbarBoxFocus, focus))
                return;

            _toolbarBoxIsFocused = null;
            _toolbarBoxFocus = null;
        }

        public void RegisterResultsList(Action focusSelected)
        {
            if (_focusSelectedResult != null)
                Logger.Warn("A results list is already registered; overwriting.");

            _focusSelectedResult = focusSelected;
        }

        public void UnregisterResultsList(Action focusSelected)
        {
            if (ReferenceEquals(_focusSelectedResult, focusSelected))
                _focusSelectedResult = null;
        }

        private void ShowInternal(bool atCursor)
        {
            Window.Show(new ShowOptions(IsIconMode, atCursor));
            _state = WindowState.Visible;
        }

        private void ShowStandaloneInternal()
        {
            if (!_structuralIconMode)
                SetTemporaryPopupMode(true);

            ShowInternal(atCursor: true);
        }

        private void HideInternal()
        {
            if (_state != WindowState.Visible)
                return;

            _state = WindowState.HidingAnimation;
            _lastHideStart = DateTime.Now;
            Window.HideAnimated();
            Hiding?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleInternal()
        {
            if (_state == WindowState.Hidden)
                ShowInternal(atCursor: false);
            else
                HideInternal();
        }

        private void SetTemporaryPopupMode(bool value)
        {
            if (_temporaryPopupMode == value)
                return;

            _temporaryPopupMode = value;
            OnPropertyChanged(nameof(IsIconMode));
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            ActiveChanged?.Invoke(this, true);

            if (IsIconMode)
                FocusSearchBox();
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            ActiveChanged?.Invoke(this, false);
        }

        private void OnWindowShowing(object? sender, ShowingEventArgs e)
        {
            WatchForegroundWindow();
            Showing?.Invoke(this, EventArgs.Empty);
        }

        private void OnWindowHidden(object? sender, EventArgs e)
        {
            StopWatchingForegroundWindow();
            _state = WindowState.Hidden;
            SetTemporaryPopupMode(false);
            Hidden?.Invoke(this, EventArgs.Empty);
        }

        // Closing on lost keyboard focus only works if the search UI actually got focus. When it didn't
        // (e.g. SetForegroundWindow was refused, or focus stayed in the settings window), clicking elsewhere
        // never closed it. Closing whenever another window comes to the foreground covers those cases.
        private void WatchForegroundWindow()
        {
            if (_foregroundCallback != null)
                return;

            _foregroundCallback = OnForegroundWindowChanged;
            _foregroundHook = PInvoke.SetWinEventHook(
                PInvoke.EVENT_SYSTEM_FOREGROUND,
                PInvoke.EVENT_SYSTEM_FOREGROUND,
                default(HMODULE),
                _foregroundCallback,
                0,
                0,
                PInvoke.WINEVENT_OUTOFCONTEXT
            );
        }

        private void StopWatchingForegroundWindow()
        {
            if (_foregroundCallback == null)
                return;

            PInvoke.UnhookWinEvent(_foregroundHook);
            _foregroundHook = default;
            _foregroundCallback = null;
        }

        private void OnForegroundWindowChanged(
            HWINEVENTHOOK hWinEventHook,
            uint eventType,
            HWND hwnd,
            int idObject,
            int idChild,
            uint idEventThread,
            uint dwmsEventTime
        )
        {
            if (_state == WindowState.Visible && !BelongsToSearchUi(hwnd))
                HideInternal();
        }

        private static bool BelongsToSearchUi(HWND hwnd)
        {
            if (hwnd.IsNull)
                return true;

            var root = PInvoke.GetAncestor(hwnd, GET_ANCESTOR_FLAGS.GA_ROOT);

            // The taskbar hosts the search box. Focus changes there are already handled by the box itself.
            if (root == (HWND)NativeMethods.FindTaskbarHandle())
                return true;

            // Our own windows, like the settings or a message box, keep it open, as before.
            NativeMethods.GetWindowThreadProcessId(root, out uint processId);
            return processId == Environment.ProcessId;
        }

        private void RunOnUi(Action action)
        {
            var dispatcher = Window.Dispatcher;
            if (dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(action);
        }
    }
}
