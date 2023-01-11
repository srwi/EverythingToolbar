using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NHotkey;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EverythingToolbar
{
    public partial class SearchWindow : Window
    {
        public static double taskbarHeight = 0;
        public static double taskbarWidth = 0;

        public new double Height
        {
            get
            {
                return (double)GetValue(HeightProperty);
            }
            set
            {
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                double newHeight = Math.Max(Math.Min(screenHeight - taskbarHeight, value), 300);
                SetValue(HeightProperty, newHeight);
            }
        }
        
        public new double Width
        {
            get
            {
                return (double)GetValue(WidthProperty);
            }
            set
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double newWidth = Math.Max(Math.Min(screenWidth - taskbarWidth, value), 300);
                SetValue(WidthProperty, newWidth);
            }
        }

        public static readonly SearchWindow Instance = new SearchWindow();
        public event EventHandler<EventArgs> Hiding;
        public event EventHandler<EventArgs> Showing;

        private SearchWindow()
        {
            InitializeComponent();

            Loaded += (s, _) =>
            {
                ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
                ResourceManager.Instance.AutoApplyTheme();
            };

            DataContext = EverythingSearch.Instance;

            if (Settings.Default.isUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.isUpgradeRequired = false;
                Settings.Default.Save();
            }
            Settings.Default.PropertyChanged += (s, e) => Settings.Default.Save();
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

        public new void Hide()
        {
            if (Visibility != Visibility.Visible)
                return;

            HistoryManager.Instance.AddToHistory(EverythingSearch.Instance.SearchTerm);
            Hiding?.Invoke(this, new EventArgs());
            base.Hide();
        }

        public new void Show()
        {
            if (Visibility == Visibility.Visible)
                return;

            ShowActivated = TaskbarStateManager.Instance.IsIcon;
            base.Show();
            Topmost = true;
            Topmost = false;
            Showing?.Invoke(this, new EventArgs());
            AnimateOpen();
        }

        public void Show(object sender, HotkeyEventArgs e)
        {
            Show();
        }

        public void Toggle()
        {
            if (Visibility == Visibility.Visible)
                Hide();
            else
                Show();
        }

        private void AnimateOpen()
        {
            QuinticEase ease = new QuinticEase
            {
                EasingMode = EasingMode.EaseOut
            };

            int modifier = 1;
            Duration duration = TimeSpan.FromSeconds(Settings.Default.isAnimationsDisabled ? 0 : 0.4);
            DoubleAnimation outer = new DoubleAnimation(modifier * 150, 0, duration)
            {
                EasingFunction = ease
            };
            DependencyProperty outerProp = TranslateTransform.YProperty;
            translateTransform?.BeginAnimation(outerProp, outer);

            DoubleAnimation opacity = new DoubleAnimation(0, 1, duration)
            {
                EasingFunction = ease
            };
            PopupMarginBorder?.BeginAnimation(OpacityProperty, opacity);

            duration = TimeSpan.FromSeconds(Settings.Default.isAnimationsDisabled ? 0 : 0.8);
            ThicknessAnimation inner = new ThicknessAnimation(new Thickness(0), duration)
            {
                EasingFunction = ease
            };
            if (TaskbarStateManager.Instance.TaskbarEdge == Edge.Top)
                inner.From = new Thickness(0, -50, 0, 50);
            else if (TaskbarStateManager.Instance.TaskbarEdge == Edge.Right)
                inner.From = new Thickness(50, 0, -50, 0);
            else if (TaskbarStateManager.Instance.TaskbarEdge == Edge.Bottom)
                inner.From = new Thickness(0, 50, 0, -50);
            else if (TaskbarStateManager.Instance.TaskbarEdge == Edge.Left)
                inner.From = new Thickness(-50, 0, 50, 0);
            ContentGrid?.BeginAnimation(MarginProperty, inner);
        }

        private void OpenSearchInEverything(object sender, RoutedEventArgs e)
        {
            EverythingSearch.Instance.OpenLastSearchInEverything();
        }
    }
}
