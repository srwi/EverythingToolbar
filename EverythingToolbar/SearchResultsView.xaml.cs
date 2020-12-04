using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EverythingToolbar
{

	public class EndOfListReachedEventArgs : EventArgs
	{
		public double VerticalOffset { get; set; }
	}

	public partial class SearchResultsView : UserControl
	{
		public event EventHandler<EndOfListReachedEventArgs> EndOfListReached;

		private SearchResult SelectedItem => SearchResultsListView.SelectedItem as SearchResult;

		public SearchResultsView()
		{
			InitializeComponent();

			SearchResultsListView.ItemsSource = EverythingSearch.Instance.SearchResults;
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

		private void ListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange > 0)
			{
				if (e.VerticalOffset > e.ExtentHeight - 2 * e.ViewportHeight)
				{
					EndOfListReached?.Invoke(this, new EndOfListReachedEventArgs() {
						VerticalOffset = e.VerticalOffset
					});
				}
			}
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
				SearchResultsListView.ScrollIntoView(SelectedItem);
			}
		}

		public void OpenSelectedSearchResult()
		{
			if (SearchResultsListView.SelectedIndex != -1)
			{
				if (Rules.HandleRule(SelectedItem))
					return;

				SelectedItem?.Open();
			}
		}

		private void OpenFilePath(object sender, RoutedEventArgs e)
		{
			SelectedItem?.OpenPath();
		}

		private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
		{
			SelectedItem?.CopyPathToClipboard();
			EverythingSearch.Instance.Reset();
		}

		private void OpenWith(object sender, RoutedEventArgs e)
		{
			SelectedItem?.OpenWith();
		}

		private void ShowInEverything(object sender, RoutedEventArgs e)
		{
			SelectedItem?.ShowInEverything();
		}

		private void CopyFile(object sender, RoutedEventArgs e)
		{
			SelectedItem?.CopyToClipboard();
			EverythingSearch.Instance.Reset();
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			OpenSelectedSearchResult();
		}

		private void Open(object sender, MouseEventArgs e)
		{
			OpenSelectedSearchResult();
		}

		public void ShowFileProperties(object sender, RoutedEventArgs e)
		{
			SelectedItem?.ShowProperties();
		}

		private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			while (mi.Items.Count > 3)
				mi.Items.RemoveAt(0);

			List<Rule> rules = Rules.LoadRules();

			if (rules.Count > 0)
				(mi.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
			else
				(mi.Items[0] as MenuItem).Visibility = Visibility.Visible;

			for (int i = rules.Count - 1; i >= 0; i--)
			{
				Rule rule = rules[i];
				MenuItem ruleMenuItem = new MenuItem() { Header = rule.Name, Tag = rule.Command };
				ruleMenuItem.Click += OpenWithRule;
				mi.Items.Insert(0, ruleMenuItem);
			}
		}

		private void OpenWithRule(object sender, RoutedEventArgs e)
		{
			SearchResult searchResult = SearchResultsListView.SelectedItem as SearchResult;
			string command = (sender as MenuItem).Tag?.ToString() ?? "";
			Rules.HandleRule(searchResult, command);
		}
	}
}
