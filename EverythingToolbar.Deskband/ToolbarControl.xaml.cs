using EverythingToolbar.Helpers;
using NHotkey;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace EverythingToolbar
{
    public partial class ToolbarControl : UserControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            Loaded += (s, _) =>
            {
                ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
                ResourceManager.Instance.AutoApplyTheme();
            };

            // Focus an invisible text box to prevent Windows from randomly focusing the search box
            // and causing visual distraction
            SearchBox.LostKeyboardFocus += OnSearchBoxLostKeyboardFocus;
            SearchWindow.Instance.Hiding += OnSearchWindowHiding;

            if (!ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                   (Key)Properties.Settings.Default.shortcutKey,
                   (ModifierKeys)Properties.Settings.Default.shortcutModifiers,
                   FocusSearchBox))
            {
                ShortcutManager.Instance.SetShortcut(Key.None, ModifierKeys.None);
                MessageBox.Show(Properties.Resources.MessageBoxFailedToRegisterHotkey,
                    Properties.Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            ShortcutManager.Instance.SetFocusCallback(FocusSearchBox);
            if (Properties.Settings.Default.isReplaceStartMenuSearch)
                ShortcutManager.Instance.HookStartMenu();
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty((sender as TextBox).Text))
            {
                Rect position = CalculateSearchWindowPosition();
                SearchWindow.Instance.Show();
                SearchWindow.Instance.PlaceWindow(position);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private Rect CalculateSearchWindowPosition()
        {
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(this)).Handle;

            RECT rect;
            if (GetWindowRect(hwnd, out rect))
            {
                ToolbarLogger.GetLogger("ToolbarControl").Info($"left {rect.Left} top {rect.Top}");
                Console.WriteLine("Left: {0}, Top: {1}, Right: {2}, Bottom: {3}", rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            Rect deskbandPosition = new Rect(new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Bottom));

            Rect windowPosition = CalculateWindowPosition(deskbandPosition, TaskbarStateManager.Instance.TaskbarEdge, Properties.Settings.Default.popupSize);
            return windowPosition;
        }

        private Rect CalculateWindowPosition(Rect placementTarget, Edge taskbarEdge, Size windowSize)
        {
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.PrimaryScreen;
            Rect windowPosition = new Rect(0, 0, windowSize.Width, windowSize.Height);

            switch (taskbarEdge)
            {
                case Edge.Bottom:
                    windowPosition.X = placementTarget.Left;
                    windowPosition.Y = placementTarget.Top - windowSize.Height;
                    break;
                case Edge.Top:
                    windowPosition.X = placementTarget.Left;
                    windowPosition.Y = placementTarget.Bottom;
                    break;
                case Edge.Right:
                    windowPosition.X = placementTarget.Left - windowSize.Width;
                    windowPosition.Y = placementTarget.Top;
                    break;
                case Edge.Left:
                    windowPosition.X = placementTarget.Right;
                    windowPosition.Y = placementTarget.Top;
                    break;
            }

            if (windowPosition.X + windowSize.Width > screen.WorkingArea.Width)
            {
                windowPosition.X = screen.WorkingArea.Width - windowSize.Width;
            }
            if (windowPosition.X < 0)
            {
                windowPosition.X = 0;
            }
            if (windowPosition.Y + windowSize.Height > screen.WorkingArea.Height)
            {
                windowPosition.Y = screen.WorkingArea.Height - windowSize.Height;
            }
            if (windowPosition.Y < 0)
            {
                windowPosition.Y = 0;
            }

            return windowPosition;
        }

        private void OnSearchWindowHiding(object sender, EventArgs e)
        {
            FocusKeyboardFocusCapture(sender, e);
        }

        private void OnSearchBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            EventDispatcher.Instance.InvokeUnfocusRequested(sender, e);

            if (e.NewFocus == null)  // New focus outside application
            {
                FocusKeyboardFocusCapture(sender, e);
            }
        }

        private void FocusKeyboardFocusCapture(object sender, EventArgs e)
        {
            Keyboard.Focus(KeyboardFocusCapture);
        }

        private void FocusSearchBox(object sender, HotkeyEventArgs e)
        {
            if (TaskbarStateManager.Instance.IsIcon)
            {
                SearchWindow.Instance.Show();
            }
            else
            {
                SearchWindow.Instance.Show();
                NativeMethods.SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
                SearchBox.Focus();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent explorer crash when pressing Alt + F4
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            EventDispatcher.Instance.InvokeFocusRequested(sender, e);
        }
    }
}
