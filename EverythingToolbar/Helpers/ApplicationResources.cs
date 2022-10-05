using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace EverythingToolbar.Helpers
{
    public class ResourcesChangedEventArgs : EventArgs
    {
        public ResourceDictionary NewResource { get; set; }
    }

    class ApplicationResources
    {
        public event EventHandler<ResourcesChangedEventArgs> ResourceChanged;

        public static readonly ApplicationResources Instance = new ApplicationResources();

        private readonly RegistryEntry systemThemeRegistryEntry = new RegistryEntry("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme");
        private RegistryWatcher systemThemeWatcher = null;

        public void LoadDefaults()
        {
            if (Properties.Settings.Default.isSyncThemeEnabled)
                SyncTheme();
            else
                ApplyTheme(Properties.Settings.Default.theme);

            ApplyItemTemplate(Properties.Settings.Default.itemTemplate);
        }

        public bool AddResource(string type, string name)
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(assemblyFolder, type, name + ".xaml");

            if (!File.Exists(path))
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error("Could not find resource file " + path);
                return false;
            }

            ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs()
            {
                NewResource = new ResourceDictionary() { Source = new Uri(path) }
            });
            return true;
        }

        public void SyncTheme()
        {
            if (systemThemeWatcher != null)
            {
                systemThemeWatcher.Stop();
                systemThemeWatcher = null;
            }

            if (!Properties.Settings.Default.isSyncThemeEnabled)
            {
                Instance.ApplyTheme(Properties.Settings.Default.theme);
                return;
            }

            // Watch system theme changes
            systemThemeWatcher = new RegistryWatcher(systemThemeRegistryEntry);
            systemThemeWatcher.OnChangeValue += (newValue) =>
            {
                Instance.ApplyThemeStandard((int)newValue == 1);
            };

            // Set to current system theme
            Instance.ApplyThemeStandard((int)systemThemeRegistryEntry.GetValue() == 1);
        }

        public void ApplyTheme(string themeName)
        {
            if (!AddResource("Themes", themeName))
            {
                Properties.Settings.Default.theme = (string)Properties.Settings.Default.Properties["theme"].DefaultValue;
                Properties.Settings.Default.Save();
                AddResource("Themes", Properties.Settings.Default.theme);
            }
        }

        public void ApplyThemeStandard(bool light)
        {
            ApplyTheme(light ? "LIGHT" : "DARK");
        }

        public void ApplyItemTemplate(string templateName)
        {
            if (!AddResource("ItemTemplates", templateName))
            {
                Properties.Settings.Default.itemTemplate = (string)Properties.Settings.Default.Properties["itemTemplate"].DefaultValue;
                Properties.Settings.Default.Save();
                AddResource("ItemTemplates", Properties.Settings.Default.itemTemplate);
            }
        }
    }

    public class ResourceLoader
    {
        public string Name { get; private set; }

        public ObservableCollection<string> Resources = new ObservableCollection<string>();

        public ResourceLoader(string folder, string name)
        {
            Name = name;

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string templateFolder = Path.Combine(assemblyFolder, folder);

            foreach (var templatePath in Directory.EnumerateFiles(templateFolder, "*.xaml"))
            {
                Resources.Add(templatePath);
            }
        }
    }
}
