using EverythingToolbar.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SettingsControl : Grid
    {
        private readonly ResourceLoader themes = new ResourceLoader("Themes", Properties.Resources.SettingsTheme);
        private readonly ResourceLoader itemTemplates = new ResourceLoader("ItemTemplates", Properties.Resources.SettingsView);

        public SettingsControl()
        {
            InitializeComponent();

            Binding themeMenuBinding = new Binding()
            {
                Converter = new ResourceLoaderToMenuItemsConverter(),
                ConverterParameter = "theme",
                Source = themes.Resources
            };
            ThemeMenu.SetBinding(ItemsControl.ItemsSourceProperty, themeMenuBinding);
            Binding itemTemplateMenuBinding = new Binding()
            {
                Converter = new ResourceLoaderToMenuItemsConverter(),
                ConverterParameter = "itemTemplate",
                Source = itemTemplates.Resources
            };
            ItemTemplateMenu.SetBinding(ItemsControl.ItemsSourceProperty, itemTemplateMenuBinding);

            (SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
        }

        private void OpenAboutWindow(object sender, RoutedEventArgs e)
        {
            EverythingSearch.Instance.Reset();

            Window about = new About();
            about.Show();
        }

        private void OpenRulesWindow(object sender, RoutedEventArgs e)
        {
            EverythingSearch.Instance.Reset();

            Window rules = new Rules();
            rules.Show();
        }
        
        private void OpenShortcutWindow(object sender, RoutedEventArgs e)
        {
            EverythingSearch.Instance.Reset();

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

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void OnSortByClicked(object sender, RoutedEventArgs e)
        {
            MenuItem selectedItem = (MenuItem)sender;
            MenuItem menu = (MenuItem)selectedItem.Parent;
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
            Properties.Settings.Default.Save();
        }

        private void OnThemeClicked(object sender, RoutedEventArgs e)
        {
            MenuItem itemChecked = (MenuItem)e.OriginalSource;
            MenuItem itemParent = (MenuItem)sender;

            for (int i = 0; i < itemParent.Items.Count; i++)
            {
                if (itemParent.Items[i] == itemChecked)
                {
                    (itemParent.Items[i] as MenuItem).IsChecked = true;
                    Properties.Settings.Default.theme = itemChecked.Tag.ToString();
                    continue;
                }

                (itemParent.Items[i] as MenuItem).IsChecked = false;
            }

            ApplicationResources.Instance.ApplyTheme(itemChecked.Tag.ToString());
        }

        private void OnItemTemplateClicked(object sender, RoutedEventArgs e)
        {
            MenuItem itemChecked = (MenuItem)e.OriginalSource;
            MenuItem itemParent = (MenuItem)sender;

            for (int i = 0; i < itemParent.Items.Count; i++)
            {
                if (itemParent.Items[i] == itemChecked)
                {
                    (itemParent.Items[i] as MenuItem).IsChecked = true;
                    Properties.Settings.Default.itemTemplate = itemChecked.Tag.ToString();
                    continue;
                }

                (itemParent.Items[i] as MenuItem).IsChecked = false;
            }

            ApplicationResources.Instance.ApplyItemTemplate(itemChecked.Tag.ToString());
        }

        private void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;

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
