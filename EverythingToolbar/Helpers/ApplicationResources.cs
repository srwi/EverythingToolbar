using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;

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

        public void LoadDefaults()
        {
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

        public void ApplyTheme(string themeName)
        {
            if (!AddResource("Themes", themeName))
            {
                Properties.Settings.Default.theme = (string)Properties.Settings.Default.Properties["theme"].DefaultValue;
                Properties.Settings.Default.Save();
                AddResource("Themes", Properties.Settings.Default.theme);
            }
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
