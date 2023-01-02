using EverythingToolbar.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EverythingToolbar
{
    public partial class SearchResultsView
    {
        private SearchResult SelectedItem => SearchResultsListView.SelectedItem as SearchResult;
        private Point dragStart;

        public SearchResultsView()
        {
            InitializeComponent();

            SearchResultsListView.ItemsSource = EverythingSearch.Instance.SearchResults;
            ((INotifyCollectionChanged)SearchResultsListView.Items).CollectionChanged += OnCollectionChanged;
            EventDispatcher.Instance.KeyPressed += OnKeyPressed;

            // Mouse events and context menu must be added to the ItemContainerStyle each time it gets updated
            Loaded += (s, e) =>
            {
                RegisterItemContainerStyleProperties(s, null);
                ResourceManager.Instance.ResourceChanged += RegisterItemContainerStyleProperties;
            };
        }

        private void RegisterItemContainerStyleProperties(object sender, ResourcesChangedEventArgs e)
        {
            SearchResultsListView.ItemContainerStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseLeftButtonUpEvent,
                Handler = new MouseButtonEventHandler(Open)
            });
            SearchResultsListView.ItemContainerStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseDownEvent,
                Handler = new MouseButtonEventHandler(OnListViewItemMouseDown)
            });
            SearchResultsListView.ItemContainerStyle.Setters.Add(new EventSetter
            {
                Event = MouseMoveEvent,
                Handler = new MouseEventHandler(OnListViewItemMouseMove)
            });
            SearchResultsListView.ItemContainerStyle.Setters.Add(new Setter()
            {
                Property = ContextMenuProperty,
                Value = new Binding() { Source = Resources["ListViewItemContextMenu"] }
            });
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                //Keyboard.Focus(SearchBox);
                EverythingSearch.Instance.SearchTerm = HistoryManager.Instance.GetPreviousItem();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                //Keyboard.Focus(SearchBox);
                EverythingSearch.Instance.SearchTerm = HistoryManager.Instance.GetNextItem();
            }
            else if (e.Key == Key.Up)
            {
                SelectPreviousSearchResult();
            }
            else if (e.Key == Key.Down)
            {
                SelectNextSearchResult();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                string path = "";
                if (SearchResultsListView.SelectedIndex >= 0)
                    path = (SearchResultsListView.SelectedItem as SearchResult).FullPathAndFileName;
                EverythingSearch.Instance.OpenLastSearchInEverything(path);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                OpenFilePath(null, null);
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Enter)
            {
                RunAsAdmin(null, null);
            }
            else if (e.Key == Key.Enter)
            {
                OpenSelectedSearchResult();
            }
            else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                PreviewSelectedFile();
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                int index = e.Key == Key.D0 ? 9 : e.Key - Key.D1;
                EverythingSearch.Instance.SelectFilterFromIndex(index);
            }
            else if (e.Key == Key.Escape)
            {
                EventDispatcher.Instance.InvokeHideWindow();
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.PageUp)
            {
                PageUp();
            }
            else if (e.Key == Key.PageDown)
            {
                PageDown();
            }
            else if (e.Key == Key.Home)
            {
                ScrollToHome();
            }
            else if (e.Key == Key.End)
            {
                ScrollToEnd();
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (SearchResultsListView.SelectedIndex == -1 && SearchResultsListView.Items.Count > 0)
                SearchResultsListView.SelectedIndex = 0;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange > 0)
            {
                if (e.VerticalOffset > e.ExtentHeight - 2 * e.ViewportHeight)
                {
                    EverythingSearch.Instance.QueryBatch();
                    ScrollToVerticalOffset(e.VerticalOffset);
                }
            }
        }

        public void ScrollToVerticalOffset(double verticalOffset)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                GetScrollViewer().ScrollToVerticalOffset(verticalOffset);
            }), DispatcherPriority.ContextIdle);
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

        public void PageUp()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GetScrollViewer().PageUp();
            }), DispatcherPriority.ContextIdle);
        }

        public void PageDown()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GetScrollViewer().PageDown();
            }), DispatcherPriority.ContextIdle);
        }

        public void ScrollToHome()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GetScrollViewer().ScrollToHome();
            }), DispatcherPriority.ContextIdle);
        }

        public void ScrollToEnd()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GetScrollViewer().ScrollToEnd();
            }), DispatcherPriority.ContextIdle);
        }

        public void OpenSelectedSearchResult()
        {
            if (SearchResultsListView.SelectedIndex == -1)
                SelectNextSearchResult();

            if (SearchResultsListView.SelectedIndex != -1)
            {
                if (Rules.HandleRule(SelectedItem))
                    return;

                SelectedItem?.Open();
            }
        }

        public void OpenFilePath(object sender, RoutedEventArgs e)
        {
            SelectedItem?.OpenPath();
        }

        public void PreviewSelectedFile()
        {
            SelectedItem?.PreviewInQuickLook();
        }

        private ScrollViewer GetScrollViewer()
        {
            Decorator listViewBorder = VisualTreeHelper.GetChild(SearchResultsListView, 0) as Decorator;
            return listViewBorder.Child as ScrollViewer;
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
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Alt:
                    SelectedItem?.ShowProperties();
                    break;
                case ModifierKeys.Control:
                    SelectedItem?.OpenPath();
                    break;
                case ModifierKeys.Shift:
                    SelectedItem?.ShowInEverything();
                    break;
                default:
                    OpenSelectedSearchResult();
                    break;
            }
        }

        public void RunAsAdmin(object sender, RoutedEventArgs e)
        {
            SelectedItem?.RunAsAdmin();
        }

        public void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            SelectedItem?.ShowProperties();
        }

        public void ShowFileWindowsContexMenu(object sender, RoutedEventArgs e)
        {
            SelectedItem?.ShowWindowsContexMenu();
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

        private void OnListViewItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            dragStart = PointToScreen(Mouse.GetPosition(this));
        }

        private void OnListViewItemMouseMove(object sender, MouseEventArgs e)
        {
            var diff = dragStart - PointToScreen(Mouse.GetPosition(this));

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                if (SearchResultsListView.SelectedItems.Count == 0)
                    return;

                string[] files = { SelectedItem?.FullPathAndFileName };
                var data = new DataObject(DataFormats.FileDrop, files);
                data.SetData(DataFormats.Text, files[0]);
                DragDrop.DoDragDrop(SearchResultsListView, data, DragDropEffects.All);
            }
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;
            MenuItem mi = cm.Items[2] as MenuItem;

            string[] extensions = { ".exe", ".bat", ".cmd" };
            bool isExecutable = (bool)SelectedItem?.IsFile && extensions.Any(ext => SelectedItem.FullPathAndFileName.EndsWith(ext));

            if (isExecutable)
                mi.Visibility = Visibility.Visible;
            else
                mi.Visibility = Visibility.Collapsed;
        }
    }
}
