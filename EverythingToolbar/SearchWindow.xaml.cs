using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EverythingToolbar
{
    public partial class SearchWindow : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_MICA_EFFECT = 1029,
            DWMWA_SYSTEMBACKDROP_TYPE = 38
        }

        private void Window_ContentRendered(object sender, System.EventArgs e)
        {
            // Apply Mica brush
            UpdateStyleAttributes((HwndSource)sender);
        }

        public static void UpdateStyleAttributes(HwndSource hwnd)
        {
            int trueValue = 0x01;
            int mica = 0x03;
            DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_SYSTEMBACKDROP_TYPE, ref mica, Marshal.SizeOf(typeof(int)));
            DwmSetWindowAttribute(hwnd.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get PresentationSource
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

            // Subscribe to PresentationSource's ContentRendered event
            presentationSource.ContentRendered += Window_ContentRendered;
        }

        //public static Edge taskbarEdge;
        public static double taskbarHeight = 0;
        public static double taskbarWidth = 0;
        public bool IsOpen = false;
        
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

        private SearchWindow()
        {
            InitializeComponent();
            DataContext = EverythingSearch.Instance;

            if (Settings.Default.isUpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.isUpgradeRequired = false;
                Settings.Default.Save();
            }
            Settings.Default.PropertyChanged += (s, e) => Settings.Default.Save();

            LostKeyboardFocus += OnLostKeyboardFocus;
            EventDispatcher.Instance.HideWindow += Hide;
            EventDispatcher.Instance.ShowWindow += Show;

            ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
            ResourceManager.Instance.AutoApplyTheme();

            Loaded += Window_Loaded;
        }

        private void OnResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            Resources = e.NewResource;
        }

        private new void Hide()
        {
            IsOpen = false;
            HistoryManager.Instance.AddToHistory(EverythingSearch.Instance.SearchTerm);
            base.Hide();
        }

        private new void Show()
        {
            if (!IsOpen)
                OnOpened(null, null);

            IsOpen = true;
            base.Show();
            Keyboard.Focus(SearchBox);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null)  // New focus outside application
            {
                EventDispatcher.Instance.InvokeHideWindow();
            }
        }

        private void OnOpened(object sender, EventArgs e)
        {
            //Keyboard.Focus(SearchBox);

            //switch (taskbarEdge)
            //{
            //    case Edge.Top:
            //        Placement = PlacementMode.Bottom;
            //        PopupMarginBorder.Margin = new Thickness(12, 0, 12, 12);
            //        break;
            //    case Edge.Left:
            //        Placement = PlacementMode.Right;
            //        PopupMarginBorder.Margin = new Thickness(0, 12, 12, 12);
            //        break;
            //    case Edge.Right:
            //        Placement = PlacementMode.Left;
            //        PopupMarginBorder.Margin = new Thickness(12, 12, 0, 12);
            //        break;
            //    case Edge.Bottom:
            //        Placement = PlacementMode.Top;
            //        PopupMarginBorder.Margin = new Thickness(12, 12, 12, 0);
            //        break;
            //}

            Height = Properties.Settings.Default.popupSize.Height;
            Width = Properties.Settings.Default.popupSize.Width;

            QuinticEase ease = new QuinticEase
            {
                EasingMode = EasingMode.EaseOut
            };

            int modifier = 1;
            Duration duration = TimeSpan.FromSeconds(Properties.Settings.Default.isAnimationsDisabled ? 0 : 0.4);
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

            duration = TimeSpan.FromSeconds(Properties.Settings.Default.isAnimationsDisabled ? 0 : 0.8);
            ThicknessAnimation inner = new ThicknessAnimation(new Thickness(0), duration)
            {
                EasingFunction = ease
            };
            //if (taskbarEdge == Edge.Top)
            //    inner.From = new Thickness(0, -50, 0, 50);
            //else if (taskbarEdge == Edge.Right)
            //    inner.From = new Thickness(50, 0, -50, 0);
            //else if (taskbarEdge == Edge.Bottom)
                inner.From = new Thickness(0, 50, 0, -50);
            //else if (taskbarEdge == Edge.Left)
            //    inner.From = new Thickness(-50, 0, 50, 0);
            ContentGrid?.BeginAnimation(MarginProperty, inner);
        }

        private void OpenSearchInEverything(object sender, RoutedEventArgs e)
        {
            EverythingSearch.Instance.OpenLastSearchInEverything();
        }
    }
}
