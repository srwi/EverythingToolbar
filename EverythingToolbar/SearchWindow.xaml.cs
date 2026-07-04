using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
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
        private bool _isRenderingHooked;
        private readonly SearchState _searchState = Ioc.Default.GetRequiredService<SearchState>();
        private readonly TaskbarStateManager _taskbarState = Ioc.Default.GetRequiredService<TaskbarStateManager>();
        private readonly EverythingSearchLauncher _launcher = Ioc.Default.GetRequiredService<EverythingSearchLauncher>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();

        public SearchWindow()
        {
            InitializeComponent();

            Deactivated += (_, _) => WeakReferenceMessenger.Default.Send(new SearchWindowActiveChanged(false));
        }

        // Forces a render every frame, so only hook this while the hide animation needs DwmFlush sync.
        private void HookRendering()
        {
            if (_isRenderingHooked)
                return;

            _isRenderingHooked = true;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void UnhookRendering()
        {
            if (!_isRenderingHooked)
                return;

            _isRenderingHooked = false;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new SearchWindowActiveChanged(true));

            if (_taskbarState.IsIcon)
                WeakReferenceMessenger.Default.Send(new FocusSearchBoxRequest());
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.Space)
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
            _launcher.OpenSearchInEverything(_searchState);
        }

        public new void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            Hiding?.Invoke(this, EventArgs.Empty);
            WeakReferenceMessenger.Default.Send(new SearchWindowHidingMessage());
        }

        private void OnHidden(object? sender, EventArgs e)
        {
            if ((int)Height != _settings.PopupHeight || (int)Width != _settings.PopupWidth)
            {
                _settings.PopupHeight = (int)Height;
                _settings.PopupWidth = (int)Width;
            }

            // Push outside of screens to hide Windows' closing animation
            ClearAnimations();
            Top = 100000;
            Left = 100000;

            base.Hide();

            UnhookRendering();

            _searchState.Reset();

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
            var activate = _taskbarState.IsIcon;

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
            // A running hide animation's Completed handler (OnHidden) may never fire if we interrupt it; unhook here.
            UnhookRendering();

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
            if (_windowsPolicy.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                AnimateShowWin11(left, top, width, height, taskbarEdge);
            else
                AnimateShowWin10(left, top, taskbarEdge);
        }

        private void AnimateShowWin10(double left, double top, Edge taskbarEdge)
        {
            if (_windowsPolicy.IsEffectiveAnimationsDisabled)
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
            if (_windowsPolicy.IsEffectiveAnimationsDisabled)
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
            if (_windowsPolicy.IsEffectiveAnimationsDisabled)
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
            if (_windowsPolicy.IsEffectiveAnimationsDisabled)
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
            HookRendering();

            if (_windowsPolicy.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                AnimateHideWin11(taskbarEdge);
            else
                AnimateHideWin10(taskbarEdge);
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            NativeMethods.DwmFlush();
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