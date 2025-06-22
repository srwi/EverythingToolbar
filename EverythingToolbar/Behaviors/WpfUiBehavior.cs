using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Windows;
using Wpf.Ui.Appearance;

namespace EverythingToolbar.Behaviors
{
    public class WpfUiBehavior : Behavior<FrameworkElement>
    {
        private readonly List<ResourceDictionary> _addedDictionaries = new();
        private static readonly RegistryEntry SystemThemeRegistryEntry = new("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        
        public WpfUiBehavior()
        {
            if (AssociatedObject is not Window)
                return;

            // TODO: Theme switching seems to be broken in WPF UI: https://github.com/lepoco/wpfui/issues/1193
            var systemThemeWatcher = new RegistryWatcher(SystemThemeRegistryEntry);
            systemThemeWatcher.OnChangeValue += newValue =>
            {
                Dispatcher.Invoke(() =>
                {
                    var theme = GetThemeFromRegistryValue((int)newValue);
                    ApplyTheme(theme);
                });
            };
        }
        
        protected override void OnAttached()
        {
            base.OnAttached();
            
            if (AssociatedObject.IsLoaded)
                AutoApplyTheme();
            else
                AssociatedObject.Loaded += (_, _) => { AutoApplyTheme(); };
        }

        private void ApplyTheme(Theme theme)
        {
            // TODO: UiApplication benutzen sorgt dafür dass deskband nicht crasht, aber lädt themes ncht korrekt
            // hier lesen vielleicht? https://github.com/lepoco/wpfui/issues/837#issuecomment-1828308304
            
            // foreach (var dict in _addedDictionaries)
            // {
            //     UiApplication.Current.Resources.MergedDictionaries.Remove(dict);
            // }
            //
            // _addedDictionaries.Clear();
            // _addedDictionaries.Add(new ThemesDictionary { Theme = theme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark });
            // _addedDictionaries.Add(new ControlsDictionary());
            //
            // foreach (var dict in _addedDictionaries)
            // {
            //     UiApplication.Current.Resources.MergedDictionaries.Add(dict);
            // }
            
            ApplicationThemeManager.Apply(theme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark);
            ApplicationThemeManager.Apply(AssociatedObject);
            
        }

        private void AutoApplyTheme()
        {
            var themeValue = (int)SystemThemeRegistryEntry.GetValue(0);
            var theme = GetThemeFromRegistryValue(themeValue);
            ApplyTheme(theme);
        }

        private Theme GetThemeFromRegistryValue(int registryValue)
        {
            if (ToolbarSettings.User.ThemeOverride.ToLower() == "light")
            {
                return Theme.Light;
            }

            if (ToolbarSettings.User.ThemeOverride.ToLower() == "dark")
            {
                return Theme.Dark;
            }

            return registryValue == 1 ? Theme.Light : Theme.Dark;
        }
    }
}
