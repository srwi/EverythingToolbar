using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public partial class SearchButton
    {
        private readonly TaskbarStateManager _taskbarState = Ioc.Default.GetRequiredService<TaskbarStateManager>();
        private static ISearchWindowController SearchWindowController =>
            Ioc.Default.GetRequiredService<ISearchWindowController>();

        public SearchButton()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<SearchWindowActiveChanged>(
                this,
                (_, m) => OnSearchWindowActiveChanged(m.IsActive)
            );

            ThemeAwareness.ResourceChanged += UpdateTheme;
        }

        private void OnSearchWindowActiveChanged(bool isActive)
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

        private void UpdateTheme(object? sender, ResourcesChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTheme(e.NewTheme);
            else
                Loaded += (_, _) =>
                {
                    UpdateTheme(e.NewTheme);
                };
        }

        private void OnClick(object? sender, RoutedEventArgs e)
        {
            SearchWindowController.Toggle();
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            _taskbarState.IsIcon = (bool)e.NewValue;
        }
    }
}
