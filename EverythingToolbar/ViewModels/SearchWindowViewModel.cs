using EverythingToolbar.Helpers;
using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchWindowViewModel
    {
        public SearchState SearchState { get; }
        public TaskbarStateService TaskbarState { get; }
        public EverythingSearchLauncher Launcher { get; }
        public ISettings Settings { get; }
        public WindowsPolicyService WindowsPolicyService { get; }
        public ThemeService ThemeService { get; }

        public SearchWindowViewModel(
            SearchState searchState,
            TaskbarStateService taskbarState,
            EverythingSearchLauncher launcher,
            ISettings settings,
            WindowsPolicyService windowsPolicy,
            ThemeService themeService)
        {
            SearchState = searchState;
            TaskbarState = taskbarState;
            Launcher = launcher;
            Settings = settings;
            WindowsPolicyService = windowsPolicy;
            ThemeService = themeService;
        }
    }
}
