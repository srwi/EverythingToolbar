using EverythingToolbar.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
			EverythingSearch.Instance.Reset();

			Window about = new About();
			about.Show();
		}

		private void OpenRulesWindow(object sender, RoutedEventArgs e)
		{
			EverythingSearch.Instance.Reset();

			Window rules = new Rules();
			rules.Show();
		}
		
		private void OpenShortcutWindow(object sender, RoutedEventArgs e)
		{
			EverythingSearch.Instance.Reset();

			ShortcutSelector shortcutSelector = new ShortcutSelector();
			if (shortcutSelector.ShowDialog().Value)
			{
				if (ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
					shortcutSelector.Key,
					shortcutSelector.Modifiers))
				{
					Properties.Settings.Default.shortcutKey = (int)shortcutSelector.Key;
					Properties.Settings.Default.shortcutModifiers = (int)shortcutSelector.Modifiers;
					Properties.Settings.Default.Save();
				}
				else
				{
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
			MenuItem selectedItem = (MenuItem)sender;
			MenuItem menu = (MenuItem)selectedItem.Parent;
			int selectedIndex = menu.Items.IndexOf(selectedItem);

			(menu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = false;
			(menu.Items[selectedIndex] as MenuItem).IsChecked = false;

			if (EverythingSearch.Instance.GetIsFastSort((uint)selectedIndex + 1))
			{
				Properties.Settings.Default.sortBy = selectedIndex + 1;
			}
			else
			{
				MessageBox.Show("To utilize this sorting method it has to have fast sorting enabled. It can be enabled in your Everything settings.",
								"Fast sorting not enabled",
								MessageBoxButton.OK,
								MessageBoxImage.Asterisk);
			}

			(menu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
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

		private void OnMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			e.Handled = true;

			var mouseDownEvent =
				new MouseButtonEventArgs(Mouse.PrimaryDevice,
					Environment.TickCount,
					MouseButton.Right)
				{
					RoutedEvent = Mouse.MouseUpEvent,
					Source = this,
				};

			InputManager.Current.ProcessInput(mouseDownEvent);
		}
	}
}
