﻿using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar.Controls
{
    public partial class SettingsControl
    {
        public SettingsControl()
        {
            InitializeComponent();

            SelectSortType();
            SelectItemTemplate();
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
                SearchResultProvider.SetInstanceName(ToolbarSettings.User.InstanceName);
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
                    // Windows Explorer reserves many shortcuts with the Windows key. Therefore, we need to update the settings,
                    // kill explorer (and the deskband) and let the initialize routine set the shortcut before explorer has time to do so.
                    ShortcutManager.UpdateSettings(shortcutSelector.Key, shortcutSelector.Modifiers);
                    foreach (var exe in Process.GetProcesses())
                    {
                        if (exe.ProcessName == "explorer")
                            exe.Kill();
                    }
                }

                ShortcutManager.TrySetShortcut(shortcutSelector.Key, shortcutSelector.Modifiers);
            }
        }

        private void OnSortByClicked(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem selectedItem)) return;
            var selectedIndex = SortByMenu.Items.IndexOf(selectedItem);

            int[] fastSortExceptions = { 4, 8 };
            if (SearchResultProvider.GetIsFastSort(selectedIndex, ToolbarSettings.User.IsSortDescending) ||
                fastSortExceptions.Contains(selectedIndex))
            {
                ToolbarSettings.User.SortBy = selectedIndex;
            }
            else
            {
                MessageBox.Show(Properties.Resources.MessageBoxFastSortUnavailable,
                    Properties.Resources.MessageBoxFastSortUnavailableTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Asterisk);
            }

            SelectSortType();
        }

        private void OnSortAscendingClicked(object sender, RoutedEventArgs e)
        {
            ToolbarSettings.User.IsSortDescending = false;
            SelectSortType();
        }

        private void OnSortDescendingClicked(object sender, RoutedEventArgs e)
        {
            ToolbarSettings.User.IsSortDescending = true;
            SelectSortType();
        }

        private void SelectSortType()
        {
            foreach (var item in SortByMenu.Items)
            {
                if (item is MenuItem menuItem)
                    menuItem.IsChecked = false;
            }

            if (SortByMenu.Items[ToolbarSettings.User.SortBy] is MenuItem sortByMenuItem)
                sortByMenuItem.IsChecked = true;

            if (ToolbarSettings.User.IsSortDescending)
                SortDescendingMenuItem.IsChecked = true;
            else
                SortAscendingMenuItem.IsChecked = true;
        }

        private void SelectItemTemplate()
        {
            foreach (MenuItem menuItem in ItemTemplateMenu.Items)
            {
                var selectedItemTemplate = menuItem.Tag.ToString();
                menuItem.IsChecked = selectedItemTemplate == ToolbarSettings.User.ItemTemplate;
            }
        }

        private void OnItemTemplateClicked(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem selectedItem)) return;

            ToolbarSettings.User.ItemTemplate = selectedItem.Tag.ToString();

            SelectItemTemplate();
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