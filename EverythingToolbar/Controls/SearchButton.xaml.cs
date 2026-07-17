using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Controls
{
    public partial class SearchButton
    {
        private readonly ThemeService _themeService = Ioc.Default.GetRequiredService<ThemeService>();
        private readonly SearchWindowController _searchWindowController = Ioc.Default.GetRequiredService<SearchWindowController>();

        public SearchButton()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _searchWindowController.ActiveChanged -= OnSearchWindowActiveChanged;
            _searchWindowController.ActiveChanged += OnSearchWindowActiveChanged;

            _themeService.ThemeChanged -= UpdateTheme;
            _themeService.ThemeChanged += UpdateTheme;

            // ThemeService.ThemeChanged only fires on subsequent changes, never for the initial state,
            // so apply the current theme here or the icon keeps WPF's default (black) in dark mode.
            UpdateTheme(_themeService.GetEffectiveTheme(ThemeFlavor.System));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _themeService.ThemeChanged -= UpdateTheme;
            _searchWindowController.ActiveChanged -= OnSearchWindowActiveChanged;
        }

        private void OnSearchWindowActiveChanged(object? sender, bool isActive)
        {
            if (Template.FindName("OuterBorder", this) is not Border border)
                return;

            border.Background = isActive
                ? new SolidColorBrush(Color.FromArgb(64, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void UpdateTheme(Theme newTheme)
        {
            if (Template.FindName("OuterBorder", this) is not Border border)
                return;

            if (newTheme == Theme.Light)
            {
                Foreground = new SolidColorBrush(Colors.Black);
                border.Opacity = 0.55;
            }
            else
            {
                Foreground = new SolidColorBrush(Colors.White);
                border.Opacity = 0.2;
            }
        }

        private void UpdateTheme(object? sender, ThemeChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTheme(e.SystemTheme);
            else
                Loaded += (_, _) =>
                {
                    UpdateTheme(e.SystemTheme);
                };
        }

        private void OnClick(object? sender, RoutedEventArgs e)
        {
            _searchWindowController.Toggle();
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _searchWindowController.SetIconMode((bool)e.NewValue);
        }
    }
}
