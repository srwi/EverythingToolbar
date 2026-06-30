using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
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
        private Point _dragStart;
        private bool _isScrollBarDragging;
        private bool _syncingSelection;
        private int? _touchId;
        private readonly DispatcherTimer _busyIndicatorTimer;
        private const int BusyIndicatorDelayMilliseconds = 2000;
        private readonly SearchSession _session = Ioc.Default.GetRequiredService<SearchSession>();
        private readonly SearchResultActions _actions = Ioc.Default.GetRequiredService<SearchResultActions>();
        private readonly CustomActionService _customActions = Ioc.Default.GetRequiredService<CustomActionService>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        private static ISearchWindowController SearchWindowController =>
            Ioc.Default.GetRequiredService<ISearchWindowController>();

        private static SearchCommands Commands => Ioc.Default.GetRequiredService<SearchCommands>();

        public SearchResultsView()
        {
            InitializeComponent();

            _session.PropertyChanged += OnSessionPropertyChanged;
            _session.ResultsReset += OnResultsReset;
            SearchResultsListView.PreviewKeyDown += OnKeyPressed;
            SearchResultsListView.SelectionChanged += OnListSelectionChanged;
            SearchResultsListView.PreviewMouseLeftButtonDown += OnPreviewLeftMouseButtonDown;

            _busyIndicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BusyIndicatorDelayMilliseconds),
            };
            _busyIndicatorTimer.Tick += BusyIndicatorTimerElapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _session.Start(SynchronizationContext.Current ?? new SynchronizationContext());

            _session.AutoSelect();
            AttachToScrollViewer();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SearchSession.Results):
                    SearchResultsListView.ItemsSource = _session.Results as System.Collections.IEnumerable;
                    break;
                case nameof(SearchSession.TotalCount):
                    TotalResultsCount = _session.TotalCount;
                    break;
                case nameof(SearchSession.IsBusy):
                    OnCollectionIsBusyChanged();
                    break;
                case nameof(SearchSession.SelectedIndex):
                    ApplySelectionFromSession();
                    break;
            }
        }

        private void ApplySelectionFromSession()
        {
            _syncingSelection = true;
            SearchResultsListView.SelectedIndex = _session.SelectedIndex;
            if (_session.SelectedIndex >= 0 && SearchResultsListView.SelectedItem != null)
                SearchResultsListView.ScrollIntoView(SearchResultsListView.SelectedItem);
            _syncingSelection = false;
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingSelection)
                return;

            _session.SelectedIndex = SearchResultsListView.SelectedIndex;
        }

        private void OnResultsReset()
        {
            Dispatcher.BeginInvoke(_session.AutoSelect);
        }

        private void OnCollectionIsBusyChanged()
        {
            if (_session.IsBusy)
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

            if (!_session.IsBusy)
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

            scrollViewer.ScrollChanged += (_, e) =>
                _session.VisiblePageCount = Math.Max(1, (int)e.ViewportHeight);

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
            _isScrollBarDragging = true;
            _session.IsAsync = false;
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
            if (_isScrollBarDragging)
            {
                _isScrollBarDragging = false;
                _session.IsAsync = true;
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
            // Prevents deselecting an item when Ctrl is held down and clicking on an already selected item
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
                Commands.PreviewSelected();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.C)
            {
                Commands.CopySelectedPath();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                Commands.CopySelected();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                SearchWindowController.Hide();
                e.Handled = true;
                return;
            }

            if (Commands.TranslateResultsGesture(e.Key, e.SystemKey, Keyboard.Modifiers, fromSearchBox: false))
                e.Handled = true;
        }

        private void OpenSelectedSearchResult()
        {
            if (SelectedItem == null)
                return;

            if (!_customActions.TryRun(SelectedItem))
                InvokeOnSelected(_actions.Open);

            SearchWindowController.Hide();
        }

        private void OpenFilePath(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.OpenPath);
            SearchWindowController.Hide();
        }

        private void InvokeOnSelected(Action<SearchResult> action)
        {
            if (SelectedItem is { } item)
                action(item);
        }

        private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.CopyPathToClipboard);
        }

        private void OpenWith(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.OpenWith);
            SearchWindowController.Hide();
        }

        private void ShowInEverything(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.ShowInEverything);
            SearchWindowController.Hide();
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.CopyToClipboard);
        }

        private void SingleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (!_settings.IsDoubleClickToOpen)
                OpenWithMouseClick();
        }

        private void DoubleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (_settings.IsDoubleClickToOpen)
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
                    InvokeOnSelected(_actions.ShowProperties);
                    SearchWindowController.Hide();
                    break;
                case ModifierKeys.Control:
                    InvokeOnSelected(_actions.OpenPath);
                    SearchWindowController.Hide();
                    break;
                case ModifierKeys.Shift:
                    InvokeOnSelected(_actions.ShowInEverything);
                    SearchWindowController.Hide();
                    break;
                default:
                    OpenSelectedSearchResult();
                    break;
            }
            SearchResultsListView.SelectedIndex = -1;
        }

        private void RunAsAdmin(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.RunAsAdmin);
            SearchWindowController.Hide();
        }

        private void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.ShowProperties);
            SearchWindowController.Hide();
        }

        private void ShowFileWindowsContextMenu(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_actions.ShowWindowsContextMenu);
        }

        private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            while (menuItem.Items.Count > 2)
                menuItem.Items.RemoveAt(0);

            List<Rule> actions = _customActions.Load();

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
                var actionIcon = CustomActionIcons.Load(action.Command);
                if (actionIcon != null)
                {
                    actionMenuItem.Icon = new Image { Width = 16, Height = 16, Source = actionIcon };
                }
                actionMenuItem.Click += OpenWithCustomAction;
                menuItem.Items.Insert(i, actionMenuItem);
            }
        }

        private void OpenWithCustomAction(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            var menuItem = sender as MenuItem;
            var command = menuItem?.Tag?.ToString() ?? "";
            _customActions.TryRun(SelectedItem, command);
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
            if (SelectedItem == null)
                return false;

            var diff = _dragStart - currentPosition;

            if (
                Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance
            )
                return false;

            string[] files = [SelectedItem.FullPathAndFileName];
            var data = new DataObject(DataFormats.FileDrop, files);
            data.SetData(DataFormats.Text, files[0]);
            DragDrop.DoDragDrop(SearchResultsListView, data, DragDropEffects.All);
            return true;
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (SelectedItem == null)
                return;

            if (_settings.IsSystemContextMenuDefault != (Keyboard.Modifiers == ModifierKeys.Shift))
            {
                _actions.ShowWindowsContextMenu(SelectedItem);
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