using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EverythingToolbar
{
	public class FilterChangedEventArgs : EventArgs
	{
		public string Filter { get; set; }
	}

	public class EndOfListReachedEventArgs : EventArgs
	{
		public int ItemCount { get; set; }
		public double VerticalOffset { get; set; }
	}

	public partial class SearchResultsView : UserControl
	{
		public event EventHandler<EndOfListReachedEventArgs> EndOfListReached;
		public event EventHandler<FilterChangedEventArgs> FilterChanged;

		private ObservableCollection<SearchResult> searchResults = new ObservableCollection<SearchResult>();

		public SearchResultsView()
		{
			InitializeComponent();

			SearchResultsListView.ItemsSource = searchResults;
		}

		public void Clear()
		{
			searchResults.Clear();
		}

		public void AddSearchResult(SearchResult searchResult)
		{
			Dispatcher.Invoke(() =>
			{
				searchResults.Add(searchResult);
			});
		}

		public void ScrollToVerticalOffset(double verticalOffset)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				Decorator listViewBorder = VisualTreeHelper.GetChild(SearchResultsListView, 0) as Decorator;
				ScrollViewer listViewScrollViewer = listViewBorder.Child as ScrollViewer;
				listViewScrollViewer.ScrollToVerticalOffset(verticalOffset);
			}), DispatcherPriority.ContextIdle);
		}

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (AllTab.IsSelected)
			{
				FilterChanged?.Invoke(this, new FilterChangedEventArgs() { Filter = "" });
			}
			else if (FilesTab.IsSelected)
			{
				FilterChanged?.Invoke(this, new FilterChangedEventArgs() { Filter = "file:" });
			}
			else if (FoldersTab.IsSelected)
			{
				FilterChanged?.Invoke(this, new FilterChangedEventArgs() { Filter = "folder:" });
			}
		}

		private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange > 0)
			{
				if (e.VerticalOffset > e.ExtentHeight - 2 * e.ViewportHeight)
				{
					EndOfListReached?.Invoke(this, new EndOfListReachedEventArgs() { ItemCount = searchResults.Count, VerticalOffset = e.VerticalOffset });
				}
			}
		}

		public void OpenSelectedSearchResult(string path = "")
		{
			if (SearchResultsListView.SelectedIndex != -1)
			{
				try
				{
					path = path == "" ? (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName : path;
					Process.Start(path);
					EverythingSearch.Instance.IncrementRunCount(path);
				}
				catch (Win32Exception)
				{
					MessageBox.Show("Failed to open this file/folder.");
				}
			}
		}

		private void SearchResultsListViewItem_PreviewMouseUp(object sender, MouseEventArgs e)
		{
			OpenSelectedSearchResult();
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

		private void OpenFilePath(object sender, RoutedEventArgs e)
		{
			OpenSelectedSearchResult(Path.GetDirectoryName((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName));
		}

		private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName);
		}

		private void OpenWith(object sender, RoutedEventArgs e)
		{
			string path = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
			var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
			args += ",OpenAs_RunDLL " + path;
			Process.Start("rundll32.exe", args);
		}
	}
}
