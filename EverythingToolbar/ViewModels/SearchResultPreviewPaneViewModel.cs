using EverythingToolbar.Search;
using SearchResult = EverythingToolbar.App.Data.SearchResult;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchResultPreviewPaneViewModel
    {
        private readonly SearchCommands _commands;

        public ISettings Settings { get; }

        public SearchResultPreviewPaneViewModel(ISettings settings, SearchCommands commands)
        {
            Settings = settings;
            _commands = commands;
        }

        public void Open(SearchResult result) => _commands.OpenSelected(result);

        public void OpenPath(SearchResult result) => _commands.OpenSelectedPath(result);

        public void OpenWith(SearchResult result) => _commands.OpenSelectedWith(result);

        public void ShowInEverything(SearchResult result) => _commands.ShowSelectedInEverything(result);

        public void ShowProperties(SearchResult result) => _commands.ShowSelectedProperties(result);
    }
}
