using SearchResult = EverythingToolbar.App.Data.SearchResult;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchResultPreviewPaneViewModel
    {
        private readonly SearchResultActions _actions;
        private readonly CustomActionService _customActions;
        private readonly ISearchWindowController _controller;

        public ISettings Settings { get; }

        public SearchResultPreviewPaneViewModel(
            SearchResultActions actions,
            CustomActionService customActions,
            ISettings settings,
            ISearchWindowController controller)
        {
            _actions = actions;
            _customActions = customActions;
            Settings = settings;
            _controller = controller;
        }

        public void Open(SearchResult result)
        {
            if (!_customActions.TryRun(result))
                _actions.Open(result);
            _controller.Hide();
        }

        public void OpenPath(SearchResult result)
        {
            _actions.OpenPath(result);
            _controller.Hide();
        }

        public void OpenWith(SearchResult result)
        {
            _actions.OpenWith(result);
            _controller.Hide();
        }

        public void ShowInEverything(SearchResult result)
        {
            _actions.ShowInEverything(result);
            _controller.Hide();
        }

        public void ShowProperties(SearchResult result)
        {
            _actions.ShowProperties(result);
            _controller.Hide();
        }
    }
}
