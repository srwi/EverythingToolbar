using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;

namespace EverythingToolbar.Services
{
    public sealed class SearchWindowController : ISearchWindowController
    {
        private enum WindowState
        {
            Hidden,
            Visible,
            HidingAnimation,
        }

        private readonly TaskbarStateService _taskbarState;
        private SearchWindow? _window;
        private WindowState _state = WindowState.Hidden;

        public event EventHandler? Showing;
        public event EventHandler? Hiding;
        public event EventHandler? Hidden;
        public event EventHandler<bool>? ActiveChanged;
        public event EventHandler? SearchBoxFocused;

        public SearchWindowController(TaskbarStateService taskbarState)
        {
            _taskbarState = taskbarState;
        }

        private bool IconMode => _taskbarState.IsIcon;

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

        public void Show() => RunOnUi(() => ShowInternal(atCursor: false));

        // Interim entry point for the launcher's popup-at-cursor toggle; folded into TogglePopupAtCursor in phase 3.
        public void ShowAtCursor() => RunOnUi(() => ShowInternal(atCursor: true));

        public void Hide() => RunOnUi(HideInternal);

        public void Toggle() => RunOnUi(() =>
        {
            if (_state == WindowState.Hidden)
                ShowInternal(atCursor: false);
            else
                HideInternal();
        });

        public void Dismiss() => RunOnUi(() =>
        {
            NativeMethods.FocusTaskbarWindow();
            HideInternal();
        });

        public void FocusSearchBox() => RunOnUi(() => WeakReferenceMessenger.Default.Send(new FocusSearchBoxRequest()));

        public void PreWarm() => RunOnUi(() => Window.PreWarm());

        public void NotifyFocusLostToOutside() => RunOnUi(HideInternal);

        public void NotifySearchBoxFocused() => SearchBoxFocused?.Invoke(this, EventArgs.Empty);

        private void ShowInternal(bool atCursor)
        {
            Window.Show(new ShowOptions(IconMode, atCursor));
            _state = WindowState.Visible;
        }

        private void HideInternal()
        {
            if (_state != WindowState.Visible)
                return;

            _state = WindowState.HidingAnimation;
            Window.HideAnimated();
            Hiding?.Invoke(this, EventArgs.Empty);
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            ActiveChanged?.Invoke(this, true);

            if (IconMode)
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
