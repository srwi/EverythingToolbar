using EverythingToolbar.Helpers;
using NHotkey;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace EverythingToolbar
{
    public partial class ToolbarControl : UserControl
    {
        // Requires Windows 10 Anniversary Update
        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        public event EventHandler<EventArgs> FocusRequested;
        public event EventHandler<EventArgs> UnfocusRequested;

        double CurrentDpi { get; set; }
        double InitialDpi { get; set; }

        public ToolbarControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            ApplicationResources.Instance.ResourceChanged += (object sender, ResourcesChangedEventArgs e) =>
            {
                try
                {
                    Resources.MergedDictionaries.Add(e.NewResource);
                    Properties.Settings.Default.Save();
                }
                catch (Exception ex)
                {
                    ToolbarLogger.GetLogger("EverythingToolbar").Error(ex, "Failed to apply resource.");
                }
            };
            ApplicationResources.Instance.LoadDefaults();

            SearchResultsPopup.Closed += (object sender, EventArgs e) =>
            {
                Keyboard.Focus(KeyboardFocusCapture);
                UnfocusRequested?.Invoke(this, new EventArgs());
            };

            ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                (Key)Properties.Settings.Default.shortcutKey,
                (ModifierKeys)Properties.Settings.Default.shortcutModifiers,
                FocusSearchBox);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource.AddHook(HwndSourceHook);

            InitialDpi = VisualTreeHelper.GetDpi(this).PixelsPerInchY;
            CurrentDpi = InitialDpi;

            UpdateDpi(GetParentWindowDpi(this));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            hwndSource.RemoveHook(HwndSourceHook);
        }

        private static double GetParentWindowDpi(Visual visual)
        {
            // Check for Windows 10 Anniversary version
            if (Environment.OSVersion.Version.CompareTo(new Version(10, 0, 14393)) < 0)
            {
                return 96.0;
            }

            if (!(PresentationSource.FromVisual(visual) is HwndSource hwnd))
            {
                return 96.0;
            }

            return GetDpiForWindow(hwnd.Handle);
        }

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_DPICHANGED_AFTERPARENT = 0x02E3;
            if (msg == WM_DPICHANGED_AFTERPARENT)
            {
                    UpdateDpi(GetParentWindowDpi(this));
                    handled = true;
            }

            return IntPtr.Zero;
        }

        private void UpdateDpi(double newDpi, bool updateSize = true)
        {
            if (updateSize)
            {
                var dpiScale = newDpi / CurrentDpi;
                Width *= dpiScale;
                Height *= dpiScale;
            }

            CurrentDpi = newDpi;

            if (VisualTreeHelper.GetChildrenCount(this) == 0)
                return;

            var child = VisualTreeHelper.GetChild(this, 0);
            double renderScale = newDpi / InitialDpi;

            var scaleTransform = Math.Abs(renderScale - 1) < 0.0001
                ? Transform.Identity
                : new ScaleTransform(renderScale, renderScale);
            child.SetValue(FrameworkElement.LayoutTransformProperty, scaleTransform);
        }

        public void Destroy()
        {
            Content = null;
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (!SearchResultsPopup.IsOpen)
                return;

            if (e.Key == Key.Up)
            {
                SearchResultsPopup.SearchResultsView.SelectPreviousSearchResult();
            }
            else if (e.Key == Key.Down)
            {
                SearchResultsPopup.SearchResultsView.SelectNextSearchResult();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                string path = "";
                if (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedIndex >= 0)
                    path = (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
                EverythingSearch.Instance.OpenLastSearchInEverything(path);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                SearchResultsPopup.SearchResultsView.OpenFilePath(null, null);
            }
            else if (e.Key == Key.Enter)
            {
                SearchResultsPopup.SearchResultsView.OpenSelectedSearchResult();
            }
            else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SearchResultsPopup.SearchResultsView.PreviewSelectedFile();
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                int index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                EverythingSearch.Instance.SelectFilterFromIndex(index);
            }
            else if (e.Key == Key.Escape)
            {
                EverythingSearch.Instance.SearchTerm = null;
                Keyboard.ClearFocus();
            }
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            if (!SearchResultsPopup.IsOpen)
                return;

            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    EverythingSearch.Instance.CycleFilters(-1);
                else
                    EverythingSearch.Instance.CycleFilters(1);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusRequested?.Invoke(this, new EventArgs());
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void FocusSearchBox(object sender, HotkeyEventArgs e)
        {
            if (SearchResultsPopup.IsOpen)
            {
                EverythingSearch.Instance.SearchTerm = null;
            }
            else
            {
                SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
                FocusRequested?.Invoke(this, new EventArgs());
                Keyboard.Focus(SearchBox);

                if (Properties.Settings.Default.isIconOnly)
                    EverythingSearch.Instance.SearchTerm = "";
            }
        }
    }
}
