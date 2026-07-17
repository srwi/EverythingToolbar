using System;

namespace EverythingToolbar.ViewModels
{
    public sealed class SearchButtonViewModel
    {
        private readonly ThemeService _themeService;
        private readonly SearchWindowController _controller;

        public SearchButtonViewModel(ThemeService themeService, SearchWindowController controller)
        {
            _themeService = themeService;
            _controller = controller;
        }

        // The current system theme, used to paint the icon on load (ThemeChanged only fires on later changes).
        public Theme CurrentSystemTheme => _themeService.GetEffectiveTheme(ThemeFlavor.System);

        // Forwarded so the control depends only on this view-model, not on ThemeService/SearchWindowController.
        public event EventHandler<ThemeChangedEventArgs> ThemeChanged
        {
            add => _themeService.ThemeChanged += value;
            remove => _themeService.ThemeChanged -= value;
        }

        public event EventHandler<bool> ActiveChanged
        {
            add => _controller.ActiveChanged += value;
            remove => _controller.ActiveChanged -= value;
        }

        public void Toggle() => _controller.Toggle();

        public void SetIconMode(bool isIcon) => _controller.SetIconMode(isIcon);
    }
}
