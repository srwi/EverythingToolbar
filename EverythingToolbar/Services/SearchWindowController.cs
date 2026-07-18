using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using NLog;

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

        private Func<bool>? _toolbarBoxIsFocused;
        private Action? _toolbarBoxFocus;

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

                if (!_structuralIconMode)
                    SetTemporaryPopupMode(true);

                ShowInternal(atCursor: true);
            });

        public void FocusSearchBox() =>
            RunOnUi(() =>
            {
                if (IsIconMode)
                    Window.FocusSearchBox();
                else
                    _toolbarBoxFocus?.Invoke();
            });

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

        private void ShowInternal(bool atCursor)
        {
            Window.Show(new ShowOptions(IsIconMode, atCursor));
            _state = WindowState.Visible;
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
            Showing?.Invoke(this, EventArgs.Empty);
        }

        private void OnWindowHidden(object? sender, EventArgs e)
        {
            _state = WindowState.Hidden;
            SetTemporaryPopupMode(false);
            Hidden?.Invoke(this, EventArgs.Empty);
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
