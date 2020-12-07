using CSDeskBand;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
	public partial class ToolbarControl : UserControl
	{
		private static Edge taskbarEdge;

		public ToolbarControl()
		{
			InitializeComponent();
			
			try
			{
				LoadThemes();
				LoadItemTemplates();
				ApplyItemTemplate(Properties.Settings.Default.itemTemplate);
				ApplyTheme(Properties.Settings.Default.theme);
			}
			catch (Exception e)
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to load resources.");
			}

			(SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;

			SearchResultsPopup.SearchResultsView.EndOfListReached += OnEndOfListReached;

			try
			{
				HotkeyManager.Current.AddOrReplace("FocusSearchBox", Key.S, ModifierKeys.Windows | ModifierKeys.Alt, FocusSearchBox);
			}
			catch (Exception e)
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Hotkey could not be registered.");
			}
		}

		public static void SetTaskbarEdge(Edge edge)
		{
			SearchResultsPopup.taskbarEdge = edge;
		}

		private void OnEndOfListReached(object sender, EndOfListReachedEventArgs e)
		{
			EverythingSearch.Instance.QueryBatch();
			SearchResultsPopup.SearchResultsView.ScrollToVerticalOffset(e.VerticalOffset);
		}

		private void CSDeskBandWpf_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!SearchResultsPopup.IsOpen)
				return;

			if (e.Key == Key.Up)
			{
				SearchResultsPopup.SearchResultsView.SelectPreviousSearchResult();
			}
			else if (e.Key == Key.Down)
			{
				SearchResultsPopup.SearchResultsView.SelectNextSearchResult();
			}
			else if (e.Key == Key.Enter)
			{
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
				{
					string path = "";
					if (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedIndex >= 0)
						path = (SearchResultsPopup.SearchResultsView.SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
					EverythingSearch.Instance.OpenLastSearchInEverything(path);
					return;
				}
				SearchResultsPopup.SearchResultsView.OpenSelectedSearchResult();
			}
			else if (e.Key == Key.Escape)
			{
				EverythingSearch.Instance.SearchTerm = null;
				Keyboard.ClearFocus();
			}
		}

		private void OpenAboutWindow(object sender, RoutedEventArgs e)
		{
			Window about = new About();
			about.Show();
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.Save();
		}

		private void MenuItem_SortBy_Click(object sender, RoutedEventArgs e)
		{
			MenuItem itemChecked = (MenuItem)sender;
			MenuItem itemParent = (MenuItem)itemChecked.Parent;

			for (int i = 0; i < itemParent.Items.Count; i++)
			{
				if (itemParent.Items[i] == itemChecked)
				{
					(itemParent.Items[i] as MenuItem).IsChecked = true;
					Properties.Settings.Default.sortBy = i + 1;
					continue;
				}

				(itemParent.Items[i] as MenuItem).IsChecked = false;
			}

			Properties.Settings.Default.Save();
		}

		public void LoadThemes()
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string themesFolder = Path.Combine(assemblyFolder, "Themes");

			foreach (var themePath in Directory.EnumerateFiles(themesFolder, "*.xaml"))
			{
				string themeName = Path.GetFileNameWithoutExtension(themePath);
				MenuItem mi = new MenuItem() { IsCheckable = true, Header = themeName };
				if (themeName == Properties.Settings.Default.theme)
				{
					mi.IsChecked = true;
				}
				mi.Click += MenuItem_Theme_Click;
				ThemeMenu.Items.Add(mi);
			}
		}

		private void MenuItem_Theme_Click(object sender, RoutedEventArgs e)
		{
			MenuItem itemChecked = (MenuItem)sender;
			MenuItem itemParent = (MenuItem)itemChecked.Parent;

			for (int i = 0; i < itemParent.Items.Count; i++)
			{
				if (itemParent.Items[i] == itemChecked)
				{
					(itemParent.Items[i] as MenuItem).IsChecked = true;
					Properties.Settings.Default.theme = itemChecked.Header.ToString();
					continue;
				}

				(itemParent.Items[i] as MenuItem).IsChecked = false;
			}

			Resources.MergedDictionaries.Clear();
			ApplyItemTemplate(Properties.Settings.Default.itemTemplate);
			if (ApplyTheme(itemChecked.Header.ToString()))
				Properties.Settings.Default.Save();
		}

		bool ApplyTheme(string themeName)
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string themePath = Path.Combine(assemblyFolder, "Themes", themeName + ".xaml");

			if (!File.Exists(themePath))
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error("Theme file not found. Defaulting to 'Medium' theme.");
				themePath = Path.Combine(assemblyFolder, "Themes", "Medium.xaml");
			}

			try
			{
				Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(themePath) });
				return true;
			}
			catch (Exception e)
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Applying theme failed.");
				return false;
			}
		}

		public void LoadItemTemplates()
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string templateFolder = Path.Combine(assemblyFolder, "ItemTemplates");

			foreach (var templatePath in Directory.EnumerateFiles(templateFolder, "*.xaml"))
			{
				string templateName = Path.GetFileNameWithoutExtension(templatePath);
				MenuItem mi = new MenuItem() { IsCheckable = true, Header = templateName };
				if (templateName == Properties.Settings.Default.itemTemplate)
				{
					mi.IsChecked = true;
				}
				mi.Click += MenuItem_ItemTemplate_Click;
				ItemTemplateMenu.Items.Add(mi);
			}
		}

		private void MenuItem_ItemTemplate_Click(object sender, RoutedEventArgs e)
		{
			MenuItem itemChecked = (MenuItem)sender;
			MenuItem itemParent = (MenuItem)itemChecked.Parent;

			for (int i = 0; i < itemParent.Items.Count; i++)
			{
				if (itemParent.Items[i] == itemChecked)
				{
					(itemParent.Items[i] as MenuItem).IsChecked = true;
					Properties.Settings.Default.itemTemplate = itemChecked.Header.ToString();
					continue;
				}

				(itemParent.Items[i] as MenuItem).IsChecked = false;
			}

			Resources.MergedDictionaries.Clear();
			ApplyTheme(Properties.Settings.Default.theme);
			if (ApplyItemTemplate(itemChecked.Header.ToString()))
				Properties.Settings.Default.Save();
		}

		bool ApplyItemTemplate(string templateName)
		{
			string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			string templatePath = Path.Combine(assemblyFolder, "ItemTemplates", templateName + ".xaml");

			if (!File.Exists(templatePath))
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error("Item template file not found. Defaulting to 'Normal' template.");
				templatePath = Path.Combine(assemblyFolder, "ItemTemplates", "Normal.xaml");
			}

			try
			{
				Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(templatePath) });
				return true;
			}
			catch (Exception e)
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Applying item template failed.");
				return false;
			}
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

		private void FocusSearchBox(object sender, HotkeyEventArgs e)
		{
			if (Properties.Settings.Default.isIconOnly)
			{
				// TODO: set popup as foreground window
				EverythingSearch.Instance.SearchTerm = "";
			}
			else
			{
#if !DEBUG
				IntPtr taskbar = FindWindow("Shell_traywnd", "");
				SetForegroundWindow(taskbar);
#endif
				Keyboard.Focus(SearchBox);
			}
		}

		private void OpenRulesWindow(object sender, RoutedEventArgs e)
		{
			Window rules = new Rules();
			rules.Show();
		}
	}
}
