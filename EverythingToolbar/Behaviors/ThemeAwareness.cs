using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NLog;
using Windows.UI.ViewManagement;
using Color = Windows.UI.Color;

namespace EverythingToolbar.Behaviors
{
    public enum Theme
    {
        Dark,
        Light,
    }

    public class ResourcesChangedEventArgs : EventArgs
    {
        public Theme NewTheme { get; set; }
    }

    public class ThemeAwareness : Behavior<FrameworkElement>
    {
        public static event EventHandler<ResourcesChangedEventArgs>? ResourceChanged;

        private readonly List<ResourceDictionary> _addedDictionaries = new();
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
        private UISettings? _uiSettings;
        private readonly RegistryValueWatcher _systemThemeWatcher;
        private static readonly RegistryEntry SystemThemeRegistryEntry = new(
            "HKEY_CURRENT_USER",
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "SystemUsesLightTheme"
        );
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ThemeAwareness>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.IsLoaded)
            {
                AutoApplyTheme();
            }
            else
            {
                AssociatedObject.Loaded += (_, _) =>
                {
                    AutoApplyTheme();
                };
            }
        }

        protected override void OnDetaching()
        {
            _systemThemeWatcher.Dispose();
            _settings.PropertyChanged -= OnSettingsChanged;
            base.OnDetaching();
        }

        public ThemeAwareness()
        {
            _systemThemeWatcher = new RegistryValueWatcher(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
            );
            _systemThemeWatcher.Changed += () => Dispatcher.Invoke(AutoApplyTheme);

            try
            {
                _uiSettings = new UISettings();
                _uiSettings.ColorValuesChanged += (sender, args) =>
                {
                    Dispatcher.Invoke(AutoApplyTheme);
                };
            }
            catch
            {
                Logger.Info("Could not apply accent color automatically.");
            }

            _settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (
                e.PropertyName
                is nameof(ISettings.ItemTemplate)
                    or nameof(ISettings.ThemeOverride)
                    or nameof(ISettings.ForceWin10Behavior)
            )
            {
                AutoApplyTheme();
            }
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

        private void AutoApplyTheme()
        {
            var themeValue = (int)(SystemThemeRegistryEntry.GetValue(0) ?? 0);
            var theme = GetThemeFromRegistryValue(themeValue);
            ApplyTheme(theme);
        }

        private void ApplyTheme(Theme theme)
        {
            foreach (var dict in _addedDictionaries)
            {
                AssociatedObject.Resources.MergedDictionaries.Remove(dict);
            }
            _addedDictionaries.Clear();

            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (assemblyLocation == null)
            {
                Logger.Error("Could not determine assembly location.");
                return;
            }

            var themeLocation = assemblyLocation;
            if (_windowsPolicy.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                themeLocation = Path.Combine(themeLocation, "Themes", "Win11");
            else
                themeLocation = Path.Combine(themeLocation, "Themes", "Win10");

            // Apply all control styles contained in "Controls" subdirectory
            var controlsLocation = new DirectoryInfo(Path.Combine(themeLocation, "Controls"));
            foreach (var file in controlsLocation.GetFiles("*.xaml"))
                AddResource(file.FullName);

            // Apply color scheme according to Windows theme
            var themeFileName = theme == Theme.Light ? "Light.xaml" : "Dark.xaml";
            AddResource(Path.Combine(themeLocation, themeFileName));

            // Apply ItemTemplate style
            var dataTemplateLocation = Path.Combine(
                assemblyLocation,
                "ItemTemplates",
                _settings.ItemTemplate + ".xaml"
            );
            AddResource(
                dataTemplateLocation,
                fallbackPath: Path.Combine(assemblyLocation, "ItemTemplates", "Normal.xaml")
            );

            // Apply accent color
            if (_uiSettings != null)
            {
                if (theme == Theme.Light)
                    SetAccentColor(GetBrush(_uiSettings.GetColorValue(UIColorType.AccentDark1)));
                else
                    SetAccentColor(GetBrush(_uiSettings.GetColorValue(UIColorType.AccentLight2)));
            }
            else
            {
                SetAccentColor(new SolidColorBrush(Colors.DimGray));
            }

            // Notify resource change
            ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs { NewTheme = theme });
        }

        private void AddResource(string path, string? fallbackPath = null)
        {
            if (!File.Exists(path))
            {
                Logger.Error("Could not find resource file " + path);

                if (fallbackPath != null)
                    AddResource(fallbackPath);

                return;
            }

            var resDict = new ResourceDictionary { Source = new Uri(path) };
            AssociatedObject.Resources.MergedDictionaries.Add(resDict);
            _addedDictionaries.Add(resDict);
        }

        private void SetAccentColor(SolidColorBrush brush)
        {
            var resDict = new ResourceDictionary();
            resDict.Add("AccentColor", brush);
            AssociatedObject.Resources.MergedDictionaries.Add(resDict);
            _addedDictionaries.Add(resDict);
        }

        private static SolidColorBrush GetBrush(Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
