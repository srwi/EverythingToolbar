using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using System;
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

            var behavior = new SearchWindowPlacement()
            {
                PlacementTarget = this
            };
            Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);

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
                SearchWindow.Instance.Show();
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

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            EventDispatcher.Instance.InvokeKeyPressed(this, e);
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
