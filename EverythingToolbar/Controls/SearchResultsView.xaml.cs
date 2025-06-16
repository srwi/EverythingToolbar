using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SearchResult = EverythingToolbar.Data.SearchResult;

namespace EverythingToolbar.Controls
{
    public partial class SearchResultsView
    {
        public static readonly DependencyProperty TotalResultsCountProperty =
            DependencyProperty.Register(
                nameof(TotalResultsCount),
                typeof(int),
                typeof(SearchResultsView),
                new PropertyMetadata(0));

        public int TotalResultsCount
        {
            get => (int)GetValue(TotalResultsCountProperty);
            set => SetValue(TotalResultsCountProperty, value);
        }

        private SearchResult? SelectedItem => SearchResultsListView.SelectedItem as SearchResult;
        private const int PageSize = 256;
        private Point _dragStart;
        private bool _isScrollBarDragging;
        private VirtualizingCollection<SearchResult>? _searchResultsCollection;
        private readonly DispatcherTimer _busyIndicatorTimer;
        private const int BusyIndicatorDelayMilliseconds = 2000;

        public SearchResultsView()
        {
            InitializeComponent();

            SearchState.Instance.PropertyChanged += (_, _) => UpdateSearchResultsProvider(SearchState.Instance);
            EventDispatcher.Instance.GlobalKeyEvent += OnKeyPressed;
            SearchResultsListView.PreviewKeyDown += OnKeyPressed;

            _busyIndicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BusyIndicatorDelayMilliseconds)
            };
            _busyIndicatorTimer.Tick += BusyIndicatorTimerElapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateSearchResultsProvider(SearchState.Instance);

            // Mouse events and context menu must be added to the ItemContainerStyle each time it gets updated
            RegisterItemContainerStyleProperties();

            AutoSelectFirstResult();
            AttachToScrollViewer();
        }

        private void UpdateSearchResultsProvider(SearchState searchState)
        {
            if (ToolbarSettings.User.IsHideEmptySearchResults && string.IsNullOrEmpty(searchState.SearchTerm))
            {
                SearchResultsListView.ItemsSource = null;
                TotalResultsCount = 0;
                return;
            }

            SearchResultProvider newProvider = new(searchState);

            if (_searchResultsCollection == null)
            {
                _searchResultsCollection = new VirtualizingCollection<SearchResult>(newProvider, PageSize);
                _searchResultsCollection.CollectionChanged += (_, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Reset)
                    {
                        TotalResultsCount = _searchResultsCollection.Count;
                        Dispatcher.BeginInvoke(AutoSelectFirstResult);
                    }
                };
                _searchResultsCollection.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(VirtualizingCollection<SearchResult>.IsBusy))
                    {
                        OnCollectionIsBusyChanged();
                    }
                };
            }
            else
            {
                _searchResultsCollection?.UpdateProvider(newProvider);
            }

            SearchResultsListView.ItemsSource = _searchResultsCollection;
        }

        private void OnCollectionIsBusyChanged()
        {
            if (_searchResultsCollection is { IsBusy: true })
            {
                if (!_busyIndicatorTimer.IsEnabled)
                {
                    _busyIndicatorTimer.Start();
                }
            }
            else
            {
                _busyIndicatorTimer.Stop();
                SpinnerOverlay.Visibility = Visibility.Collapsed;
                SearchResultsListView.Opacity = 1.0;
            }
        }

        private void BusyIndicatorTimerElapsed(object? sender, EventArgs e)
        {
            _busyIndicatorTimer.Stop();

            if (_searchResultsCollection is not { IsBusy: true })
                return;

            SpinnerOverlay.Visibility = Visibility.Visible;
            SearchResultsListView.Opacity = 0.3;
        }

        private void AttachToScrollViewer()
        {
            var listViewBorder = VisualTreeHelper.GetChild(SearchResultsListView, 0) as Decorator;

            var scrollViewer = listViewBorder?.Child as ScrollViewer;
            if (scrollViewer == null)
                return;

            var verticalScrollBar = FindVisualChild<ScrollBar>(scrollViewer, s => s.Orientation == Orientation.Vertical);
            if (verticalScrollBar == null)
                return;

            verticalScrollBar.PreviewMouseLeftButtonDown += ScrollBar_PreviewMouseLeftButtonDown;
            verticalScrollBar.PreviewMouseLeftButtonUp += ScrollBar_PreviewMouseLeftButtonUp;
            verticalScrollBar.MouseLeave += ScrollBar_MouseLeave;
        }

        private void ScrollBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_searchResultsCollection != null)
            {
                _isScrollBarDragging = true;
                _searchResultsCollection.IsAsync = false;
            }
        }

        private void ScrollBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetScrollBarDragging();
        }

        private void ScrollBar_MouseLeave(object sender, MouseEventArgs e)
        {
            ResetScrollBarDragging();
        }

        private void ResetScrollBarDragging()
        {
            if (_isScrollBarDragging && _searchResultsCollection != null)
            {
                _isScrollBarDragging = false;
                _searchResultsCollection.IsAsync = true;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? condition = null) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (condition == null || condition(typedChild)))
                    return typedChild;

                var result = FindVisualChild(child, condition);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void RegisterItemContainerStyleProperties()
        {
            SearchResultsListView.ItemContainerStyle ??= new Style(typeof(ListViewItem));

            var newStyle = new Style(typeof(ListViewItem), SearchResultsListView.ItemContainerStyle);
            newStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseLeftButtonUpEvent,
                Handler = new MouseButtonEventHandler(SingleClickSearchResult)
            });
            newStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseDoubleClickEvent,
                Handler = new MouseButtonEventHandler(DoubleClickSearchResult)
            });
            newStyle.Setters.Add(new EventSetter
            {
                Event = PreviewMouseDownEvent,
                Handler = new MouseButtonEventHandler(OnListViewItemMouseDown)
            });
            newStyle.Setters.Add(new EventSetter
            {
                Event = MouseMoveEvent,
                Handler = new MouseEventHandler(OnListViewItemMouseMove)
            });
            newStyle.Setters.Add(new Setter
            {
                Property = ContextMenuProperty,
                Value = new Binding { Source = Resources["ListViewItemContextMenu"] }
            });
            SearchResultsListView.ItemContainerStyle = newStyle;
        }

        private void OnKeyPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PreviewSelectedFile();
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Enter)
            {
                RunAsAdmin(this, new RoutedEventArgs());
                SearchResultsListView.SelectedIndex = -1;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift && e.Key == Key.Enter)
            {
                if (SelectedItem == null)
                    return;

                SearchResultProvider.OpenSearchInEverything(SearchState.Instance, SelectedItem.FullPathAndFileName);
                SearchResultsListView.SelectedIndex = -1;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                OpenFilePath(this, new RoutedEventArgs());
                SearchResultsListView.SelectedIndex = -1;
            }
            else if (e.Key == Key.Enter)
            {
                if (SearchResultsListView.SelectedIndex >= 0)
                {
                    OpenSelectedSearchResult();
                    SearchResultsListView.SelectedIndex = -1;
                }
                else
                {
                    SelectNextSearchResult();
                }
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
            else if (e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End)
            {
                e.Handled = ForwardKeyPressToControl(SearchResultsListView, e.Key);
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

        private void AutoSelectFirstResult()
        {
            if (!ToolbarSettings.User.IsAutoSelectFirstResult)
                return;

            SelectNthSearchResult(0);
        }

        private void SelectNextSearchResult()
        {
            SelectNthSearchResult(SearchResultsListView.SelectedIndex + 1);
        }

        private void SelectPreviousSearchResult()
        {
            SelectNthSearchResult(SearchResultsListView.SelectedIndex - 1);
        }

        private void SelectNthSearchResult(int n)
        {
            if (n < 0 || n >= SearchResultsListView.Items.Count)
                return;

            SearchResultsListView.SelectedIndex = n;
            if (SelectedItem != null)
                SearchResultsListView.ScrollIntoView(SelectedItem);

            if (!ToolbarSettings.User.IsAutoSelectFirstResult || !ToolbarSettings.User.IsSearchAsYouType)
                FocusSelectedItem();
        }

        private bool ForwardKeyPressToControl(Control control, Key key)
        {
            var presentationSource = PresentationSource.FromVisual(control);
            if (presentationSource == null)
                return false;

            // We want to be able to restore focus to the text box later
            var currentFocus = Keyboard.FocusedElement;
            var caretIndex = currentFocus is TextBox textBox ? textBox.CaretIndex : -1;

            var args = new KeyEventArgs(Keyboard.PrimaryDevice, presentationSource, 0, key) { RoutedEvent = Keyboard.KeyDownEvent };
            control.RaiseEvent(args);

            // Restore focus to text box
            if (ToolbarSettings.User.IsAutoSelectFirstResult &&
                currentFocus is TextBox restoredTextBox &&
                caretIndex >= 0)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    currentFocus.Focus();
                    restoredTextBox.CaretIndex = Math.Min(caretIndex, restoredTextBox.Text.Length);
                }), DispatcherPriority.Send);
            }

            return args.Handled;
        }

        private void OpenSelectedSearchResult()
        {
            if (SelectedItem == null)
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
                OpenWithMouseClick();
        }

        private void DoubleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (ToolbarSettings.User.IsDoubleClickToOpen)
                OpenWithMouseClick();
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            OpenSelectedSearchResult();
        }

        private void OpenWithMouseClick()
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
                default:
                    OpenSelectedSearchResult();
                    break;
            }
            SearchResultsListView.SelectedIndex = -1;
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
            if (sender is not MenuItem menuItem)
                return;

            while (menuItem.Items.Count > 2)
                menuItem.Items.RemoveAt(0);

            List<Rule> rules = Rules.LoadRules();

            if (rules.Count == 0)
            {
                menuItem.Items.Insert(0, new MenuItem
                {
                    Header = Properties.Resources.ContextMenuOpenWithNoRules,
                    IsEnabled = false
                });
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                MenuItem ruleMenuItem = new() { Header = rules[i].Name, Tag = rules[i].Command };
                ruleMenuItem.Click += OpenWithRule;
                menuItem.Items.Insert(i, ruleMenuItem);
            }
        }

        private void OpenWithRule(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            var menuItem = sender as MenuItem;
            var command = menuItem?.Tag?.ToString() ?? "";
            Rules.HandleRule(SelectedItem, command);
        }

        private void OnListViewItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = PointToScreen(Mouse.GetPosition(this));
        }

        private void OnListViewItemMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || SelectedItem == null)
                return;

            var diff = _dragStart - PointToScreen(Mouse.GetPosition(this));

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                string[] files = [SelectedItem.FullPathAndFileName];
                var data = new DataObject(DataFormats.FileDrop, files);
                data.SetData(DataFormats.Text, files[0]);
                DragDrop.DoDragDrop(SearchResultsListView, data, DragDropEffects.All);
            }
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            var cm = sender as ContextMenu;
            var mi = cm?.Items[2] as MenuItem;
            if (mi == null)
                return;

            string[] extensions = [".exe", ".bat", ".cmd"];
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