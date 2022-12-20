using EverythingToolbar.Helpers;
using NHotkey;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
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
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SearchResultsWindow.Instance.ShowActivated = false;
            SearchResultsWindow.Instance.Show();
            SearchResultsWindow.Instance.Hide();
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
            if (SearchResultsWindow.Instance.IsOpen)
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
