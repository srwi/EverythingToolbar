using EverythingToolbar.Helpers;
using NHotkey;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace EverythingToolbar
{
    public partial class ToolbarControl : UserControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            SearchBox.LostKeyboardFocus += (object sender, KeyboardFocusChangedEventArgs e) =>
            {
                Keyboard.Focus(KeyboardFocusCapture);
                EventDispatcher.Instance.InvokeUnfocusRequested(sender, e);
            };

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

            ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
            ResourceManager.Instance.AutoApplyTheme();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.ShowActivated = false;
            SearchWindow.Instance.Show();
            SearchWindow.Instance.Hide();
        }

        public void Destroy()
        {
            Content = null;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent explorer crash when pressing Alt + F4
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            EventDispatcher.Instance.InvokeFocusRequested(sender, e);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public void FocusSearchBox(object sender, HotkeyEventArgs e)
        {
            if (SearchWindow.Instance.IsOpen)
            {
                EverythingSearch.Instance.SearchTerm = null;
            }
            else
            {
                SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
                EventDispatcher.Instance.InvokeFocusRequested(sender, e);
                Keyboard.Focus(SearchBox);

                if (Properties.Settings.Default.isIconOnly)
                    EverythingSearch.Instance.SearchTerm = "";
            }
        }
    }
}
