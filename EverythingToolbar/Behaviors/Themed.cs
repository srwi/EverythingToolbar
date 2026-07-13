using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;

namespace EverythingToolbar.Behaviors
{
    public class Themed : Behavior<FrameworkElement>
    {
        // Service-locator reach: behaviors cannot be constructor-injected from XAML.
        private static ThemeService ThemeService => Ioc.Default.GetRequiredService<ThemeService>();

        public ThemedSurface Surface { get; set; } = ThemedSurface.AppWindow;

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject.IsLoaded)
                Register();
            else
                AssociatedObject.Loaded += OnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            ThemeService.Unregister(AssociatedObject);
            base.OnDetaching();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => Register();

        private void Register() => ThemeService.Register(AssociatedObject, Surface);
    }
}