using System.Windows.Input;
using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchBoxViewModel
    {
        private readonly SearchState _searchState;
        private readonly SearchWindowController _controller;
        private readonly SearchCommands _commands;
        private readonly IEverythingClient _everythingClient;

        public ISettings Settings { get; }
        public bool IsEverything15Supported => _everythingClient.GetEverythingVersion().Minor >= 5;

        public SearchBoxViewModel(
            SearchState searchState,
            ISettings settings,
            SearchWindowController searchWindowController,
            SearchCommands commands,
            IEverythingClient everythingClient
        )
        {
            _searchState = searchState;
            Settings = settings;
            _controller = searchWindowController;
            _commands = commands;
            _everythingClient = everythingClient;
        }

        public void HideWindow() => _controller.Hide();

        public void Dismiss() => _controller.Dismiss();

        public void NotifyFocusLostToOutside() => _controller.NotifyFocusLostToOutside();

        public void NotifySearchBoxFocused() => _controller.NotifySearchBoxFocused();

        public string PreviousHistoryTerm() => _searchState.GetPreviousSearchTerm();

        public string NextHistoryTerm() => _searchState.GetNextSearchTerm();

        public bool TryHandleResultsGesture(Key key, Key systemKey, ModifierKeys modifiers) =>
            _commands.TranslateResultsGesture(key, systemKey, modifiers, fromSearchBox: true);
    }
}
