using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using EverythingToolbar.Settings;

namespace EverythingToolbar.Controls
{
    public partial class SettingsControl
    {
        private readonly ISearchWindowController _searchWindowController = Ioc.Default.GetRequiredService<ISearchWindowController>();

        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

        public SettingsControl()
        {
            InitializeComponent();
            DataContext = this;

            SelectSortType();
        }

        private void OpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            _searchWindowController.Hide();
            Window settings = new SettingsWindow();
            settings.Show();
        }

        private void OnSortByClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem selectedItem)
                return;

            int selectedIndex = SortByMenu.Items.IndexOf(selectedItem);

            int[] fastSortExceptions = [4, 8];
            if (
                Ioc.Default.GetRequiredService<IEverythingClient>().GetIsFastSort((SortBy)selectedIndex, Settings.IsSortDescending)
                || fastSortExceptions.Contains(selectedIndex)
            )
            {
                Settings.SortBy = selectedIndex;
            }
            else
            {
                FluentMessageBox
                    .CreateRegular(
                        Properties.Resources.MessageBoxFastSortUnavailable,
                        Properties.Resources.MessageBoxFastSortUnavailableTitle
                    )
                    .ShowDialogAsync();
            }

            SelectSortType();
        }

        private void OnSortAscendingClicked(object sender, RoutedEventArgs e)
        {
            Settings.IsSortDescending = false;
            SelectSortType();
        }

        private void OnSortDescendingClicked(object sender, RoutedEventArgs e)
        {
            Settings.IsSortDescending = true;
            SelectSortType();
        }

        private void SelectSortType()
        {
            foreach (var item in SortByMenu.Items)
            {
                if (item is MenuItem menuItem)
                    menuItem.IsChecked = false;
            }

            if (SortByMenu.Items[Settings.SortBy] is MenuItem sortByMenuItem)
                sortByMenuItem.IsChecked = true;

            if (Settings.IsSortDescending)
                SortDescendingMenuItem.IsChecked = true;
            else
                SortAscendingMenuItem.IsChecked = true;
        }

        private void OpenButtonContextMenu(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.ContextMenu is not { } contextMenu)
                return;

            contextMenu.PlacementTarget = button;
            contextMenu.Placement = PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }

        private void TogglePreviewPane(object sender, RoutedEventArgs e)
        {
            Settings.IsPreviewPaneEnabled = !Settings.IsPreviewPaneEnabled;
        }
    }
}
