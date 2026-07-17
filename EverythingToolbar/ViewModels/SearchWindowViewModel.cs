namespace EverythingToolbar.ViewModels
{
    public sealed class SearchWindowViewModel
    {
        private readonly SearchState _searchState;
        private readonly EverythingSearchLauncher _launcher;
        private readonly ISettings _settings;

        public WindowsPolicyService WindowsPolicyService { get; }
        public ThemeService ThemeService { get; }

        public SearchWindowViewModel(
            SearchState searchState,
            EverythingSearchLauncher launcher,
            ISettings settings,
            WindowsPolicyService windowsPolicy,
            ThemeService themeService)
        {
            _searchState = searchState;
            _launcher = launcher;
            _settings = settings;
            WindowsPolicyService = windowsPolicy;
            ThemeService = themeService;
        }

        public bool AnimationsDisabled => WindowsPolicyService.IsEffectiveAnimationsDisabled;
        public bool IsWindows11OrGreater => WindowsPolicyService.GetWindowsVersion() >= Utils.WindowsVersion.Windows11;

        public void SelectFilterFromIndex(int index) => _searchState.SelectFilterFromIndex(index);

        public void OpenSearchInEverything() => _launcher.OpenSearchInEverything(_searchState);

        public void ResetSearch() => _searchState.Reset();

        public void SavePopupSize(int width, int height)
        {
            if (_settings.PopupHeight != height || _settings.PopupWidth != width)
            {
                _settings.PopupHeight = height;
                _settings.PopupWidth = width;
            }
        }
    }
}
