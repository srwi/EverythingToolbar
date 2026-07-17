using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Controls;
using EverythingToolbar.ViewModels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar
{
    public partial class SearchWindow
    {
        public event EventHandler<EventArgs>? Hiding;
        public event EventHandler<EventArgs>? Hidden;
        public event EventHandler<EventArgs>? Showing;

        private bool _isFirstShow = true;
        private readonly SearchWindowViewModel _viewModel;
        private readonly SearchWindowAnimator _animator;

        public SearchWindow(SearchWindowViewModel viewModel)
            : base(viewModel.ThemeService, viewModel.WindowsPolicyService)
        {
            _viewModel = viewModel;
            InitializeComponent();

            _animator = new SearchWindowAnimator(
                this,
                ContentGrid,
                SetTopmostBelowTaskbar,
                OnHidden,
                () => _viewModel.AnimationsDisabled,
                _viewModel.IsWindows11OrGreater);

            Deactivated += (_, _) => WeakReferenceMessenger.Default.Send(new SearchWindowActiveChanged(false));
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SearchWindowActiveChanged(true));

            if (_viewModel.ShouldActivateOnShow)
                WeakReferenceMessenger.Default.Send(new FocusSearchBoxRequest());
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is >= Key.D0 and <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                _viewModel.SelectFilterFromIndex(index);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                NativeMethods.FocusTaskbarWindow();
                Hide();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void OnLostKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null) // New focus outside application
            {
                Hide();
            }
        }

        private void OpenSearchInEverything(object? sender, RoutedEventArgs e)
        {
            _viewModel.OpenSearchInEverything();
        }

        public new void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            Hiding?.Invoke(this, EventArgs.Empty);
            WeakReferenceMessenger.Default.Send(new SearchWindowHidingMessage());
        }

        private void OnHidden()
        {
            _viewModel.SavePopupSize((int)Width, (int)Height);

            // Push outside of screens to hide Windows' closing animation
            _animator.ClearAnimations();
            Top = 100000;
            Left = 100000;

            base.Hide();

            _animator.UnhookRendering();

            _viewModel.ResetSearch();

            Hidden?.Invoke(this, EventArgs.Empty);
        }

        public void PreWarm()
        {
            if (!_isFirstShow || Visibility == Visibility.Visible)
                return;

            _isFirstShow = false;

            // Park off-screen so the warm-up show can never flash on screen.
            Top = 100000;
            Left = 100000;

            ShowActivated = false;
            base.Show(); // Intentionally without firing Showing - no placement, no animation
            UpdateLayout();
            base.Hide(); // Intentionally without firing Hiding - no animation, no state reset
        }

        public new void Show()
        {
            var activate = _viewModel.ShouldActivateOnShow;

            if (Visibility == Visibility.Visible)
            {
                if (activate)
                    ActivateAndBringToFront();

                return;
            }

            ShowActivated = activate;
            base.Show();

            if (activate)
            {
                Dispatcher.BeginInvoke(new Action(ActivateAndBringToFront), DispatcherPriority.Input);
            }

            // For first show we ensure the UI is fully rendered
            if (_isFirstShow)
            {
                _isFirstShow = false;
                UpdateLayout();
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        Showing?.Invoke(this, EventArgs.Empty);
                    }),
                    DispatcherPriority.Loaded
                );
            }
            else
            {
                Showing?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ActivateAndBringToFront()
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            Activate();
            NativeMethods.ForciblySetForegroundWindow(hwnd);
        }

        public void Toggle()
        {
            if (Visibility == Visibility.Visible)
                Hide();
            else
                Show();
        }

        public void AnimateShow(double left, double top, double width, double height, Edge taskbarEdge)
        {
            _animator.AnimateShow(left, top, width, height, taskbarEdge);
        }

        public void AnimateHide(Edge taskbarEdge)
        {
            _animator.AnimateHide(taskbarEdge);
        }

        private void SetTopmostBelowTaskbar()
        {
            const int hwndTopmost = -1;

            const SET_WINDOW_POS_FLAGS flags =
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;

            var hwnd = new WindowInteropHelper(this).Handle;
            var taskbarHwnd = NativeMethods.FindTaskbarHandle();

            PInvoke.SetWindowPos((HWND)hwnd, (HWND)(IntPtr)hwndTopmost, 0, 0, 0, 0, flags);

            // The taskbar should always be above the search window
            if (taskbarHwnd != IntPtr.Zero)
                PInvoke.SetWindowPos((HWND)taskbarHwnd, (HWND)(IntPtr)hwndTopmost, 0, 0, 0, 0, flags);
        }
    }
}
