using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class SearchButton
    {
        private readonly SearchButtonViewModel _viewModel = Ioc.Default.GetRequiredService<SearchButtonViewModel>();

        public SearchButton()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ActiveChanged -= OnSearchWindowActiveChanged;
            _viewModel.ActiveChanged += OnSearchWindowActiveChanged;

            _viewModel.ThemeChanged -= UpdateTheme;
            _viewModel.ThemeChanged += UpdateTheme;

            // ThemeChanged only fires on subsequent changes, never for the initial state,
            // so apply the current theme here or the icon keeps WPF's default (black) in dark mode.
            UpdateTheme(_viewModel.CurrentSystemTheme);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.ThemeChanged -= UpdateTheme;
            _viewModel.ActiveChanged -= OnSearchWindowActiveChanged;
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
        }

        private void OnClick(object? sender, RoutedEventArgs e)
        {
            _viewModel.Toggle();
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel.SetIconMode((bool)e.NewValue);
        }
    }
}
