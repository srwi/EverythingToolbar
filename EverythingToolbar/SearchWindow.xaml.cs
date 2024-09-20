using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using EverythingToolbar.Helpers;

namespace EverythingToolbar
{
    public partial class SearchWindow
    {
        public static readonly SearchWindow Instance = new SearchWindow();
        public event EventHandler<EventArgs> Hiding;
        public event EventHandler<EventArgs> Showing;

        private static readonly int DropShadowBlurRadius = 12;

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

            EventDispatcher.Instance.GlobalKeyEvent += OnPreviewKeyDown;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (EverythingSearch.Instance.Initialize())
                EverythingSearch.Instance.Reset();
        }

        private void OnActivated(object sender, EventArgs e)
        {
            if (TaskbarStateManager.Instance.IsIcon)
            {
                EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
            }

            EventDispatcher.Instance.InvokeFocusRequested(this, EventArgs.Empty);
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                EverythingSearch.Instance.SelectFilterFromIndex(index);
            }
            else if (e.Key == Key.Escape)
            {
                Instance.Hide();
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.Tab)
            {
                var offset = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
                EverythingSearch.Instance.CycleFilters(offset);
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
            EverythingSearch.Instance.OpenLastSearchInEverything();
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
            
            EverythingSearch.Instance.Reset();
        }

        public new void Show()
        {
            if (Visibility == Visibility.Visible)
                return;

            ShowActivated = TaskbarStateManager.Instance.IsIcon;
            base.Show();

            // Bring to top and immediately behind taskbar
            Topmost = true;
            Topmost = false;

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
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
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
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
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
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.4),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void AnimateHideWin10(Edge taskbarEdge)
        {
            Topmost = true;
            Topmost = false;

            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(30),
            });

            double target = 0;
            DependencyProperty property = null;
            switch(taskbarEdge)
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
            Topmost = true;
            Topmost = false;

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
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
            };
            animation.Completed += OnHidden;
            BeginAnimation(property, animation);

            var toThickness = new Thickness(0);
            switch (taskbarEdge)
            {
                case Edge.Top:
                    toThickness = new Thickness(0, -50, 0, 50);
                    break;
                case Edge.Right:
                    toThickness = new Thickness(50, 0, -50, 0);
                    break;
                case Edge.Bottom:
                    toThickness = new Thickness(0, 50, 0, -50);
                    break;
                case Edge.Left:
                    toThickness = new Thickness(-50, 0, 50, 0);
                    break;
            }
            ContentGrid.BeginAnimation(MarginProperty, new ThicknessAnimation
            {
                To = toThickness,
                Duration = ToolbarSettings.User.IsAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.5),
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn }
            });
        }

        public void AnimateHide(Edge taskbarEdge)
        {
            if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                AnimateHideWin11(taskbarEdge);
            else
                AnimateHideWin10(taskbarEdge);
        }
    }
}
