using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using Microsoft.Xaml.Behaviors;
using NLog;
using System.IO;
using System.Reflection;
using System.Threading;
using System;
using System.Windows;
using Windows.UI.ViewManagement;
using System.Windows.Media;

namespace EverythingToolbar.Behaviors
{
    public class ThemeAwareness : Behavior<FrameworkElement>
    {
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
            ThemeHandler.Instance.ResourceChanged += (s, e) => { AssociatedObject.Resources = e.NewResource; };
            ThemeHandler.Instance.AutoApplyTheme();
        }
    }

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

    public class ThemeHandler
    {
        public static readonly ThemeHandler Instance = new ThemeHandler();
        public event EventHandler<ResourcesChangedEventArgs> ResourceChanged;

        private static ResourceDictionary _currentResources;
        private readonly SynchronizationContext _uiThreadContext;
        private readonly RegistryEntry _systemThemeRegistryEntry = new RegistryEntry("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        private readonly RegistryWatcher _systemThemeWatcher = null;
        private static readonly ILogger _logger = ToolbarLogger.GetLogger<ThemeHandler>();
        private readonly UISettings _settings = new UISettings();

        private ThemeHandler()
        {
            _currentResources = new ResourceDictionary();

            if (_systemThemeWatcher != null)
            {
                _systemThemeWatcher.Stop();
                _systemThemeWatcher = null;
            }

            // Store UI thread SynchronizationContext so that the RegistryWatcher can later use it to access the UI thread
            _uiThreadContext = SynchronizationContext.Current;
            _systemThemeWatcher = new RegistryWatcher(_systemThemeRegistryEntry);
            _systemThemeWatcher.OnChangeValue += (newValue) =>
            {
                _uiThreadContext.Post(state => {
                    ApplyTheme((int)newValue == 1);
                }, null);
            };

            _settings.ColorValuesChanged += (UISettings sender, object args) =>
            {
                _uiThreadContext.Post(state => {
                    AutoApplyTheme();
                }, null);
            };

            Settings.Default.PropertyChanged += OnSettingsChanged;
        }

        private static SolidColorBrush GetBrush(Windows.UI.Color color)
        {
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        public void AutoApplyTheme()
        {
            bool isLightTheme = (int)_systemThemeRegistryEntry.GetValue() == 1;
            ApplyTheme(isLightTheme);
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "itemTemplate")
            {
                AutoApplyTheme();
            }
        }

        private void ApplyTheme(bool isLightTheme)
        {
            _currentResources.Clear();

            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string themeLocation = assemblyLocation;
            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                themeLocation = Path.Combine(themeLocation, "Themes", "Win11");
            else
                themeLocation = Path.Combine(themeLocation, "Themes", "Win10");

            // Apply all control styles contained in "Controls" subdirectory
            DirectoryInfo controlsLocation = new DirectoryInfo(Path.Combine(themeLocation, "Controls"));
            foreach (var file in controlsLocation.GetFiles("*.xaml"))
                AddResource(file.FullName);

            // Apply color scheme according to Windows theme
            string themeFileName = isLightTheme ? "Light.xaml" : "Dark.xaml";
            AddResource(Path.Combine(themeLocation, themeFileName));

            // Apply ItemTemplate style
            string dataTemplateLocation = Path.Combine(assemblyLocation, "ItemTemplates", Properties.Settings.Default.itemTemplate + ".xaml");
            AddResource(dataTemplateLocation);

            // Apply accent color
            SolidColorBrush accentColor;
            if (isLightTheme)
                accentColor = GetBrush(_settings.GetColorValue(UIColorType.AccentDark1));
            else
                accentColor = GetBrush(_settings.GetColorValue(UIColorType.AccentLight2));
            AddSolidColorBrush(accentColor);

            // Notify resource change
            ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs()
            {
                NewResource = _currentResources,
                NewTheme = isLightTheme ? Theme.Light : Theme.Dark
            });
        }

        private void AddResource(string path)
        {
            if (!File.Exists(path))
            {
                _logger.Error("Could not find resource file " + path);
                return;
            }

            var resDict = new ResourceDictionary() { Source = new Uri(path) };
            _currentResources.MergedDictionaries.Add(resDict);
        }

        private void AddSolidColorBrush(SolidColorBrush brush)
        {
            var resDict = new ResourceDictionary();
            resDict.Add("AccentColor", brush);
            _currentResources.MergedDictionaries.Add(resDict);
        }
    }
}
