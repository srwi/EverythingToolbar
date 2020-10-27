using CSDeskBand;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
	public partial class ToolbarControl : UserControl
	{
		private CancellationTokenSource cancellationTokenSource;
		private CancellationToken cancellationToken;
		private static Edge taskbarEdge;
		private readonly int searchBlockSize = 100;

		public ToolbarControl()
		{
			InitializeComponent();

			// Fixes #3
			if (Properties.Settings.Default.sortBy < 1)
			{
				Properties.Settings.Default.sortBy = 1;
				Properties.Settings.Default.Save();
			}

			(SortByMenu.Items[Properties.Settings.Default.sortBy - 1] as MenuItem).IsChecked = true;

			searchResultsPopup.searchResultsView.EndOfListReached += OnEndOfListReached;
			searchResultsPopup.searchResultsView.FilterChanged += OnFilterChanged;
			searchResultsPopup.Closed += SearchResultsPopup_Closed;
		}

		public static void SetTaskbarEdge(Edge edge)
        {
            taskbarEdge = edge;
        }

		private void OnFilterChanged(object sender, FilterChangedEventArgs e)
		{
			EverythingSearch.Instance.SearchMacro = e.Filter;
			StartSearch(EverythingSearch.Instance.SearchTerm);
		}

		private void OnEndOfListReached(object sender, EndOfListReachedEventArgs e)
		{
			RequestSearchResults(e.ItemCount, searchBlockSize);
			searchResultsPopup.searchResultsView.ScrollToVerticalOffset(e.VerticalOffset);
		}

		public void StartSearch(string searchTerm)
		{
			searchResultsPopup.searchResultsView.Clear();
			EverythingSearch.Instance.SearchTerm = searchTerm;
			RequestSearchResults(0, searchBlockSize);
		}

		private void RequestSearchResults(int offset, int count)
		{
			cancellationTokenSource?.Cancel();
			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;

			Task.Run(() =>
			{
				foreach(SearchResult searchResult in EverythingSearch.Instance.Query(offset, count))
				{
					searchResultsPopup.searchResultsView.AddSearchResult(searchResult);
				}
			}, cancellationToken);
		}

		private void SearchResultsPopup_Closed(object sender, EventArgs e)
		{
			if (!searchBox.IsKeyboardFocused)
				keyboardFocusCapture.Focus();

			searchBox.Clear();
		}

		private void SearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (searchResultsPopup.IsMouseOver && !searchResultsPopup.searchResultsView.SearchResultsListView.IsMouseOver)
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
				searchResultsPopup.Close();
				return;
			}

			searchResultsPopup.Open(taskbarEdge);
			StartSearch(searchBox.Text);
		}

		private void CSDeskBandWpf_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!searchResultsPopup.IsOpen)
				return;

			if (e.Key == Key.Up)
			{
				searchResultsPopup.searchResultsView.SelectPreviousSearchResult();
			}
			else if (e.Key == Key.Down)
			{
				searchResultsPopup.searchResultsView.SelectNextSearchResult();
			}
			else if (e.Key == Key.Enter)
			{
				searchResultsPopup.searchResultsView.OpenSelectedSearchResult();
			}
			else if (e.Key == Key.Escape)
			{
				searchResultsPopup.Close();
				keyboardFocusCapture.Focus();
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
	}
}
