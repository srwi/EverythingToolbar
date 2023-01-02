using EverythingToolbar.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SettingsControl : Grid
    {
        public SettingsControl()
        {
            InitializeComponent();

            // Preselect sorting method
            (SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;

            // Preselect active datatemplate
            for (int i = 0; i < ItemTemplateMenu.Items.Count; i++)
            {
                MenuItem menuItem = ItemTemplateMenu.Items[i] as MenuItem;
                if (menuItem.Tag.ToString() == Properties.Settings.Default.itemTemplate)
                    menuItem.IsChecked = true;
                else
                    menuItem.IsChecked = false;
            }

            // IsEnabled property of matchWholeWord menu item needs to be handled
            // in code because DataTriggers are not compatible with DynamicResources as MenuItem styles
            Properties.Settings.Default.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled" || e.PropertyName == "CurrentFilter")
            {
                bool newEnabledState = !Properties.Settings.Default.isRegExEnabled && EverythingSearch.Instance.CurrentFilter.IsMatchWholeWord == null;
                IsMatchWholeWordMenuItem.IsEnabled = newEnabledState;
            }
        }

        private void OpenAboutWindow(object sender, RoutedEventArgs e)
        {
            Window about = new About();
            about.Show();
        }

        private void OpenRulesWindow(object sender, RoutedEventArgs e)
        {
            Window rules = new Rules();
            rules.Show();
        }

        private void OpenInstanceNameDialog(object sender, RoutedEventArgs e)
        {
            var inputDialog = new InputDialog(Properties.Resources.SettingsSetInstanceName,
                                              Properties.Settings.Default.instanceName);
            if (inputDialog.ShowDialog() == true)
            {
                Properties.Settings.Default.instanceName = inputDialog.ResponseText;
                EverythingSearch.Instance.SetInstanceName(Properties.Settings.Default.instanceName);
            }
        }

        private void OpenShortcutWindow(object sender, RoutedEventArgs e)
        {
            ShortcutSelector shortcutSelector = new ShortcutSelector();
            if (shortcutSelector.ShowDialog().Value)
            {
                if (shortcutSelector.Modifiers == ModifierKeys.Windows)
                {
                    ShortcutManager.Instance.SetShortcut(shortcutSelector.Key, shortcutSelector.Modifiers);
                    foreach (Process exe in Process.GetProcesses())
                    {
                        if (exe.ProcessName == "explorer")
                            exe.Kill();
                    }
                }

                if (ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                    shortcutSelector.Key,
                    shortcutSelector.Modifiers))
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
            int selectedIndex = menu.Items.IndexOf(selectedItem);

            (menu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = false;
            (menu.Items[selectedIndex] as MenuItem).IsChecked = false;

            int[] fastSortExceptions = { 9, 10, 17, 18 };
            if (EverythingSearch.Instance.GetIsFastSort((uint)selectedIndex + 1) ||
                fastSortExceptions.Contains(selectedIndex + 1))
            {
                Properties.Settings.Default.sortBy = selectedIndex + 1;
            }
            else
            {
                MessageBox.Show(Properties.Resources.MessageBoxFastSortUnavailable,
                                Properties.Resources.MessageBoxFastSortUnavailableTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Asterisk);
            }

            (menu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
        }

        private void OnItemTemplateClicked(object sender, RoutedEventArgs e)
        {
            var selectedItem = sender as MenuItem;
            var menu = selectedItem.Parent as MenuItem;

            for (int i = 0; i < menu.Items.Count; i++)
            {
                var menuItem = menu.Items[i] as MenuItem;
                if (menuItem == selectedItem)
                {
                    menuItem.IsChecked = true;
                    Properties.Settings.Default.itemTemplate = selectedItem.Tag.ToString();
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
