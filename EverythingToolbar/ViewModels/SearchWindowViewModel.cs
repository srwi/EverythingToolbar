namespace EverythingToolbar.ViewModels
{
    public sealed class SearchWindowViewModel
    {
        private readonly SearchState _searchState;
        private readonly EverythingSearchLauncher _launcher;
        private readonly ISettings _settings;

        public WindowsPolicy WindowsPolicy { get; }
        public ThemeService ThemeService { get; }

        public SearchWindowViewModel(
            SearchState searchState,
            EverythingSearchLauncher launcher,
            ISettings settings,
            WindowsPolicy windowsPolicy,
            ThemeService themeService
        )
        {
            _searchState = searchState;
            _launcher = launcher;
            _settings = settings;
            WindowsPolicy = windowsPolicy;
            ThemeService = themeService;
        }

        public bool AnimationsDisabled => WindowsPolicy.IsEffectiveAnimationsDisabled;
        public bool IsWindows11OrGreater => WindowsPolicy.GetEffectiveWindowsVersion() >= WindowsVersion.Windows11;

        public int PopupWidth => _settings.PopupWidth;
        public int PopupHeight => _settings.PopupHeight;

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
