using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Settings;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class SettingsControl
    {
        private readonly SettingsControlViewModel _viewModel =
            Ioc.Default.GetRequiredService<SettingsControlViewModel>();

        public SettingsControl()
        {
            InitializeComponent();
            DataContext = _viewModel;

            SelectSortType();
        }

        private void OpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            _viewModel.HideWindow();
            Window settings = new SettingsWindow();
            settings.Show();
        }

        private void OnSortByClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem selectedItem)
                return;

            int selectedIndex = SortByMenu.Items.IndexOf(selectedItem);

            if (!_viewModel.TrySetSortBy(selectedIndex))
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
            _viewModel.SetSortDescending(false);
            SelectSortType();
        }

        private void OnSortDescendingClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.SetSortDescending(true);
            SelectSortType();
        }

        private void SelectSortType()
        {
            foreach (var item in SortByMenu.Items)
            {
                if (item is MenuItem menuItem)
                    menuItem.IsChecked = false;
            }

            if (SortByMenu.Items[_viewModel.Settings.SortBy] is MenuItem sortByMenuItem)
                sortByMenuItem.IsChecked = true;

            if (_viewModel.Settings.IsSortDescending)
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
            _viewModel.TogglePreviewPane();
        }
    }
}
