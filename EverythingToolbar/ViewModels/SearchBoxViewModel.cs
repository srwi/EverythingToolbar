using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchBoxViewModel
    {
        public SearchState SearchState { get; }
        public ISettings Settings { get; }
        public ISearchWindowController SearchWindowController { get; }
        public SearchCommands Commands { get; }

        public SearchBoxViewModel(
            SearchState searchState,
            ISettings settings,
            ISearchWindowController searchWindowController,
            SearchCommands commands)
        {
            SearchState = searchState;
            Settings = settings;
            SearchWindowController = searchWindowController;
            Commands = commands;
        }
    }
}
