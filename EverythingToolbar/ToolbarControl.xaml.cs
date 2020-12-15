using CSDeskBand;
using EverythingToolbar.Helpers;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace EverythingToolbar
{
	public partial class ToolbarControl : UserControl
	{
		private static Edge taskbarEdge;

		public ToolbarControl()
		{
			InitializeComponent();

			ApplicationResources.Instance.ResourceChanged += (object sender, ResourcesChangedEventArgs e) =>
			{
				try
				{
					Resources.MergedDictionaries.Add(e.NewResource);
					Properties.Settings.Default.Save();
				}
				catch (Exception ex)
				{
					ToolbarLogger.GetLogger("EverythingToolbar").Error(ex, "Failed to apply resources.");
				}
			};
			ApplicationResources.Instance.LoadDefaults();

			SearchResultsPopup.Closed += (object sender, EventArgs e) =>
			{
				Keyboard.Focus(KeyboardFocusCapture);
			};

			ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
				(Key)Properties.Settings.Default.shortcutKey,
				(ModifierKeys)Properties.Settings.Default.shortcutModifiers,
				FocusSearchBox);
		}

		public static void SetTaskbarEdge(Edge edge)
		{
			SearchResultsPopup.taskbarEdge = edge;
		}

		private void OnKeyPressed(object sender, KeyEventArgs e)
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

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private void FocusSearchBox(object sender, HotkeyEventArgs e)
		{
			SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
			Keyboard.Focus(SearchBox);

			if (Properties.Settings.Default.isIconOnly)
				EverythingSearch.Instance.SearchTerm = "";
		}
	}
}
