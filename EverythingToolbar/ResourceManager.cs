using EverythingToolbar.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace EverythingToolbar
{
    public class ResourcesChangedEventArgs : EventArgs
    {
        public ResourceDictionary NewResource { get; set; }
    }

    public class ResourceManager
    {
        public static readonly ResourceManager Instance = new ResourceManager();
        public event EventHandler<ResourcesChangedEventArgs> ResourceChanged;
        
        private static ResourceDictionary CurrentResources;
        private readonly RegistryEntry systemThemeRegistryEntry = new RegistryEntry("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        private readonly RegistryWatcher systemThemeWatcher = null;

        private ResourceManager()
        {
            CurrentResources = new ResourceDictionary();

            if (systemThemeWatcher != null)
            {
                systemThemeWatcher.Stop();
                systemThemeWatcher = null;
            }

            systemThemeWatcher = new RegistryWatcher(systemThemeRegistryEntry);
            systemThemeWatcher.OnChangeValue += (newValue) =>
            {
                ApplyTheme((int)newValue == 1);
            };

            Properties.Settings.Default.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "itemTemplate")
            {
                AutoApplyTheme();
            }
        }

        public void AutoApplyTheme()
        {
            bool isLightTheme = (int)systemThemeRegistryEntry.GetValue() == 1;
            ApplyTheme(isLightTheme);
        }

        private void ApplyTheme(bool isLightTheme)
        {
            CurrentResources.Clear();

            string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string themeLocation = assemblyLocation;
            if (Utils.IsWindows11)
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

            // Notify resource change
            ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs()
            {
                NewResource = CurrentResources
            });
        }

        private void AddResource(string path)
        {
            if (!File.Exists(path))
                ToolbarLogger.GetLogger("EverythingToolbar").Error("Could not find resource file " + path);

            var resDict = new ResourceDictionary() { Source = new Uri(path) };
            CurrentResources.MergedDictionaries.Add(resDict);
        }
    }
}
