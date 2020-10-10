using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EverythingToolbar
{
	public partial class ToolbarControl : UserControl
    {
		#region Context Menu
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
		#endregion

		public ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();
		private CancellationTokenSource updateSource;
		private CancellationToken updateToken;
		private bool preventScrollChangedEvent = false;
		private readonly int searchBlockSize = 100;
		private double scrollOffsetBeforeSearch = 0;
		private readonly Edge taskbarEdge;

		public ToolbarControl(Edge edge)
		{
			InitializeComponent();
			taskbarEdge = edge;

			// Fixes #3
			if (Properties.Settings.Default.sortBy < 1)
			{
				Properties.Settings.Default.sortBy = 1;
				Properties.Settings.Default.Save();
			}

			(SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;
		}

		private void OpenSearchResultsWindow()
		{
			switch (taskbarEdge)
			{
				case Edge.Top:
					searchResultsPopup.Placement = PlacementMode.Bottom;
					break;
				case Edge.Left:
					searchResultsPopup.Placement = PlacementMode.Right;
					break;
				case Edge.Right:
					searchResultsPopup.Placement = PlacementMode.Left;
					break;
				case Edge.Bottom:
					searchResultsPopup.Placement = PlacementMode.Top;
					break;
			}

			AdjustStyle(taskbarEdge);
			Dispatcher.BeginInvoke((Action)(() => TabControl.SelectedIndex = 0));
			searchResultsPopup.IsOpen = true;
			searchResultsPopup.StaysOpen = true;
		}

		private void CloseSearchResultsWindow()
		{
			searchResultsPopup.IsOpen = false;
			searchResults.Clear();
		}

		public void StartSearch(string searchTerm)
		{
			scrollOffsetBeforeSearch = 0;
			searchResults.Clear();
			EverythingSearch.Instance.SearchTerm = searchTerm;
			RequestSearchResults(0, searchBlockSize);
		}

		private void RequestSearchResults(int offset, int count)
		{
			preventScrollChangedEvent = true;

			updateSource?.Cancel();
			updateSource = new CancellationTokenSource();
			updateToken = updateSource.Token;

			Task.Run(() =>
			{
				foreach(SearchResult result in EverythingSearch.Instance.Query(offset, count))
				{
					Dispatcher.Invoke(() =>
					{
						searchResults.Add(result);
					});
				}

				Dispatcher.Invoke(new Action(() =>
				{
					Decorator listViewBorder = VisualTreeHelper.GetChild(SearchResultsListView, 0) as Decorator;
					ScrollViewer listViewScrollViewer = listViewBorder.Child as ScrollViewer;
					listViewScrollViewer.ScrollToVerticalOffset(scrollOffsetBeforeSearch);
					preventScrollChangedEvent = false;
				}), DispatcherPriority.ContextIdle);
			}, updateToken);
		}

		public void SelectNextSearchResult()
		{
			if (SearchResultsListView.SelectedIndex + 1 < SearchResultsListView.Items.Count)
			{
				SearchResultsListView.SelectedIndex++;
				SearchResultsListView.ScrollIntoView(SearchResultsListView.SelectedItem);
			}
		}

		public void SelectPreviousSearchResult()
		{
			if (SearchResultsListView.SelectedIndex > 0)
			{
				SearchResultsListView.SelectedIndex--;
				SearchResultsListView.ScrollIntoView(SearchResultsListView.SelectedItem);
			}
		}

		public void OpenSelectedSearchResult()
		{
			keyboardFocusCapture.Focus();
			if (SearchResultsListView.SelectedIndex != -1)
			{
				try
				{
					string path = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
					Process.Start(path);
					EverythingSearch.Instance.IncrementRunCount(path);
				}
				catch (Win32Exception)
				{
					MessageBox.Show("Failed to open this file/folder.");
				}
			}
		}

		public void AdjustStyle(Edge edge)
		{
			switch (edge)
			{
				case Edge.Top:
					searchResultsPopupBorder.BorderThickness = new Thickness(1, 0, 1, 1);
					break;
				case Edge.Left:
					searchResultsPopupBorder.BorderThickness = new Thickness(0, 1, 1, 1);
					break;
				case Edge.Right:
					searchResultsPopupBorder.BorderThickness = new Thickness(1, 1, 0, 1);
					break;
				case Edge.Bottom:
					searchResultsPopupBorder.BorderThickness = new Thickness(1, 1, 1, 0);
					break;
			}

			SearchResultsListView.Height = searchResultsPopup.Height - TabControl.Height;
		}

		private void SearchResultsPopup_Closed(object sender, EventArgs e)
		{
			if (!searchBox.IsKeyboardFocused)
				keyboardFocusCapture.Focus();

			searchBox.Clear();
		}

		private void SearchResultsListViewItem_PreviewMouseUp(object sender, MouseEventArgs e)
		{
			OpenSelectedSearchResult();
		}

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!searchResultsPopup.IsMouseOver)
			{
				return;
			}

			if (AllTab.IsSelected)
			{
				EverythingSearch.Instance.SearchMacro = "";
			}
			else if (FilesTab.IsSelected)
			{
				EverythingSearch.Instance.SearchMacro = "file:";
			}
			else if (FoldersTab.IsSelected)
			{
				EverythingSearch.Instance.SearchMacro = "folder:";
			}

			StartSearch(EverythingSearch.Instance.SearchTerm);
		}

		private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (preventScrollChangedEvent)
			{
				e.Handled = true;
				return;
			}

			if (e.VerticalChange > 0)
			{
				if (e.VerticalOffset > e.ExtentHeight - 2 * e.ViewportHeight)
				{
					scrollOffsetBeforeSearch = e.VerticalOffset;
					RequestSearchResults(searchResults.Count, searchBlockSize);
				}
			}
		}

		private void SearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (searchResultsPopup.IsMouseOver && !SearchResultsListView.IsMouseOver)
			{
				searchBox.Focus();
			}
			else
			{
				searchResultsPopup.StaysOpen = false;
			}
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (searchBox.Text.Length == 0)
			{
				CloseSearchResultsWindow();
				return;
			}

			if (SearchResultsListView.ItemsSource == null)
				SearchResultsListView.ItemsSource = searchResults;

			OpenSearchResultsWindow();
			StartSearch(searchBox.Text);
		}

		private void CSDeskBandWpf_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!searchResultsPopup.IsOpen)
				return;

			if (e.Key == Key.Up)
			{
				SelectPreviousSearchResult();
			}
			else if (e.Key == Key.Down)
			{
				SelectNextSearchResult();
			}
			else if (e.Key == Key.Enter)
			{
				OpenSelectedSearchResult();
			}
			else if (e.Key == Key.Escape)
			{
				CloseSearchResultsWindow();
				Keyboard.ClearFocus();
			}
		}

		private void OpenAboutWindow(object sender, RoutedEventArgs e)
		{
			Window about = new About();
			about.Show();
		}
	}
}
