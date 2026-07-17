using System;
using System.Windows.Input;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Search
{
    public sealed class SearchCommands
    {
        private readonly SearchSession _session;
        private readonly SearchResultActions _actions;
        private readonly CustomActionService _customActions;
        private readonly SearchWindowController _controller;
        private readonly ISettings _settings;
        private readonly SearchState _searchState;

        public SearchCommands(
            SearchSession session,
            SearchResultActions actions,
            CustomActionService customActions,
            SearchWindowController controller,
            ISettings settings,
            SearchState searchState
        )
        {
            _session = session;
            _actions = actions;
            _customActions = customActions;
            _controller = controller;
            _settings = settings;
            _searchState = searchState;
        }

        public bool TranslateResultsGesture(Key key, Key systemKey, ModifierKeys modifiers, bool fromSearchBox)
        {
            if (key == Key.Enter && modifiers == ModifierKeys.None)
            {
                if (_session.SelectedResult == null)
                    _session.MoveDown();
                else
                    OpenSelected();
                return true;
            }
            if (key == Key.Enter && modifiers == ModifierKeys.Control)
            {
                OpenSelectedPath();
                return true;
            }
            if (key == Key.Enter && modifiers == ModifierKeys.Shift)
            {
                ShowSelectedInEverything();
                return true;
            }
            if (key == Key.Enter && modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                RunSelectedAsAdmin();
                return true;
            }
            if ((key == Key.Enter || systemKey == Key.Enter) && modifiers == ModifierKeys.Alt)
            {
                ShowSelectedProperties();
                return true;
            }

            if (modifiers == ModifierKeys.Control && key == Key.I)
            {
                _settings.IsMatchCase = !_settings.IsMatchCase;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.B)
            {
                _settings.IsMatchWholeWord = !_settings.IsMatchWholeWord;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.U)
            {
                _settings.IsMatchPath = !_settings.IsMatchPath;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.R)
            {
                _settings.IsRegExEnabled = !_settings.IsRegExEnabled;
                return true;
            }

            if (modifiers == ModifierKeys.Control && key is >= Key.D0 and <= Key.D9)
            {
                var index = key == Key.D0 ? 9 : key - Key.D1;
                _searchState.SelectFilterFromIndex(index);
                return true;
            }

            if (key == Key.Tab && modifiers is ModifierKeys.None or ModifierKeys.Shift)
            {
                _searchState.CycleFilters(modifiers == ModifierKeys.Shift ? -1 : 1);
                return true;
            }

            switch (key)
            {
                case Key.Up:
                    _session.MoveUp();
                    break;
                case Key.Down:
                    _session.MoveDown();
                    break;
                case Key.PageUp:
                    _session.PageUp();
                    break;
                case Key.PageDown:
                    _session.PageDown();
                    break;
                case Key.Home when CanHomeEndNavigate(modifiers, fromSearchBox):
                    _session.SelectFirst();
                    break;
                case Key.End when CanHomeEndNavigate(modifiers, fromSearchBox):
                    _session.SelectLast();
                    break;
                default:
                    return false;
            }

            SyncFocusToSelection();
            return true;
        }

        private bool CanHomeEndNavigate(ModifierKeys modifiers, bool fromSearchBox) =>
            !fromSearchBox || (modifiers != ModifierKeys.Shift && _settings.IsHomeEndNavigateResults);

        private void SyncFocusToSelection()
        {
            if (_session.KeepSearchBoxFocused)
                return;

            if (_session.SelectedIndex < 0)
                _controller.FocusSearchBox();
            else
                _controller.FocusSelectedResult();
        }

        public void OpenSelected(SearchResult? target = null) =>
            Act(target, RunOrOpen, hide: true, clearSelection: true);

        public void OpenSelectedPath(SearchResult? target = null) =>
            Act(target, _actions.OpenPath, hide: true, clearSelection: true);

        public void RunSelectedAsAdmin(SearchResult? target = null) =>
            Act(target, _actions.RunAsAdmin, hide: true, clearSelection: true);

        public void ShowSelectedProperties(SearchResult? target = null) =>
            Act(target, _actions.ShowProperties, hide: true, clearSelection: true);

        public void OpenSelectedWith(SearchResult? target = null) =>
            Act(target, _actions.OpenWith, hide: true, clearSelection: true);

        public void ShowSelectedInEverything(SearchResult? target = null) =>
            Act(target, _actions.ShowInEverything, hide: true, clearSelection: true);

        public void CopySelected(SearchResult? target = null) =>
            Act(target, _actions.CopyToClipboard, hide: false, clearSelection: false);

        public void CopySelectedPath(SearchResult? target = null) =>
            Act(target, _actions.CopyPathToClipboard, hide: false, clearSelection: false);

        public void ShowSelectedWindowsContextMenu(SearchResult? target = null) =>
            Act(target, _actions.ShowWindowsContextMenu, hide: false, clearSelection: false);

        public void PreviewSelected(SearchResult? target = null) =>
            Act(target, _actions.Preview, hide: false, clearSelection: false);

        private void RunOrOpen(SearchResult item)
        {
            if (!_customActions.TryRun(item))
                _actions.Open(item);
        }

        private void Act(SearchResult? target, Action<SearchResult> action, bool hide, bool clearSelection)
        {
            var item = target ?? _session.SelectedResult;
            if (item != null)
                action(item);
            if (hide)
                _controller.Hide();
            if (clearSelection)
                _session.ClearSelection();
        }
    }
}
