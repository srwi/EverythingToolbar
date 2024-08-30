using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Windows.UI.ViewManagement;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using Microsoft.Xaml.Behaviors;
using NLog;
using Color = Windows.UI.Color;

namespace EverythingToolbar.Behaviors
{
    public enum Theme
    {
        Dark,
        Light
    }

    public class ResourcesChangedEventArgs : EventArgs
    {
        public ResourceDictionary NewResource { get; set; }
        public Theme NewTheme { get; set; }
    }

    public class ThemeAwareness : Behavior<FrameworkElement>
    {
        public static event EventHandler<ResourcesChangedEventArgs> ResourceChanged;

        private static ResourceDictionary _currentResources;
        private static UISettings _settings;
        private static readonly RegistryEntry SystemThemeRegistryEntry = new RegistryEntry("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ThemeAwareness>();

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.IsLoaded)
            {
                AssociatedObjectOnLoaded(null, null);
            }
            else
            {
                AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs _)
        {
            ResourceChanged += (s, e) => { AssociatedObject.Resources = e.NewResource; };
            AutoApplyTheme();
        }

        public ThemeAwareness()
        {
            _currentResources = new ResourceDictionary();

            var systemThemeWatcher = new RegistryWatcher(SystemThemeRegistryEntry);
            systemThemeWatcher.OnChangeValue += newValue =>
            {
                Dispatcher.Invoke(() => {
                    ApplyTheme((int)newValue == 1);
                });
            };

            try
            {
                _settings = new UISettings();
                _settings.ColorValuesChanged += (sender, args) =>
                {
                    Dispatcher.Invoke(AutoApplyTheme);
                };
            }
            catch
            {
                Logger.Info("Could not apply accent color automatically.");
            }

            Settings.Default.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "itemTemplate")
            {
                AutoApplyTheme();
            }
        }

        private void AutoApplyTheme()
        {
            var isLightTheme = (int)SystemThemeRegistryEntry.GetValue(0) == 1;
            ApplyTheme(isLightTheme);
        }

        private void ApplyTheme(bool isLightTheme)
        {
            _currentResources.Clear();

            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var themeLocation = assemblyLocation;
            if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                themeLocation = Path.Combine(themeLocation, "Themes", "Win11");
            else
                themeLocation = Path.Combine(themeLocation, "Themes", "Win10");

            // Apply all control styles contained in "Controls" subdirectory
            var controlsLocation = new DirectoryInfo(Path.Combine(themeLocation, "Controls"));
            foreach (var file in controlsLocation.GetFiles("*.xaml"))
                AddResource(file.FullName);

            // Apply color scheme according to Windows theme
            var themeFileName = isLightTheme ? "Light.xaml" : "Dark.xaml";
            AddResource(Path.Combine(themeLocation, themeFileName));

            // Apply ItemTemplate style
            var dataTemplateLocation = Path.Combine(assemblyLocation, "ItemTemplates", Settings.Default.itemTemplate + ".xaml");
            AddResource(dataTemplateLocation, fallbackPath: Path.Combine(assemblyLocation, "ItemTemplates", "Normal.xaml"));

            // Apply accent color
            if (_settings != null)
            {
                if (isLightTheme)
                    SetAccentColor(GetBrush(_settings.GetColorValue(UIColorType.AccentDark1)));
                else
                    SetAccentColor(GetBrush(_settings.GetColorValue(UIColorType.AccentLight2)));
            }
            else
            {
                SetAccentColor(new SolidColorBrush(Colors.DimGray));
            }

            // Notify resource change
            ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs
            {
                NewResource = _currentResources,
                NewTheme = isLightTheme ? Theme.Light : Theme.Dark
            });
        }

        private static void AddResource(string path, string fallbackPath = null)
        {
            if (!File.Exists(path))
            {
                Logger.Error("Could not find resource file " + path);

                if (fallbackPath != null)
                    AddResource(fallbackPath);

                return;
            }

            var resDict = new ResourceDictionary { Source = new Uri(path) };
            _currentResources.MergedDictionaries.Add(resDict);
        }

        private void SetAccentColor(SolidColorBrush brush)
        {
            var resDict = new ResourceDictionary();
            resDict.Add("AccentColor", brush);
            _currentResources.MergedDictionaries.Add(resDict);
        }

        private static SolidColorBrush GetBrush(Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
