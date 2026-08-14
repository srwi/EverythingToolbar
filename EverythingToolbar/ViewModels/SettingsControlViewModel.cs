using System.Linq;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.ViewModels
{
    public sealed class SettingsControlViewModel
    {
        private readonly ISearchWindowController _controller;
        private readonly IEverythingClient _everythingClient;
        private readonly SearchState _searchState;

        public ISettings Settings { get; }
        public SearchState SearchState => _searchState;
        public bool IsEverything15Supported => _everythingClient.GetEverythingVersion().Minor >= 5;

        public SettingsControlViewModel(
            ISearchWindowController searchWindowController,
            ISettings settings,
            IEverythingClient everythingClient,
            SearchState searchState
        )
        {
            _controller = searchWindowController;
            Settings = settings;
            _everythingClient = everythingClient;
            _searchState = searchState;
        }

        public void HideWindow() => _controller.Hide();

        public bool TrySetSortBy(int index)
        {
            int[] fastSortExceptions = [4, 8];
            if (
                _everythingClient.GetIsFastSort((SortBy)index, Settings.IsSortDescending)
                || fastSortExceptions.Contains(index)
            )
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
