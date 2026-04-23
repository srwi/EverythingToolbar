using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using EverythingToolbar.Settings;
using SearchResult = EverythingToolbar.Data.SearchResult;

namespace EverythingToolbar.Controls
{
    public partial class SearchResultsView
    {
        public static readonly DependencyProperty SelectedSearchResultProperty = DependencyProperty.Register(
            nameof(SelectedSearchResult),
            typeof(SearchResult),
            typeof(SearchResultsView),
            new PropertyMetadata(null)
        );

        public static readonly DependencyProperty TotalResultsCountProperty = DependencyProperty.Register(
            nameof(TotalResultsCount),
            typeof(int),
            typeof(SearchResultsView),
            new PropertyMetadata(0)
        );

        public int TotalResultsCount
        {
            get => (int)GetValue(TotalResultsCountProperty);
            set => SetValue(TotalResultsCountProperty, value);
        }

        public SearchResult? SelectedSearchResult
        {
            get => (SearchResult?)GetValue(SelectedSearchResultProperty);
            set => SetValue(SelectedSearchResultProperty, value);
        }

        private SearchResult? SelectedItem => SelectedSearchResult;

        private IEnumerable<SearchResult> GetSelectedItems()
        {
            return SearchResultsListView.SelectedItems.Cast<SearchResult>();
        }

        private const int PageSize = 256;
        private Point _dragStart;
        private bool _isScrollBarDragging;
        private int? _touchId;
        private VirtualizingCollection<SearchResult>? _searchResultsCollection;
        private SynchronizationContext _synchronizationContext = new();
        private readonly DispatcherTimer _busyIndicatorTimer;
        private const int BusyIndicatorDelayMilliseconds = 2000;

        public SearchResultsView()
        {
            InitializeComponent();

            SearchState.Instance.PropertyChanged += (_, _) => UpdateSearchResultsProvider(SearchState.Instance);
            EventDispatcher.Instance.GlobalKeyEvent += OnKeyPressed;
            SearchResultsListView.PreviewKeyDown += OnKeyPressed;
            SearchResultsListView.PreviewMouseLeftButtonDown += OnPreviewLeftMouseButtonDown;

            _busyIndicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BusyIndicatorDelayMilliseconds),
            };
            _busyIndicatorTimer.Tick += BusyIndicatorTimerElapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();

            UpdateSearchResultsProvider(SearchState.Instance);

            AutoSelectFirstResult();
            AttachToScrollViewer();
        }

        private void UpdateSearchResultsProvider(SearchState searchState)
        {
            if (ToolbarSettings.User.IsHideEmptySearchResults && string.IsNullOrEmpty(searchState.SearchTerm))
            {
                _searchResultsCollection = null;
                SearchResultsListView.ItemsSource = null;
                TotalResultsCount = 0;
                return;
            }

            SearchResultProvider newProvider = new(searchState, _synchronizationContext);

            if (_searchResultsCollection == null)
            {
                _searchResultsCollection = new VirtualizingCollection<SearchResult>(
                    newProvider,
                    PageSize,
                    _synchronizationContext
                );
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

            var verticalScrollBar = FindVisualChild<ScrollBar>(
                scrollViewer,
                s => s.Orientation == Orientation.Vertical
            );
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

        private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? condition = null)
            where T : DependencyObject
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

        private void OnPreviewLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.OriginalSource is not DependencyObject source)
                    return;

                ListViewItem? item = ItemsControl.ContainerFromElement(SearchResultsListView, source) as ListViewItem;
                if (item?.IsSelected == true)
                    e.Handled = true;
            }
        }

        private void OnKeyPressed(object? sender, KeyEventArgs e)
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
                var first = GetSelectedItems().FirstOrDefault();
                if (first == null) return;

                SearchResultProvider.OpenSearchInEverything(SearchState.Instance, first.FullPathAndFileName);
                SearchResultsListView.SelectedIndex = -1;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                OpenFilePath(this, new RoutedEventArgs());
                SearchResultsListView.SelectedIndex = -1;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Alt && (e.Key == Key.Enter || e.SystemKey == Key.Enter))
            {
                ShowFileProperties(this, new RoutedEventArgs());
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
                var paths = string.Join(Environment.NewLine, GetSelectedItems().Select(i => i.FullPathAndFileName));
                if (!string.IsNullOrEmpty(paths)) Clipboard.SetText(paths);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                var paths = new StringCollection();
                foreach (var item in GetSelectedItems()) paths.Add(item.FullPathAndFileName);
                if (paths.Count > 0) Clipboard.SetFileDropList(paths);
            }
            else if (e.Key == Key.Up)
            {
                HandleUpNavigation();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                HandleDownNavigation();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp || e.Key == Key.PageDown || e.Key == Key.Home || e.Key == Key.End)
            {
                var restoreFocus = e.Key is Key.Home or Key.End && KeepSearchBoxFocused;
                e.Handled = ForwardKeyPressToControl(SearchResultsListView, e.Key, restoreFocus: restoreFocus);
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

        private static bool KeepSearchBoxFocused =>
            ToolbarSettings.User.IsAutoSelectFirstResult && ToolbarSettings.User.IsSearchAsYouType;

        private static FocusBehavior EffectiveListFocusBehavior =>
            KeepSearchBoxFocused && ToolbarSettings.User.ListFocusBehavior == FocusBehavior.RepeatWithSearch
                ? FocusBehavior.Repeat
                : ToolbarSettings.User.ListFocusBehavior;

        private void AutoSelectFirstResult()
        {
            if (ToolbarSettings.User.IsAutoSelectFirstResult)
                SelectNthSearchResult(0);
            else
                SearchResultsListView.SelectedIndex = -1;
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

            if (!KeepSearchBoxFocused)
                FocusSelectedItem();
        }

        private void JumpToEnd()
        {
            var originalFocus = Keyboard.FocusedElement;
            SearchResultsListView.Focus();
            ForwardKeyPressToControl(SearchResultsListView, Key.End, originalFocus, restoreFocus: KeepSearchBoxFocused);
        }

        private void FocusSearchBox()
        {
            SearchResultsListView.SelectedIndex = -1;
            EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
        }

        private void HandleUpNavigation()
        {
            if (SearchResultsListView.SelectedIndex > 0)
            {
                SelectPreviousSearchResult();
            }
            else if (SearchResultsListView.SelectedIndex == 0)
            {
                switch (EffectiveListFocusBehavior)
                {
                    case FocusBehavior.Repeat:
                        JumpToEnd();
                        break;
                    case FocusBehavior.RepeatWithSearch:
                        FocusSearchBox();
                        break;
                    case FocusBehavior.Clamp:
                    default:
                        if (!ToolbarSettings.User.IsAutoSelectFirstResult)
                            FocusSearchBox();
                        break;
                }
            }
            else
            {
                if (EffectiveListFocusBehavior != FocusBehavior.Clamp)
                    JumpToEnd();
            }
        }

        private void HandleDownNavigation()
        {
            if (SearchResultsListView.SelectedIndex == SearchResultsListView.Items.Count - 1)
            {
                switch (EffectiveListFocusBehavior)
                {
                    case FocusBehavior.Repeat:
                        SelectNthSearchResult(0);
                        break;
                    case FocusBehavior.RepeatWithSearch:
                        FocusSearchBox();
                        break;
                    case FocusBehavior.Clamp:
                    default:
                        break;
                }
            }
            else
            {
                SelectNextSearchResult();
            }
        }

        private bool ForwardKeyPressToControl(
            Control control,
            Key key,
            IInputElement? originalFocus = null,
            bool restoreFocus = false
        )
        {
            var presentationSource = PresentationSource.FromVisual(control);
            if (presentationSource == null)
                return false;

            originalFocus ??= Keyboard.FocusedElement;
            var caretIndex = originalFocus is TextBox textBox ? textBox.CaretIndex : -1;

            var args = new KeyEventArgs(Keyboard.PrimaryDevice, presentationSource, 0, key)
            {
                RoutedEvent = Keyboard.KeyDownEvent,
            };
            control.RaiseEvent(args);

            if (restoreFocus && originalFocus is TextBox restoredTextBox && caretIndex >= 0)
            {
                Dispatcher.BeginInvoke(
                    (Action)(
                        () =>
                        {
                            originalFocus.Focus();
                            restoredTextBox.CaretIndex = Math.Min(caretIndex, restoredTextBox.Text.Length);
                        }
                    ),
                    DispatcherPriority.Send
                );
            }

            return args.Handled;
        }

        private void OpenSelectedSearchResult()
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0) return;

            foreach (var item in items)
            {
                if (!CustomActions.HandleAction(item))
                    item.Open();
            }
            SearchWindow.Instance.Hide();
        }

        private void OpenFilePath(object sender, RoutedEventArgs e)
        {
            foreach (var item in GetSelectedItems())
                item.OpenPath();
            SearchWindow.Instance.Hide();
        }

        private void PreviewSelectedFile()
        {
            var first = GetSelectedItems().FirstOrDefault();
            first?.PreviewInQuickLook();
            first?.PreviewInSeer();
        }

        private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
        {
            var paths = string.Join(Environment.NewLine, GetSelectedItems().Select(i => i.FullPathAndFileName));
            if (!string.IsNullOrEmpty(paths))
                Clipboard.SetText(paths);
        }

        private void OpenWith(object sender, RoutedEventArgs e)
        {
            foreach (var item in GetSelectedItems())
                item.OpenWith();
            SearchWindow.Instance.Hide();
        }

        private void ShowInEverything(object sender, RoutedEventArgs e)
        {
            foreach (var item in GetSelectedItems())
                item.ShowInEverything();
            SearchWindow.Instance.Hide();
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            var paths = new StringCollection();
            foreach (var item in GetSelectedItems()) paths.Add(item.FullPathAndFileName);

            if (paths.Count > 0)
                Clipboard.SetFileDropList(paths);
        }

        private void SingleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (ToolbarSettings.User.IsSelectionModeEnabled)
                return;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                return;

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
                    foreach (var item in GetSelectedItems()) item.ShowProperties();
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
            foreach (var item in GetSelectedItems())
                item.RunAsAdmin();
            SearchWindow.Instance.Hide();
        }

        private void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            foreach (var item in GetSelectedItems())
                item.ShowProperties();
            SearchWindow.Instance.Hide();
        }

        private void ShowFileWindowsContextMenu(object sender, RoutedEventArgs e)
        {
            SearchResult.ShowWindowsContextMenu(GetSelectedItems());
        }

        private void Cut(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0)
                return;

            foreach (var item in items)
                item.CutToClipboard();
        }

        private void Rename(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0)
                return;

            if (items.Count > 1)
            {
                FluentMessageBox
                    .CreateError(
                        Properties.Resources.MessageBoxRenameSingleItemOnly,
                        Properties.Resources.MessageBoxErrorTitle
                    )
                    .ShowDialogAsync();
                return;
            }

            items[0].Rename();
            SearchWindow.Instance.Hide();
        }

        private void DeleteToRecycleBin(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0)
                return;

            foreach (var item in items)
                item.DeleteToRecycleBin();

            SearchWindow.Instance.Hide();
        }

        private async void DeletePermanently(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0)
                return;

            var message = items.Count == 1
                ? string.Format(
                    Properties.Resources.MessageBoxDeletePermanentlyConfirm,
                    items[0].FileName
                )
                : string.Format(
                    Properties.Resources.MessageBoxDeletePermanentlyConfirmMultiple,
                    items.Count
                );

            var result = await FluentMessageBox
                .CreateYesNo(message, Properties.Resources.MessageBoxWarningTitle)
                .ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                foreach (var item in items)
                    item.DeletePermanently();
            }

            SearchWindow.Instance.Hide();
        }

        private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            while (menuItem.Items.Count > 2)
                menuItem.Items.RemoveAt(0);

            List<Rule> actions = CustomActions.LoadCustomActions();

            if (actions.Count == 0)
            {
                menuItem.Items.Insert(
                    0,
                    new MenuItem { Header = Properties.Resources.ContextMenuOpenWithNoCustomActions, IsEnabled = false }
                );
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                Rule action = actions[i];
                MenuItem actionMenuItem = new()
                {
                    Header = action.Name,
                    Tag = action.Command,
                    DataContext = action,
                };
                if (action.Icon != null)
                {
                    Image iconImage = new() { Width = 16, Height = 16 };
                    iconImage.SetBinding(Image.SourceProperty, new Binding("Icon"));
                    actionMenuItem.Icon = iconImage;
                }
                actionMenuItem.Click += OpenWithCustomAction;
                menuItem.Items.Insert(i, actionMenuItem);
            }
        }

        private void OpenWithCustomAction(object sender, RoutedEventArgs e)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0) return;

            var menuItem = sender as MenuItem;
            var command = menuItem?.Tag?.ToString() ?? "";

            foreach (var item in items)
            {
                CustomActions.HandleAction(item, command);
            }
        }

        private void OnListViewItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = PointToScreen(Mouse.GetPosition(this));
        }

        private void OnListViewItemMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            TryStartDragDrop(PointToScreen(Mouse.GetPosition(this)));
        }

        private void OnListViewItemTouchDown(object sender, TouchEventArgs e)
        {
            _touchId = e.TouchDevice.Id;
            _dragStart = PointToScreen(e.GetTouchPoint(this).Position);
        }

        private void OnListViewItemTouchMove(object sender, TouchEventArgs e)
        {
            if (_touchId != e.TouchDevice.Id)
                return;

            if (TryStartDragDrop(PointToScreen(e.GetTouchPoint(this).Position)))
                _touchId = null;
        }

        private void OnListViewItemTouchUp(object sender, TouchEventArgs e)
        {
            if (_touchId == e.TouchDevice.Id)
                _touchId = null;
        }

        private bool TryStartDragDrop(Point currentPosition)
        {
            var items = GetSelectedItems().ToList();
            if (items.Count == 0)
                return false;

            var diff = _dragStart - currentPosition;

            if (
                Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance
            )
                return false;

            string[] files = items.Select(i => i.FullPathAndFileName).ToArray();
            var data = new DataObject(DataFormats.FileDrop, files);
            data.SetData(DataFormats.Text, string.Join(Environment.NewLine, files));
            DragDrop.DoDragDrop(SearchResultsListView, data, DragDropEffects.All);
            return true;
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (SelectedItem == null)
                return;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                SearchResult.ShowWindowsContextMenu(GetSelectedItems());
                e.Handled = true;
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
            var isExecutable =
                SelectedItem.IsFile && extensions.Any(ext => SelectedItem.FullPathAndFileName.EndsWith(ext));

            mi.Visibility = isExecutable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FocusSelectedItem()
        {
            var selectedItem = (ListViewItem)
                SearchResultsListView.ItemContainerGenerator.ContainerFromItem(SelectedItem);
            if (selectedItem != null)
                Keyboard.Focus(selectedItem);
        }
    }
}