using EverythingToolbar.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EverythingToolbar
{
	public partial class SettingsControl : Grid
	{
		private readonly ResourceLoader themes = new ResourceLoader("Themes", "Theme");
		private readonly ResourceLoader itemTemplates = new ResourceLoader("ItemTemplates", "View");

		public SettingsControl()
		{
			InitializeComponent();

			Binding themeMenuBinding = new Binding()
			{
				Converter = new ResourceLoaderToMenuItemsConverter(),
				ConverterParameter = "theme",
				Source = themes.Resources
			};
			ThemeMenu.SetBinding(ItemsControl.ItemsSourceProperty, themeMenuBinding);
			Binding itemTemplateMenuBinding = new Binding()
			{
				Converter = new ResourceLoaderToMenuItemsConverter(),
				ConverterParameter = "itemTemplate",
				Source = itemTemplates.Resources
			};
			ItemTemplateMenu.SetBinding(ItemsControl.ItemsSourceProperty, itemTemplateMenuBinding);

			(SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
		}

		private void OpenAboutWindow(object sender, RoutedEventArgs e)
		{
			Window about = new About();
			about.Show();
		}

		private void OpenRulesWindow(object sender, RoutedEventArgs e)
		{
			Window rules = new Rules();
			rules.Show();
		}

		private void OpenShortcutWindow(object sender, RoutedEventArgs e)
		{
			EverythingSearch.Instance.Reset();

			ShortcutSelector shortcutSelector = new ShortcutSelector();
			if (shortcutSelector.ShowDialog().Value)
			{
				try
				{
					//HotkeyManager.Current.AddOrReplace("FocusSearchBox",
					//	shortcutSelector.Key,
					//	shortcutSelector.Modifiers,
					//	FocusSearchBox);
					//Properties.Settings.Default.shortcutKey = (int)shortcutSelector.Key;
					//Properties.Settings.Default.shortcutModifiers = (int)shortcutSelector.Modifiers;
					//Properties.Settings.Default.Save();
				}
				catch (Exception ex)
				{
					ToolbarLogger.GetLogger("EverythingToolbar").Error(ex, "Hotkey could not be registered.");
					MessageBox.Show("Failed to register hotkey. It might be in use by another application.",
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
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

		private void MenuItem_Theme_Click(object sender, RoutedEventArgs e)
		{
			MenuItem itemChecked = (MenuItem)e.OriginalSource;
			MenuItem itemParent = (MenuItem)sender;

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

			ApplicationResources.Instance.ApplyTheme(itemChecked.Header.ToString());
		}

		private void MenuItem_ItemTemplate_Click(object sender, RoutedEventArgs e)
		{
			MenuItem itemChecked = (MenuItem)e.OriginalSource;
			MenuItem itemParent = (MenuItem)sender;

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

			ApplicationResources.Instance.ApplyItemTemplate(itemChecked.Header.ToString());
		}
	}
}
