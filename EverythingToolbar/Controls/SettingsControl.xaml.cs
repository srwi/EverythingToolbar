﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Controls
{
    public partial class SettingsControl : Grid
    {
        public SettingsControl()
        {
            InitializeComponent();

            // Preselect sorting method
            (SortByMenu.Items[Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;

            // Preselect active datatemplate
            for (int i = 0; i < ItemTemplateMenu.Items.Count; i++)
            {
                MenuItem menuItem = ItemTemplateMenu.Items[i] as MenuItem;
                if (menuItem.Tag.ToString() == Settings.Default.itemTemplate)
                    menuItem.IsChecked = true;
                else
                    menuItem.IsChecked = false;
            }

            // IsEnabled property of matchWholeWord menu item needs to be handled
            // in code because DataTriggers are not compatible with DynamicResources as MenuItem styles
            Settings.Default.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled" || e.PropertyName == "CurrentFilter")
            {
                bool newEnabledState = !Settings.Default.isRegExEnabled && EverythingSearch.Instance.CurrentFilter.IsMatchWholeWord == null;
                IsMatchWholeWordMenuItem.IsEnabled = newEnabledState;
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
                                              Settings.Default.instanceName);
            if (inputDialog.ShowDialog() == true)
            {
                Settings.Default.instanceName = inputDialog.ResponseText;
                EverythingSearch.Instance.SetInstanceName(Settings.Default.instanceName);
            }
        }

        private void OpenShortcutWindow(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
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
            int selectedIndex = menu.Items.IndexOf(selectedItem);

            (menu.Items[Settings.Default.sortBy - 1] as MenuItem).IsChecked = false;
            (menu.Items[selectedIndex] as MenuItem).IsChecked = false;

            int[] fastSortExceptions = { 9, 10, 17, 18 };
            if (EverythingSearch.Instance.GetIsFastSort(selectedIndex + 1) ||
                fastSortExceptions.Contains(selectedIndex + 1))
            {
                Settings.Default.sortBy = selectedIndex + 1;
            }
            else
            {
                MessageBox.Show(Properties.Resources.MessageBoxFastSortUnavailable,
                                Properties.Resources.MessageBoxFastSortUnavailableTitle,
                                MessageBoxButton.OK,
                                MessageBoxImage.Asterisk);
            }

            (menu.Items[Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
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
                    Settings.Default.itemTemplate = selectedItem.Tag.ToString();
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
