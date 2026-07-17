using System.Windows.Input;
using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchBoxViewModel
    {
        private readonly SearchState _searchState;
        private readonly ISearchWindowController _controller;
        private readonly SearchCommands _commands;

        public ISettings Settings { get; }

        public SearchBoxViewModel(
            SearchState searchState,
            ISettings settings,
            ISearchWindowController searchWindowController,
            SearchCommands commands)
        {
            _searchState = searchState;
            Settings = settings;
            _controller = searchWindowController;
            _commands = commands;
        }

        public void HideWindow() => _controller.Hide();

        public void CycleFilters(int offset) => _searchState.CycleFilters(offset);

        public string PreviousHistoryTerm() => _searchState.GetPreviousSearchTerm();

        public string NextHistoryTerm() => _searchState.GetNextSearchTerm();

        public bool TryHandleResultsGesture(Key key, Key systemKey, ModifierKeys modifiers) =>
            _commands.TranslateResultsGesture(key, systemKey, modifiers, fromSearchBox: true);
    }
}
