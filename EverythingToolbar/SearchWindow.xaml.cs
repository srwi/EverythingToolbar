using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Shell;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

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

            if (Settings.Default.isUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.isUpgradeRequired = false;
            }

            if (Environment.OSVersion.Version < Utils.WindowsVersion.Windows11)
            {
                AllowsTransparency = true;
                Loaded += (s, e) =>
                {
                    WindowChrome.SetWindowChrome(this, new WindowChrome()
                    {
                        ResizeBorderThickness = new Thickness(DropShadowBlurRadius + 3),
                        CaptionHeight = 0
                    });
                };
            }
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
                NativeMethods.SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
                SearchBox.Focus();
            }

            EventDispatcher.Instance.InvokeFocusRequested(sender, e);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null)  // New focus outside application
            {
                Hide();
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            EventDispatcher.Instance.InvokeKeyPressed(this, e);
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
            if (Height != Settings.Default.popupSize.Height || Width != Settings.Default.popupSize.Width)
                Settings.Default.popupSize = new Size(Width, Height);

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
            if (taskbarEdge == Edge.Right || taskbarEdge == Edge.Left)
                Top = top;
            else
                Left = left;

            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                AnimateShowWin11(left, top, width, height, taskbarEdge);
            else
                AnimateShowWin10(left, top, width, height, taskbarEdge);
        }

        private void AnimateShowWin10(double left, double top, double width, double height, Edge taskbarEdge)
        {
            Height = Settings.Default.popupSize.Height;
            Width = Settings.Default.popupSize.Width;
            DropShadowEffect.BlurRadius = DropShadowBlurRadius;
            Thickness contentGridOffset;
            if (taskbarEdge == Edge.Top)
            {
                Top -= DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                contentGridOffset = new Thickness(0, -50, 0, 50);
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, 0, DropShadowBlurRadius, DropShadowBlurRadius);
            }
            else if (taskbarEdge == Edge.Right)
            {
                Top -= DropShadowBlurRadius;
                Left += DropShadowBlurRadius;
                contentGridOffset = new Thickness(50, 0, -50, 0);
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, DropShadowBlurRadius, 0, DropShadowBlurRadius);
            }
            else if (taskbarEdge == Edge.Left)
            {
                Top -= DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                contentGridOffset = new Thickness(-50, 0, 50, 0);
                PopupMarginBorder.Margin = new Thickness(0, DropShadowBlurRadius, DropShadowBlurRadius, DropShadowBlurRadius);
            }
            else
            {
                Top += DropShadowBlurRadius;
                Left -= DropShadowBlurRadius;
                contentGridOffset = new Thickness(0, 50, 0, -50);
                PopupMarginBorder.Margin = new Thickness(DropShadowBlurRadius, DropShadowBlurRadius, DropShadowBlurRadius, 0);
            }

            int sign = taskbarEdge == Edge.Right || taskbarEdge == Edge.Bottom ? 1 : -1;
            double target = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? left : top;
            Duration duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.4);
            DoubleAnimation positionAnimation = new DoubleAnimation(target + 150 * sign, target, duration)
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };
            DependencyProperty positionProperty = taskbarEdge == Edge.Bottom || taskbarEdge == Edge.Top ? TopProperty : LeftProperty;
            BeginAnimation(positionProperty, positionAnimation);

            duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.8);
            ThicknessAnimation inner = new ThicknessAnimation(new Thickness(0), duration)
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                From = contentGridOffset
            };
            ContentGrid.BeginAnimation(MarginProperty, inner);
        }

        private void AnimateShowWin11(double left, double top, double width, double height, Edge taskbarEdge)
        {
            int sign = taskbarEdge == Edge.Right || taskbarEdge == Edge.Bottom ? 1 : -1;
            double offset = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? width : height;
            double target = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? left : top;
            Duration duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.2);
            DoubleAnimation positionAnimation = new DoubleAnimation(target + offset * sign, target, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            DependencyProperty positionProperty = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? LeftProperty : TopProperty;
            BeginAnimation(positionProperty, positionAnimation);

            duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.4);
            ThicknessAnimation contentAnimation = new ThicknessAnimation(new Thickness(0), duration)
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
            };
            if (taskbarEdge == Edge.Top)
                contentAnimation.From = new Thickness(0, -50, 0, 50);
            else if (taskbarEdge == Edge.Right)
                contentAnimation.From = new Thickness(50, 0, -50, 0);
            else if (taskbarEdge == Edge.Bottom)
                contentAnimation.From = new Thickness(0, 50, 0, -50);
            else if (taskbarEdge == Edge.Left)
                contentAnimation.From = new Thickness(-50, 0, 50, 0);
            ContentGrid.BeginAnimation(MarginProperty, contentAnimation);
        }

        private void AnimateHideWin10(Edge taskbarEdge)
        {
            // Move the window back so the next opening animation will not jump
            double target = 0;
            DependencyProperty property = TopProperty;
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
            DoubleAnimation animation = new DoubleAnimation(target, TimeSpan.Zero);
            animation.Completed += OnHidden;
            BeginAnimation(property, animation);
        }

        private void AnimateHideWin11(Edge taskbarEdge)
        {
            Topmost = true;
            Topmost = false;

            int sign = taskbarEdge == Edge.Right || taskbarEdge == Edge.Bottom ? 1 : -1;
            double offset = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? Width : Height;
            double target = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? RestoreBounds.Left : RestoreBounds.Top;
            Duration duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.2);
            DoubleAnimation positionAnimation = new DoubleAnimation(target, target + offset * sign, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            DependencyProperty positionProperty = taskbarEdge == Edge.Right || taskbarEdge == Edge.Left ? LeftProperty : TopProperty;
            positionAnimation.Completed += OnHidden;
            BeginAnimation(positionProperty, positionAnimation);

            duration = Settings.Default.isAnimationsDisabled ? TimeSpan.Zero : TimeSpan.FromSeconds(0.5);
            ThicknessAnimation contentAnimation = new ThicknessAnimation(new Thickness(0), duration)
            {
                EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseIn }
            };
            if (taskbarEdge == Edge.Top)
                contentAnimation.To = new Thickness(0, -50, 0, 50);
            else if (taskbarEdge == Edge.Right)
                contentAnimation.To = new Thickness(50, 0, -50, 0);
            else if (taskbarEdge == Edge.Bottom)
                contentAnimation.To = new Thickness(0, 50, 0, -50);
            else if (taskbarEdge == Edge.Left)
                contentAnimation.To = new Thickness(-50, 0, 50, 0);
            ContentGrid.BeginAnimation(MarginProperty, contentAnimation);
        }

        public void AnimateHide(Edge taskbarEdge)
        {
            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                AnimateHideWin11(taskbarEdge);
            else
                AnimateHideWin10(taskbarEdge);
        }
    }
}
