using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using Wpf.Ui.Appearance;

namespace EverythingToolbar.Behaviors
{
    public class WpfUiBehavior : Behavior<FrameworkElement>
    {
        private static readonly RegistryEntry SystemThemeRegistryEntry = new(
            "HKEY_CURRENT_USER",
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme"
        );
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject.IsLoaded)
                AutoApplyTheme();
            else
                AssociatedObject.Loaded += (_, _) =>
                {
                    AutoApplyTheme();
                };
        }

        private void ApplyTheme(Theme theme)
        {
            ApplicationThemeManager.Apply(theme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark);
            ApplicationThemeManager.Apply(AssociatedObject);
        }

        private void AutoApplyTheme()
        {
            var themeValue = SystemThemeRegistryEntry.GetValue(0) as int? ?? 0;
            var theme = GetThemeFromRegistryValue(themeValue);
            ApplyTheme(theme);
        }

        private Theme GetThemeFromRegistryValue(int registryValue)
        {
            if (_settings.ThemeOverride.ToLower() == "light")
            {
                return Theme.Light;
            }

            if (_settings.ThemeOverride.ToLower() == "dark")
            {
                return Theme.Dark;
            }

            return registryValue == 1 ? Theme.Light : Theme.Dark;
        }
    }
}
