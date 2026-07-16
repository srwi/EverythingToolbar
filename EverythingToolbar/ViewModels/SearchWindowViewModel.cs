using EverythingToolbar.Helpers;
using EverythingToolbar.Search;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchWindowViewModel
    {
        public SearchState SearchState { get; }
        public TaskbarStateManager TaskbarState { get; }
        public EverythingSearchLauncher Launcher { get; }
        public ISettings Settings { get; }
        public WindowsPolicy WindowsPolicy { get; }
        public ThemeService ThemeService { get; }

        public SearchWindowViewModel(
            SearchState searchState,
            TaskbarStateManager taskbarState,
            EverythingSearchLauncher launcher,
            ISettings settings,
            WindowsPolicy windowsPolicy,
            ThemeService themeService)
        {
            SearchState = searchState;
            TaskbarState = taskbarState;
            Launcher = launcher;
            Settings = settings;
            WindowsPolicy = windowsPolicy;
            ThemeService = themeService;
        }
    }
}
