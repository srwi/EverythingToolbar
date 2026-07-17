using System.Linq;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.ViewModels
{
    public sealed class SettingsControlViewModel
    {
        private readonly ISearchWindowController _controller;
        private readonly IEverythingClient _everythingClient;

        public ISettings Settings { get; }

        public SettingsControlViewModel(
            ISearchWindowController searchWindowController,
            ISettings settings,
            IEverythingClient everythingClient)
        {
            _controller = searchWindowController;
            Settings = settings;
            _everythingClient = everythingClient;
        }

        public void HideWindow() => _controller.Hide();

        public bool TrySetSortBy(int index)
        {
            int[] fastSortExceptions = [4, 8];
            if (_everythingClient.GetIsFastSort((SortBy)index, Settings.IsSortDescending)
                || fastSortExceptions.Contains(index))
            {
                Settings.SortBy = index;
                return true;
            }

            return false;
        }

        public void SetSortDescending(bool descending) => Settings.IsSortDescending = descending;

        public void TogglePreviewPane() => Settings.IsPreviewPaneEnabled = !Settings.IsPreviewPaneEnabled;
    }
}
