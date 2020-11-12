using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
		public event EventHandler<EventArgs> PopupCloseRequested;

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
			if (SearchResultsListView.SelectedIndex != -1)
			{
				if (Rules.HandleRule(SearchResultsListView.SelectedItem as SearchResult))
					return;

				try
				{
					string path = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
					Process.Start(path);
					EverythingSearch.Instance.IncrementRunCount(path);
				}
				catch (Win32Exception)
				{
					MessageBox.Show("Failed to open file/folder.");
				}
			}
		}

		private void OpenFilePath(object sender, RoutedEventArgs e)
		{
			if (SearchResultsListView.SelectedIndex != -1)
			{
				try
				{
					string path = Path.GetDirectoryName((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName);
					Process.Start(path);
					EverythingSearch.Instance.IncrementRunCount(path);
				}
				catch (Win32Exception)
				{
					MessageBox.Show("Failed to open folder.");
				}
			}
		}

		private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName);
			PopupCloseRequested?.Invoke(this, new EventArgs());
		}

		private void OpenWith(object sender, RoutedEventArgs e)
		{
			string path = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
			var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
			args += ",OpenAs_RunDLL " + path;
			Process.Start("rundll32.exe", args);
		}

		private void ShowInEverything(object sender, RoutedEventArgs e)
		{
			EverythingSearch.Instance.OpenLastSearchInEverything((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName);
		}

		private void CopyFile(object sender, RoutedEventArgs e)
		{
			StringCollection file = new StringCollection();
			file.Add((SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName);
			Clipboard.SetFileDropList(file);
			PopupCloseRequested?.Invoke(this, new EventArgs());
		}

		private void Open(object sender, RoutedEventArgs e)
		{
			OpenSelectedSearchResult();
		}

		private void Open(object sender, MouseEventArgs e)
		{
			OpenSelectedSearchResult();
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct SHELLEXECUTEINFO
		{
			public int cbSize;
			public uint fMask;
			public IntPtr hwnd;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpVerb;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpFile;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpParameters;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpDirectory;
			public int nShow;
			public IntPtr hInstApp;
			public IntPtr lpIDList;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string lpClass;
			public IntPtr hkeyClass;
			public uint dwHotKey;
			public IntPtr hIcon;
			public IntPtr hProcess;
		}

		public void ShowFileProperties(object sender, RoutedEventArgs e)
		{
			SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
			info.cbSize = Marshal.SizeOf(info);
			info.lpVerb = "properties";
			info.lpFile = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
			info.nShow = 5;
			info.fMask = 12;
			ShellExecuteEx(ref info);
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

			foreach (Rule rule in rules)
			{
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
