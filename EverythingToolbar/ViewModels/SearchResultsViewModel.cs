using System.Collections.Generic;
using System.Windows.Input;
using EverythingToolbar.Search;
using SearchResult = EverythingToolbar.App.Data.SearchResult;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchResultsViewModel
    {
        private readonly CustomActionService _customActions;
        private readonly ISearchWindowController _controller;
        private readonly SearchCommands _commands;

        public SearchSession Session { get; }
        public ISettings Settings { get; }

        public SearchResultsViewModel(
            SearchSession session,
            CustomActionService customActions,
            ISettings settings,
            ISearchWindowController searchWindowController,
            SearchCommands commands
        )
        {
            Session = session;
            _customActions = customActions;
            Settings = settings;
            _controller = searchWindowController;
            _commands = commands;
        }

        public bool IsDoubleClickToOpen => Settings.IsDoubleClickToOpen;
        public bool IsSystemContextMenuDefault => Settings.IsSystemContextMenuDefault;

        public void HideWindow() => _controller.Hide();

        public bool TryHandleResultsGesture(Key key, Key systemKey, ModifierKeys modifiers) =>
            _commands.TranslateResultsGesture(key, systemKey, modifiers, fromSearchBox: false);

        public void OpenSelected() => _commands.OpenSelected();

        public void OpenSelectedPath() => _commands.OpenSelectedPath();

        public void RunSelectedAsAdmin() => _commands.RunSelectedAsAdmin();

        public void ShowSelectedProperties() => _commands.ShowSelectedProperties();

        public void OpenSelectedWith() => _commands.OpenSelectedWith();

        public void CopySelected() => _commands.CopySelected();

        public void CopySelectedPath() => _commands.CopySelectedPath();

        public void PreviewSelected() => _commands.PreviewSelected();

        public void ShowSelectedWindowsContextMenu() => _commands.ShowSelectedWindowsContextMenu();

        public void ShowSelectedInEverything() => _commands.ShowSelectedInEverything();

        public List<Rule> LoadCustomActions() => _customActions.Load();

        public bool TryRunCustomAction(SearchResult item, string command = "") => _customActions.TryRun(item, command);
    }
}
