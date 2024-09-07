using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public partial class SettingsControl
    {
        public SettingsControl()
        {
            InitializeComponent();

            // Preselect sorting method
            (SortByMenu.Items[ToolbarSettings.User.SortBy - 1] as MenuItem).IsChecked = true;

            // Preselect active datatemplate
            for (var i = 0; i < ItemTemplateMenu.Items.Count; i++)
            {
                var menuItem = ItemTemplateMenu.Items[i] as MenuItem;
                if (menuItem.Tag.ToString() == ToolbarSettings.User.ItemTemplate)
                    menuItem.IsChecked = true;
                else
                    menuItem.IsChecked = false;
            }
        }

        private void OpenAboutWindow(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
            Window about = new About();
            about.Show();
        }

        private void OpenRulesWindow(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
            Window rules = new Rules();
            rules.Show();
        }

        private void OpenInstanceNameDialog(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
            var inputDialog = new InputDialog(Properties.Resources.SettingsSetInstanceName,
                                              ToolbarSettings.User.InstanceName);
            if (inputDialog.ShowDialog() == true)
            {
                ToolbarSettings.User.InstanceName = inputDialog.ResponseText;
                EverythingSearch.Instance.SetInstanceName(ToolbarSettings.User.InstanceName);
            }
        }

        private void OpenShortcutWindow(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
            var shortcutSelector = new ShortcutSelector();
            if (shortcutSelector.ShowDialog().Value)
            {
                if (shortcutSelector.Modifiers == ModifierKeys.Windows)
                {
                    ShortcutManager.Instance.SetShortcut(shortcutSelector.Key, shortcutSelector.Modifiers);
                    foreach (var exe in Process.GetProcesses())
                    {
                        if (exe.ProcessName == "explorer")
                            exe.Kill();
                    }
                }

                if (ShortcutManager.Instance.AddOrReplace("FocusSearchBox", shortcutSelector.Key, shortcutSelector.Modifiers))
                {
                    ShortcutManager.Instance.SetShortcut(shortcutSelector.Key, shortcutSelector.Modifiers);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.MessageBoxFailedToRegisterHotkey,
                        Properties.Resources.MessageBoxErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void OnSortByClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = sender as MenuItem;
            var menu = selectedItem.Parent as MenuItem;
            var selectedIndex = menu.Items.IndexOf(selectedItem);

            (menu.Items[ToolbarSettings.User.SortBy - 1] as MenuItem).IsChecked = false;
            (menu.Items[selectedIndex] as MenuItem).IsChecked = false;

            int[] fastSortExceptions = { 9, 10, 17, 18 };
            if (EverythingSearch.GetIsFastSort(selectedIndex + 1) ||
                fastSortExceptions.Contains(selectedIndex + 1))
            {
                ToolbarSettings.User.SortBy = selectedIndex + 1;
            }
            else
            {
                MessageBox.Show(Properties.Resources.MessageBoxFastSortUnavailable,
                                Properties.Resources.MessageBoxFastSortUnavailableTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Asterisk);
            }

            (menu.Items[ToolbarSettings.User.SortBy - 1] as MenuItem).IsChecked = true;
        }

        private void OnItemTemplateClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = sender as MenuItem;
            var menu = selectedItem.Parent as MenuItem;

            for (var i = 0; i < menu.Items.Count; i++)
            {
                var menuItem = menu.Items[i] as MenuItem;
                if (menuItem == selectedItem)
                {
                    menuItem.IsChecked = true;
                    ToolbarSettings.User.ItemTemplate = selectedItem.Tag.ToString();
                }
                else
                {
                    menuItem.IsChecked = false;
                }
            }
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            // Simulate right mouse button click to open context menu
            var mouseDownEvent =
                new MouseButtonEventArgs(Mouse.PrimaryDevice,
                    Environment.TickCount,
                    MouseButton.Right)
                {
                    RoutedEvent = Mouse.MouseUpEvent,
                    Source = this,
                };
            InputManager.Current.ProcessInput(mouseDownEvent);
        }
    }
}
