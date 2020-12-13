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
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			AddResource(Path.Combine(assemblyFolder, "Themes", Properties.Settings.Default.theme + ".xaml"));
			AddResource(Path.Combine(assemblyFolder, "ItemTemplates", Properties.Settings.Default.itemTemplate + ".xaml"));
		}

		public void AddResource(ResourceDictionary resource)
		{
			ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs()
			{
				NewResource = resource
			});
		}

		public void AddResource(string path)
		{
			ResourceChanged?.Invoke(this, new ResourcesChangedEventArgs()
			{
				NewResource = new ResourceDictionary() { Source = new Uri(path) }
			});
		}

		public void ApplyTheme(string themeName)
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string themePath = Path.Combine(assemblyFolder, "Themes", themeName + ".xaml");

			if (!File.Exists(themePath))
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error("Theme file not found. Defaulting to 'Medium' theme.");
				themePath = Path.Combine(assemblyFolder, "Themes", "Medium.xaml");
			}

			AddResource(themePath);
		}

		public void ApplyItemTemplate(string templateName)
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string templatePath = Path.Combine(assemblyFolder, "ItemTemplates", templateName + ".xaml");

			if (!File.Exists(templatePath))
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error("Item template file not found. Defaulting to 'Normal' template.");
				templatePath = Path.Combine(assemblyFolder, "ItemTemplates", "Normal.xaml");
			}

			AddResource(templatePath);
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
