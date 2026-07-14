using System;
using System.Windows.Input;
using EverythingToolbar.Helpers;
using EverythingToolbar.Settings;
using SearchResult = EverythingToolbar.Data.SearchResult;

namespace EverythingToolbar.Search
{
    public sealed class SearchCommands
    {
        private readonly SearchSession _session;
        private readonly SearchResultActions _actions;
        private readonly ISearchWindowController _controller;
        private readonly MatchOptions _matchOptions;
        private readonly SearchOptions _searchOptions;
        private readonly SearchState _searchState;

        public SearchCommands(
            SearchSession session,
            SearchResultActions actions,
            ISearchWindowController controller,
            MatchOptions matchOptions,
            SearchOptions searchOptions,
            SearchState searchState
        )
        {
            _session = session;
            _actions = actions;
            _controller = controller;
            _matchOptions = matchOptions;
            _searchOptions = searchOptions;
            _searchState = searchState;
        }

        public bool TranslateResultsGesture(Key key, Key systemKey, ModifierKeys modifiers, bool fromSearchBox)
        {
            if (key == Key.Enter && modifiers == ModifierKeys.None)
            {
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
                _matchOptions.IsMatchCase = !_matchOptions.IsMatchCase;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.B)
            {
                _matchOptions.IsMatchWholeWord = !_matchOptions.IsMatchWholeWord;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.U)
            {
                _matchOptions.IsMatchPath = !_matchOptions.IsMatchPath;
                return true;
            }
            if (modifiers == ModifierKeys.Control && key == Key.R)
            {
                _matchOptions.IsRegExEnabled = !_matchOptions.IsRegExEnabled;
                return true;
            }

            if (modifiers == ModifierKeys.Control && key is >= Key.D0 and <= Key.D9)
            {
                var index = key == Key.D0 ? 9 : key - Key.D1;
                _searchState.SelectFilterFromIndex(index);
                return true;
            }

            switch (key)
            {
                case Key.Up:
                    _session.MoveUp();
                    return true;
                case Key.Down:
                    _session.MoveDown();
                    return true;
                case Key.PageUp:
                    _session.PageUp();
                    return true;
                case Key.PageDown:
                    _session.PageDown();
                    return true;
                case Key.Home:
                    if (CanHomeEndNavigate(modifiers, fromSearchBox))
                    {
                        _session.SelectFirst();
                        return true;
                    }
                    return false;
                case Key.End:
                    if (CanHomeEndNavigate(modifiers, fromSearchBox))
                    {
                        _session.SelectLast();
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private bool CanHomeEndNavigate(ModifierKeys modifiers, bool fromSearchBox) =>
            !fromSearchBox || (modifiers != ModifierKeys.Shift && _searchOptions.IsHomeEndNavigateResults);


        public void OpenSelected()
        {
            var item = _session.SelectedResult;
            if (item == null)
            {
                _session.MoveDown();
                return;
            }

            if (!CustomActions.HandleAction(item))
                _actions.Open(item);

            _controller.Hide();
            _session.ClearSelection();
        }

        public void OpenSelectedPath() => InvokeAndHide(_actions.OpenPath);

        public void RunSelectedAsAdmin() => InvokeAndHide(_actions.RunAsAdmin);

        public void ShowSelectedProperties() => InvokeAndHide(_actions.ShowProperties);

        public void OpenSelectedWith() => InvokeAndHide(_actions.OpenWith);

        public void ShowSelectedInEverything()
        {
            if (_session.SelectedResult is { } item)
                _actions.ShowInEverything(item);
            _session.ClearSelection();
        }

        public void CopySelected() => Invoke(_actions.CopyToClipboard);

        public void CopySelectedPath() => Invoke(_actions.CopyPathToClipboard);

        public void ShowSelectedWindowsContextMenu() => Invoke(_actions.ShowWindowsContextMenu);

        public void PreviewSelected()
        {
            if (_session.SelectedResult is { } item)
            {
                _actions.PreviewInQuickLook(item);
                _actions.PreviewInSeer(item);
            }
        }

        private void InvokeAndHide(Action<SearchResult> action)
        {
            if (_session.SelectedResult is { } item)
                action(item);
            _controller.Hide();
            _session.ClearSelection();
        }

        private void Invoke(Action<SearchResult> action)
        {
            if (_session.SelectedResult is { } item)
                action(item);
        }
    }
}