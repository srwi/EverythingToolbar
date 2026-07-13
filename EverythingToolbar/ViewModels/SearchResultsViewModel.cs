using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchResultsViewModel
    {
        public SearchSession Session { get; }
        public SearchResultActions Actions { get; }
        public CustomActionService CustomActions { get; }
        public ISettings Settings { get; }
        public ISearchWindowController SearchWindowController { get; }
        public SearchCommands Commands { get; }

        public SearchResultsViewModel(
            SearchSession session,
            SearchResultActions actions,
            CustomActionService customActions,
            ISettings settings,
            ISearchWindowController searchWindowController,
            SearchCommands commands)
        {
            Session = session;
            Actions = actions;
            CustomActions = customActions;
            Settings = settings;
            SearchWindowController = searchWindowController;
            Commands = commands;
        }
    }
}
