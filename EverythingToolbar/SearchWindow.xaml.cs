using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shell;

namespace EverythingToolbar
{
    public partial class SearchWindow
    {
        public static readonly SearchWindow Instance = new SearchWindow();
        public event EventHandler<EventArgs> Hiding;
        public event EventHandler<EventArgs> Showing;

        private const int DropShadowBlurRadius = 12;

        private bool _dwmFlushOnRender;

        private SearchWindow()
        {
            InitializeComponent();

            if (Utils.GetWindowsVersion() < Utils.WindowsVersion.Windows11)
            {
                AllowsTransparency = true;
                Loaded += (s, e) =>
                {
                    WindowChrome.SetWindowChrome(this, new WindowChrome
                    {
                        ResizeBorderThickness = new Thickness(DropShadowBlurRadius + 3),
                        CaptionHeight = 0
                    });
                };
            }

            CompositionTarget.Rendering += OnCompositionTargetRendering;
            EventDispatcher.Instance.GlobalKeyEvent += OnPreviewKeyDown;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            if (TaskbarStateManager.Instance.IsIcon)
                EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);

            EventDispatcher.Instance.InvokeFocusRequested(this, EventArgs.Empty);

            SetTopmostBelowTaskbar();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
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

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null)  // New focus outside application
            {
                Hide();
            }
        }

        private void OpenSearchInEverything(object sender, RoutedEventArgs e)
        {
            SearchResultProvider.OpenSearchInEverything(SearchState.Instance);
        }

        public new void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            Hiding?.Invoke(this, EventArgs.Empty);
        }

        private void OnHidden(object sender, EventArgs e)
        {
            if ((int)Height != ToolbarSettings.User.PopupHeight || (int)Width != ToolbarSettings.User.PopupWidth)
            {
                ToolbarSettings.User.PopupHeight = (int)Height;
                ToolbarSettings.User.PopupWidth = (int)Width;
            }

            // Push outside of screens to prevent flickering when showing
            BeginAnimation(TopProperty, new DoubleAnimation { To = 100000, Duration = TimeSpan.Zero });
            BeginAnimation(LeftProperty, new DoubleAnimation { To = 100000, Duration = TimeSpan.Zero });

            base.Hide();

            _dwmFlushOnRender = false;

            SearchState.Instance.Reset();
        }

        public new void Show()
        {
            if (Visibility == Visibility.Visible)
                return;

            ShowActivated = TaskbarStateManager.Instance.IsIcon;
            base.Show();

            SetTopmostBelowTaskbar();

            Showing?.Invoke(this, EventArgs.Empty);
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
            Width = width;
            Height = height;

            var vertical = taskbarEdge == Edge.Left || taskbarEdge == Edge.Right;
            var animation = new DoubleAnimation
            {
                To = vertical ? top : left,
                Duration = TimeSpan.Zero
            };
            animation.Completed += (s, e) =>
            {
                if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                    AnimateShowWin11(left, top, width, height, taskbarEdge);
                else
                    AnimateShowWin10(left, top, width, height, taskbarEdge);
            };

            BeginAnimation(vertical ? TopProperty : LeftProperty, animation);
        }

        private void AnimateShowWin10(double left, double top, double width, double height, Edge taskbarEdge)
        {
            DropShadowEffect.BlurRadius = DropShadowBlurRadius;
            if (taskbarEdge == Edge.Top)
            {
                Top -= DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, 0, DropShadowBlurRadius, DropShadowBlurRadius);
            }
            else if (taskbarEdge == Edge.Right)
            {
                Top -= DropShadowBlurRadius;
                Left += DropShadowBlurRadius;
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, DropShadowBlurRadius, 0, DropShadowBlurRadius);
            }
            else if (taskbarEdge == Edge.Left)
            {
                Top -= DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                PopupMarginBorder.Margin = new Thickness(0, DropShadowBlurRadius, DropShadowBlurRadius, DropShadowBlurRadius);
            }
            else
            {
                Top += DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, DropShadowBlurRadius, DropShadowBlurRadius, 0);
            }

            DependencyProperty property = null;
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
            BeginAnimation(property, new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            });

            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            });

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
            ContentGrid.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                From = fromThickness,
                To = new Thickness(0),
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.8),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void AnimateShowWin11(double left, double top, double width, double height, Edge taskbarEdge)
        {
            DependencyProperty property = null;
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
            BeginAnimation(property, new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.25),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 }
            });

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
            ContentGrid.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                From = fromThickness,
                To = new Thickness(0),
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.3),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 }
            });
        }

        private void AnimateHideWin10(Edge taskbarEdge)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(30),
            });

            double target = 0;
            DependencyProperty property = null;
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
            var animation = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(30),
            };
            animation.Completed += OnHidden;
            BeginAnimation(property, animation);
        }

        private void AnimateHideWin11(Edge taskbarEdge)
        {
            DependencyProperty property = null;
            double from = 0;
            double to = 0;
            switch (taskbarEdge)
            {
                case Edge.Left:
                    from = RestoreBounds.Left;
                    to = RestoreBounds.Left - Width;
                    property = LeftProperty;
                    break;
                case Edge.Right:
                    from = RestoreBounds.Left;
                    to = RestoreBounds.Left + Width;
                    property = LeftProperty;
                    break;
                case Edge.Top:
                    from = RestoreBounds.Top;
                    to = RestoreBounds.Top - Height;
                    property = TopProperty;
                    break;
                case Edge.Bottom:
                    from = RestoreBounds.Top;
                    to = RestoreBounds.Top + Height;
                    property = TopProperty;
                    break;
            }
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.25),
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

        private void OnCompositionTargetRendering(object sender, EventArgs e)
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

            var hwnd = new WindowInteropHelper(this).Handle;
            var taskbarHwnd = NativeMethods.FindWindow("Shell_TrayWnd", null);

            if (taskbarHwnd != IntPtr.Zero)
            {
                // Set window above other windows but below the taskbar
                NativeMethods.SetWindowPos(hwnd, taskbarHwnd, 0, 0, 0, 0,
                    swpNomove | swpNosize | swpNoactivate | swpShowwindow);
            }
            else
            {
                // Regular topmost
                NativeMethods.SetWindowPos(hwnd, (IntPtr)hwndTopmost, 0, 0, 0, 0,
                    swpNomove | swpNosize | swpNoactivate | swpShowwindow);
            }
        }
    }
}