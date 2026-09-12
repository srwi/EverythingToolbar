using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using NLog;

namespace EverythingToolbar.ViewModels
{
    public sealed partial class SearchButtonViewModel : ObservableObject
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchButtonViewModel>();

        private readonly ThemeService _themeService;
        private readonly SearchWindowController _controller;
        private readonly ISettings _settings;

        [ObservableProperty]
        private bool _isActive;

        private ImageSource? _iconSource;

        public SearchButtonViewModel(ThemeService themeService, SearchWindowController controller, ISettings settings)
        {
            _themeService = themeService;
            _controller = controller;
            _settings = settings;

            _settings.PropertyChanged += OnSettingsPropertyChanged;
            _themeService.ThemeChanged += OnThemeChanged;
            _controller.ActiveChanged += OnActiveChanged;
        }

        public ImageSource? IconSource => _iconSource ??= LoadIconSource();

        public void Toggle() => _controller.Toggle();

        public void SetIconMode(bool isIcon) => _controller.SetIconMode(isIcon);

        public void Cleanup()
        {
            _settings.PropertyChanged -= OnSettingsPropertyChanged;
            _themeService.ThemeChanged -= OnThemeChanged;
            _controller.ActiveChanged -= OnActiveChanged;
        }

        private void OnActiveChanged(object? sender, bool isActive)
        {
            IsActive = isActive;
        }

        private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.IconName))
            {
                _iconSource = null;
                OnPropertyChanged(nameof(IconSource));
            }
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            _iconSource = null;
            OnPropertyChanged(nameof(IconSource));
        }

        private ImageSource? LoadIconSource()
        {
            var iconName = GetThemedIconName();
            var resourcePath = iconName.Replace('\\', '/').TrimStart('/');
            string?[] candidates =
            [
                Path.IsPathRooted(iconName) ? iconName : null,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName),
                $"pack://application:,,,/EverythingToolbar;component/{resourcePath}",
                "pack://application:,,,/EverythingToolbar;component/Images/AppIcon.ico",
            ];

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                    continue;

                if (!candidate.StartsWith("pack:", StringComparison.OrdinalIgnoreCase) && !File.Exists(candidate))
                    continue;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(candidate, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Could not load icon from candidate: {Candidate}", candidate);
                }
            }

            return null;
        }

        private string GetThemedIconName()
        {
            // If the user selected a custom or non-default icon (e.g. the Blue icon), respect that choice.
            if (!string.IsNullOrWhiteSpace(_settings.IconName) &&
                !string.Equals(_settings.IconName, "Icons/Dark.ico", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(_settings.IconName, "Icons/Light.ico", StringComparison.OrdinalIgnoreCase))
            {
                return _settings.IconName;
            }

            var systemTheme = _themeService.GetEffectiveTheme(ThemeFlavor.System);
            return systemTheme == Theme.Light ? "Icons/Light.ico" : "Icons/Dark.ico";
        }
    }
}
