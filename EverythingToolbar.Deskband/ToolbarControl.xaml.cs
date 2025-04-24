using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using System;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            var behavior = new SearchWindowPlacement
            {
                PlacementTarget = this
            };
            Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);

            SearchWindow.Instance.Hiding += OnSearchWindowHiding;

            ShortcutManager.Instance.Initialize(FocusSearchBox);

            if (ToolbarSettings.User.IsReplaceStartMenuSearch)
                StartMenuIntegration.Instance.Enable();
        }

        private void OnSearchWindowHiding(object sender, EventArgs e)
        {
            Keyboard.Focus(KeyboardFocusCapture);
        }

        private void OnSearchBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            EventDispatcher.Instance.InvokeUnfocusRequested(sender, e);

            if (e.NewFocus == null)  // New focus outside application
            {
                // Focus an invisible text box to prevent Windows from randomly focusing the search box
                // and causing visual distraction
                Keyboard.Focus(KeyboardFocusCapture);
            }
        }

        private void OnSearchBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SearchWindow.Instance.Show();
        }

        private void FocusSearchBox(object sender, HotkeyEventArgs e)
        {
            if (TaskbarStateManager.Instance.IsIcon)
            {
                SearchWindow.Instance.Toggle();
            }
            else if (SearchBox.IsKeyboardFocusWithin)
            {
                SearchWindow.Instance.Hide();
            }
            else
            {
                EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
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