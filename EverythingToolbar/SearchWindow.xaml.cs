using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;

namespace EverythingToolbar
{
    public partial class SearchWindow
    {
        public static readonly SearchWindow Instance = new();
        public event EventHandler<EventArgs>? Hiding;
        public event EventHandler<EventArgs>? Hidden;
        public event EventHandler<EventArgs>? Showing;

        private bool _dwmFlushOnRender;
        private bool _isFirstShow = true;

        private SearchWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += OnCompositionTargetRendering;
            EventDispatcher.Instance.GlobalKeyEvent += OnPreviewKeyDown;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            if (TaskbarStateManager.Instance.IsIcon)
                EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);

            EventDispatcher.Instance.InvokeFocusRequested(this, EventArgs.Empty);
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is >= Key.D0 and <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                SearchState.Instance.SelectFilterFromIndex(index);
            }
            else if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                NativeMethods.FocusTaskbarWindow();
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
            SearchResultProvider.OpenSearchInEverything(SearchState.Instance);
        }

        public new void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            Hiding?.Invoke(this, EventArgs.Empty);
        }

        private void OnHidden(object? sender, EventArgs e)
        {
            if ((int)Height != ToolbarSettings.User.PopupHeight || (int)Width != ToolbarSettings.User.PopupWidth)
            {
                ToolbarSettings.User.PopupHeight = (int)Height;
                ToolbarSettings.User.PopupWidth = (int)Width;
            }

            // Push outside of screens to hide Windows' closing animation
            ClearAnimations();
            Top = 100000;
            Left = 100000;

            base.Hide();

            _dwmFlushOnRender = false;

            SearchState.Instance.Reset();
            
            // Fire Hidden event after animation is complete and window is actually hidden
            Hidden?.Invoke(this, EventArgs.Empty);
        }

        public new void Show()
        {
            var activate = TaskbarStateManager.Instance.IsIcon;

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

        private void ClearAnimations()
        {
            BeginAnimation(LeftProperty, null);
            BeginAnimation(TopProperty, null);
            BeginAnimation(OpacityProperty, null);
            ContentGrid.BeginAnimation(MarginProperty, null);
        }

        public void AnimateShow(double left, double top, double width, double height, Edge taskbarEdge)
        {
            // Clearing all animations allows us to set the corresponding properties again
            ClearAnimations();

            Width = width;
            Height = height;

            // Move window to correct secondary axis position
            var vertical = taskbarEdge is Edge.Left or Edge.Right;
            if (vertical)
                Top = top;
            else
                Left = left;

            SetTopmostBelowTaskbar();

            // Animate window along primary axis position
            if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                AnimateShowWin11(left, top, width, height, taskbarEdge);
            else
                AnimateShowWin10(left, top, taskbarEdge);
        }

        private void AnimateShowWin10(double left, double top, Edge taskbarEdge)
        {
            if (Utils.IsEffectiveAnimationsDisabled)
            {
                Opacity = 1;
                Left = left;
                Top = top;
                return;
            }

            DependencyProperty? property = null;
            double from = 0;
            double to = 0;
            switch (taskbarEdge)
            {
                case Edge.Left:
                    from = left - 150;
                    to = left;
                    property = LeftProperty;
                    break;
                case Edge.Right:
                    from = left + 150;
                    to = left;
                    property = LeftProperty;
                    break;
                case Edge.Top:
                    from = top - 150;
                    to = top;
                    property = TopProperty;
                    break;
                case Edge.Bottom:
                    from = top + 150;
                    to = top;
                    property = TopProperty;
                    break;
            }
            BeginAnimation(
                property,
                new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );

            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );

            var fromThickness = new Thickness(0);
            switch (taskbarEdge)
            {
                case Edge.Left:
                    fromThickness = new Thickness(-50, 0, 50, 0);
                    break;
                case Edge.Right:
                    fromThickness = new Thickness(50, 0, -50, 0);
                    break;
                case Edge.Top:
                    fromThickness = new Thickness(0, -50, 0, 50);
                    break;
                case Edge.Bottom:
                    fromThickness = new Thickness(0, 50, 0, -50);
                    break;
            }
            ContentGrid.BeginAnimation(
                MarginProperty,
                new ThicknessAnimation
                {
                    From = fromThickness,
                    To = new Thickness(0),
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );
        }

        private void AnimateShowWin11(double left, double top, double width, double height, Edge taskbarEdge)
        {
            if (Utils.IsEffectiveAnimationsDisabled)
            {
                Opacity = 1;
                Left = left;
                Top = top;
                return;
            }

            DependencyProperty? property = null;
            double from = 0;
            double to = 0;
            switch (taskbarEdge)
            {
                case Edge.Left:
                    from = left - width;
                    to = left;
                    property = LeftProperty;
                    break;
                case Edge.Right:
                    from = left + width;
                    to = left;
                    property = LeftProperty;
                    break;
                case Edge.Top:
                    from = top - height;
                    to = top;
                    property = TopProperty;
                    break;
                case Edge.Bottom:
                    from = top + height;
                    to = top;
                    property = TopProperty;
                    break;
            }
            BeginAnimation(
                property,
                new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );

            var fromThickness = new Thickness(0);
            switch (taskbarEdge)
            {
                case Edge.Top:
                    fromThickness = new Thickness(0, -50, 0, 50);
                    break;
                case Edge.Right:
                    fromThickness = new Thickness(50, 0, -50, 0);
                    break;
                case Edge.Bottom:
                    fromThickness = new Thickness(0, 50, 0, -50);
                    break;
                case Edge.Left:
                    fromThickness = new Thickness(-50, 0, 50, 0);
                    break;
            }
            ContentGrid.BeginAnimation(
                MarginProperty,
                new ThicknessAnimation
                {
                    From = fromThickness,
                    To = new Thickness(0),
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );
        }

        private void AnimateHideWin10(Edge taskbarEdge)
        {
            if (Utils.IsEffectiveAnimationsDisabled)
            {
                Dispatcher.BeginInvoke(() => OnHidden(this, EventArgs.Empty));
                return;
            }

            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(30),
                }
            );

            double target = 0;
            DependencyProperty? property = null;
            switch (taskbarEdge)
            {
                case Edge.Left:
                    target = RestoreBounds.Left - 150;
                    property = LeftProperty;
                    break;
                case Edge.Right:
                    target = RestoreBounds.Left + 150;
                    property = LeftProperty;
                    break;
                case Edge.Top:
                    target = RestoreBounds.Top - 150;
                    property = TopProperty;
                    break;
                case Edge.Bottom:
                    target = RestoreBounds.Top + 150;
                    property = TopProperty;
                    break;
            }
            var animation = new DoubleAnimation { To = target, Duration = TimeSpan.FromMilliseconds(30) };
            animation.Completed += OnHidden;
            BeginAnimation(property, animation);
        }

        private void AnimateHideWin11(Edge taskbarEdge)
        {
            if (Utils.IsEffectiveAnimationsDisabled)
            {
                Dispatcher.BeginInvoke(() => OnHidden(this, EventArgs.Empty));
                return;
            }

            DependencyProperty? property = null;
            double from = 0;
            double to = 0;
            double extraOffset = 50; // To include all possible window decorations
            switch (taskbarEdge)
            {
                case Edge.Left:
                    from = RestoreBounds.Left;
                    to = RestoreBounds.Left - Width - extraOffset;
                    property = LeftProperty;
                    break;
                case Edge.Right:
                    from = RestoreBounds.Left;
                    to = RestoreBounds.Left + Width + extraOffset;
                    property = LeftProperty;
                    break;
                case Edge.Top:
                    from = RestoreBounds.Top;
                    to = RestoreBounds.Top - Height - extraOffset;
                    property = TopProperty;
                    break;
                case Edge.Bottom:
                    from = RestoreBounds.Top;
                    to = RestoreBounds.Top + Height + extraOffset;
                    property = TopProperty;
                    break;
            }
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn, Power = 6 },
            };
            animation.Completed += OnHidden;
            BeginAnimation(property, animation);
        }

        public void AnimateHide(Edge taskbarEdge)
        {
            _dwmFlushOnRender = true;

            if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                AnimateHideWin11(taskbarEdge);
            else
                AnimateHideWin10(taskbarEdge);
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            if (_dwmFlushOnRender)
                NativeMethods.DwmFlush();
        }

        private void SetTopmostBelowTaskbar()
        {
            const int hwndTopmost = -1;

            const int swpNoactivate = 0x0010;
            const int swpShowwindow = 0x0040;
            const int swpNomove = 0x0002;
            const int swpNosize = 0x0001;

            const uint flags = swpNomove | swpNosize | swpNoactivate | swpShowwindow;

            var hwnd = new WindowInteropHelper(this).Handle;
            var taskbarHwnd = NativeMethods.FindTaskbarHandle();

            NativeMethods.SetWindowPos(hwnd, hwndTopmost, 0, 0, 0, 0, flags);

            // The taskbar should always be above the search window
            if (taskbarHwnd != IntPtr.Zero)
                NativeMethods.SetWindowPos(taskbarHwnd, hwndTopmost, 0, 0, 0, 0, flags);
        }
    }
}
