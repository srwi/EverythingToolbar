using EverythingToolbar.Search;
using EverythingToolbar.Settings;
using System;
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
        }

        private void OpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Hide();
            Window settings = new SettingsWindow();
            settings.Show();
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