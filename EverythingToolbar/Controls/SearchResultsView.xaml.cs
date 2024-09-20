using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public partial class SearchResultsView
    {
        private SearchResult SelectedItem => SearchResultsListView.SelectedItem as SearchResult;
        private Point _dragStart;

        public SearchResultsView()
        {
            InitializeComponent();

            SearchResultsListView.ItemsSource = EverythingSearch.Instance.SearchResults;
            ((INotifyCollectionChanged)SearchResultsListView.Items).CollectionChanged += AutoSelectFirstResult;

            EventDispatcher.Instance.GlobalKeyEvent += OnKeyPressed;
            SearchResultsListView.PreviewKeyDown += OnKeyPressed;

            // Mouse events and context menu must be added to the ItemContainerStyle each time it gets updated
            Loaded += (s, e) =>
            {
                RegisterItemContainerStyleProperties(null, null);
                ThemeAwareness.ResourceChanged += RegisterItemContainerStyleProperties;
            };
        }

        private void RegisterItemContainerStyleProperties(object sender, ResourcesChangedEventArgs e)
        {
            SearchResultsListView.ItemContainerStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseLeftButtonUpEvent,
                Handler = new MouseButtonEventHandler(SingleClickSearchResult)
            });
            SearchResultsListView.ItemContainerStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseDoubleClickEvent,
                Handler = new MouseButtonEventHandler(DoubleClickSearchResult)
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
            SearchResultsListView.ItemContainerStyle.Setters.Add(new Setter
            {
                Property = ContextMenuProperty,
                Value = new Binding { Source = Resources["ListViewItemContextMenu"] }
            });
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PreviewSelectedFile();
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Enter)
            {
                RunAsAdmin(this, null);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                if (SearchResultsListView.SelectedItem == null)
                    return;

                var path = ((SearchResult)SearchResultsListView.SelectedItem).FullPathAndFileName;
                EverythingSearch.Instance.OpenLastSearchInEverything(path);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                OpenFilePath(this, null);
            }
            else if (e.Key == Key.Enter)
            {
                if (SearchResultsListView.SelectedIndex >= 0)
                    OpenSelectedSearchResult();
                else
                    SelectNextSearchResult();
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.C)
            {
                SelectedItem?.CopyPathToClipboard();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                SelectedItem?.CopyToClipboard();
            }
            else if (e.Key == Key.Up)
            {
                if (SearchResultsListView.SelectedIndex == 0 &&
                    (!ToolbarSettings.User.IsAutoSelectFirstResult || !ToolbarSettings.User.IsSearchAsYouType))
                {
                    SearchResultsListView.SelectedIndex = -1;
                    EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
                }
                else
                {
                    SelectPreviousSearchResult();
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                SelectNextSearchResult();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                PageDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                SelectFirstSearchResult();
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                SelectLastSearchResult();
                e.Handled = true;
            }
            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToolbarSettings.User.IsMatchCase = !ToolbarSettings.User.IsMatchCase;
            }
            else if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToolbarSettings.User.IsMatchWholeWord = !ToolbarSettings.User.IsMatchWholeWord;
            }
            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToolbarSettings.User.IsMatchPath = !ToolbarSettings.User.IsMatchPath;
            }
            else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToolbarSettings.User.IsRegExEnabled = !ToolbarSettings.User.IsRegExEnabled;
            }
        }

        private void AutoSelectFirstResult(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!ToolbarSettings.User.IsAutoSelectFirstResult)
                return;

            if (SearchResultsListView.SelectedItems.Count == 0 && !SearchResultsListView.Items.IsEmpty)
                SelectNthSearchResult(0);
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange <= 0)
                return;
            
            if (e.VerticalOffset > e.ExtentHeight - 2 * e.ViewportHeight)
            {
                EverythingSearch.Instance.QueryBatch(append: true);
                ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void ScrollToVerticalOffset(double verticalOffset)
        {
            Dispatcher.Invoke(() =>
            {
                GetScrollViewer().ScrollToVerticalOffset(verticalOffset);
            }, DispatcherPriority.ContextIdle);
        }

        private void SelectNextSearchResult()
        {
            SelectNthSearchResult(SearchResultsListView.SelectedIndex + 1);
        }

        private void SelectPreviousSearchResult()
        {
            SelectNthSearchResult(SearchResultsListView.SelectedIndex - 1);
        }

        private void SelectFirstSearchResult()
        {
            SelectNthSearchResult(0);
        }

        private void SelectLastSearchResult()
        {
            SelectNthSearchResult(SearchResultsListView.Items.Count - 1);
        }

        private void SelectNthSearchResult(int n)
        {
            if (n < 0 || n >= SearchResultsListView.Items.Count)
                return;

            SearchResultsListView.SelectedIndex = n;
            SearchResultsListView.ScrollIntoView(SelectedItem);
            
            if (!ToolbarSettings.User.IsAutoSelectFirstResult || !ToolbarSettings.User.IsSearchAsYouType)
                FocusSelectedItem();
        }

        private int? GetPageSize()
        {
            var item = SearchResultsListView.ItemContainerGenerator.ContainerFromIndex(SearchResultsListView.SelectedIndex) as ListViewItem;
            if (item == null)
                return null;

            return (int)(SearchResultsListView.ActualHeight / item.ActualHeight);
        }

        private void PageUp()
        {
            if (SearchResultsListView.SelectedIndex < 0)
                return;

            var pageSize = GetPageSize();
            if (pageSize == null)
                return;

            var stepSize = Math.Max(0, pageSize.Value - 1);
            var newIndex = Math.Max(SearchResultsListView.SelectedIndex - stepSize, 0);
            SearchResultsListView.SelectedIndex = newIndex;
            SearchResultsListView.ScrollIntoView(SelectedItem);
        }

        private void PageDown()
        {
            if (SearchResultsListView.SelectedIndex < 0)
                return;

            var pageSize = GetPageSize();
            if (pageSize == null)
                return;

            var stepSize = Math.Max(0, pageSize.Value - 1);
            var newIndex = Math.Min(SearchResultsListView.SelectedIndex + stepSize, SearchResultsListView.Items.Count - 1);
            SearchResultsListView.SelectedIndex = newIndex;
            SearchResultsListView.ScrollIntoView(SelectedItem);
        }

        private void OpenSelectedSearchResult()
        {
            if (SearchResultsListView.SelectedIndex == -1)
                return;

            if (!Rules.HandleRule(SelectedItem))
                SelectedItem?.Open();

            SearchWindow.Instance.Hide();
        }

        private void OpenFilePath(object sender, RoutedEventArgs e)
        {
            SelectedItem?.OpenPath();
            SearchWindow.Instance.Hide();
        }

        private void PreviewSelectedFile()
        {
            SelectedItem?.PreviewInQuickLook();
            SelectedItem?.PreviewInSeer();
        }

        private ScrollViewer GetScrollViewer()
        {
            var listViewBorder = VisualTreeHelper.GetChild(SearchResultsListView, 0) as Decorator;
            return listViewBorder?.Child as ScrollViewer;
        }

        private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
        {
            SelectedItem?.CopyPathToClipboard();
        }

        private void OpenWith(object sender, RoutedEventArgs e)
        {
            SelectedItem?.OpenWith();
            SearchWindow.Instance.Hide();
        }

        private void ShowInEverything(object sender, RoutedEventArgs e)
        {
            SelectedItem?.ShowInEverything();
            SearchWindow.Instance.Hide();
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            SelectedItem?.CopyToClipboard();
        }

        private void SingleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (!ToolbarSettings.User.IsDoubleClickToOpen)
                Open();
        }

        private void DoubleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (ToolbarSettings.User.IsDoubleClickToOpen)
                Open();
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            OpenSelectedSearchResult();
        }

        private void Open()
        {
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Alt:
                    SelectedItem?.ShowProperties();
                    SearchWindow.Instance.Hide();
                    break;
                case ModifierKeys.Control:
                    SelectedItem?.OpenPath();
                    SearchWindow.Instance.Hide();
                    break;
                case ModifierKeys.Shift:
                    SelectedItem?.ShowInEverything();
                    SearchWindow.Instance.Hide();
                    break;
                case ModifierKeys.None:
                case ModifierKeys.Windows:
                default:
                    OpenSelectedSearchResult();
                    break;
            }
        }

        private void RunAsAdmin(object sender, RoutedEventArgs e)
        {
            SelectedItem?.RunAsAdmin();
            SearchWindow.Instance.Hide();
        }

        private void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            SelectedItem?.ShowProperties();
            SearchWindow.Instance.Hide();
        }

        private void ShowFileWindowsContextMenu(object sender, RoutedEventArgs e)
        {
            SelectedItem?.ShowWindowsContextMenu();
        }

        private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            if (mi == null)
                return;

            while (mi.Items.Count > 3)
                mi.Items.RemoveAt(0);

            var rules = Rules.LoadRules();

            if (rules.Count > 0)
                (mi.Items[0] as MenuItem).Visibility = Visibility.Collapsed;
            else
                (mi.Items[0] as MenuItem).Visibility = Visibility.Visible;

            for (var i = rules.Count - 1; i >= 0; i--)
            {
                var rule = rules[i];
                var ruleMenuItem = new MenuItem { Header = rule.Name, Tag = rule.Command };
                ruleMenuItem.Click += OpenWithRule;
                mi.Items.Insert(0, ruleMenuItem);
            }
        }

        private void OpenWithRule(object sender, RoutedEventArgs e)
        {
            if (SearchResultsListView.SelectedItem == null)
                return;
            
            var searchResult = SearchResultsListView.SelectedItem as SearchResult;
            var command = (sender as MenuItem).Tag?.ToString() ?? "";
            Rules.HandleRule(searchResult, command);
        }

        private void OnListViewItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = PointToScreen(Mouse.GetPosition(this));
        }

        private void OnListViewItemMouseMove(object sender, MouseEventArgs e)
        {
            var diff = _dragStart - PointToScreen(Mouse.GetPosition(this));

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
            var cm = sender as ContextMenu;
            var mi = cm?.Items[2] as MenuItem;
            if (mi == null)
                return;

            string[] extensions = { ".exe", ".bat", ".cmd" };
            var isExecutable = SelectedItem.IsFile && extensions.Any(ext => SelectedItem.FullPathAndFileName.EndsWith(ext));

            mi.Visibility = isExecutable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FocusSelectedItem()
        {
            var selectedItem = (ListViewItem)SearchResultsListView.ItemContainerGenerator.ContainerFromItem(SelectedItem);
            if (selectedItem != null)
                Keyboard.Focus(selectedItem);
        }
    }
}
