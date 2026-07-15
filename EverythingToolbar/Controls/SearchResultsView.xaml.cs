using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.ViewModels;

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

        public SearchResult? SelectedSearchResult
        {
            get => (SearchResult?)GetValue(SelectedSearchResultProperty);
            set => SetValue(SelectedSearchResultProperty, value);
        }

        private Point _dragStart;
        private int _lastScrolledIndex = -1;
        private ScrollViewer? _scrollViewer;
        private Action? _focusSelectedItem;
        private int? _touchId;
        private readonly DispatcherTimer _busyIndicatorTimer;
        private const int BusyIndicatorDelayMilliseconds = 2000;
        private readonly SearchResultsViewModel _viewModel = Ioc.Default.GetRequiredService<SearchResultsViewModel>();

        public SearchResultsView()
        {
            InitializeComponent();

            _viewModel.Session.PropertyChanged += OnSessionPropertyChanged;
            _viewModel.Session.ResultsReset += OnResultsReset;
            SearchResultsListView.PreviewKeyDown += OnKeyPressed;
            SearchResultsListView.SelectionChanged += OnListSelectionChanged;
            SearchResultsListView.PreviewMouseLeftButtonDown += OnPreviewLeftMouseButtonDown;

            SearchResultsListView.AddHandler(
                ScrollViewer.ScrollChangedEvent,
                new ScrollChangedEventHandler(OnScrollChanged)
            );
            SearchResultsListView.AddHandler(
                Thumb.DragStartedEvent,
                new DragStartedEventHandler(OnScrollBarDragStarted)
            );
            SearchResultsListView.AddHandler(
                Thumb.DragCompletedEvent,
                new DragCompletedEventHandler(OnScrollBarDragCompleted)
            );

            _busyIndicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BusyIndicatorDelayMilliseconds),
            };
            _busyIndicatorTimer.Tick += BusyIndicatorTimerElapsed;

            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Session.Start();

            // Let keyboard navigation move focus onto the selected item (see SearchCommands.SyncFocusToSelection).
            _focusSelectedItem ??= FocusSelectedItem;
            _viewModel.RegisterResultsList(_focusSelectedItem);

            _viewModel.Session.AutoSelect();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_focusSelectedItem != null)
                _viewModel.UnregisterResultsList(_focusSelectedItem);
        }

        private void FocusSelectedItem()
        {
            // Defer past the pending scroll/layout so the container exists even after a jump (virtualization).
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    var index = SearchResultsListView.SelectedIndex;
                    if (
                        index >= 0
                        && SearchResultsListView.ItemContainerGenerator.ContainerFromIndex(index)
                            is ListViewItem container
                    )
                        Keyboard.Focus(container);
                }),
                DispatcherPriority.Input
            );
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchSession.IsBusy))
                OnCollectionIsBusyChanged();
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = SearchResultsListView.SelectedIndex;
            if (selectedIndex < 0)
                return;

            if (selectedIndex == _lastScrolledIndex)
                return;

            _lastScrolledIndex = selectedIndex;

            ScrollIndexIntoView(selectedIndex);
        }

        private void ScrollIndexIntoView(int index)
        {
            var scrollViewer = GetListScrollViewer();
            if (scrollViewer == null)
            {
                // Template not realized yet; try item-based scrolling instead
                if (SearchResultsListView.SelectedItem != null)
                    SearchResultsListView.ScrollIntoView(SearchResultsListView.SelectedItem);
                return;
            }

            var viewportItems = scrollViewer.ViewportHeight;
            var topItem = scrollViewer.VerticalOffset;

            if (index < topItem)
                scrollViewer.ScrollToVerticalOffset(index);
            else if (index >= topItem + viewportItems)
                scrollViewer.ScrollToVerticalOffset(index - viewportItems + 1);
        }

        private ScrollViewer? GetListScrollViewer()
        {
            if (_scrollViewer != null)
                return _scrollViewer;

            _scrollViewer = FindVisualChild<ScrollViewer>(SearchResultsListView);
            return _scrollViewer;
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    return typed;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private void OnResultsReset()
        {
            _lastScrolledIndex = -1;
            Dispatcher.BeginInvoke(_viewModel.Session.AutoSelect);
        }

        private void OnCollectionIsBusyChanged()
        {
            if (_viewModel.Session.IsBusy)
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

            if (!_viewModel.Session.IsBusy)
                return;

            SpinnerOverlay.Visibility = Visibility.Visible;
            SearchResultsListView.Opacity = 0.3;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _viewModel.Session.VisiblePageCount = Math.Max(1, (int)e.ViewportHeight);
        }

        private void OnScrollBarDragStarted(object sender, DragStartedEventArgs e)
        {
            _viewModel.Session.IsAsync = false;
        }

        private void OnScrollBarDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _viewModel.Session.IsAsync = true;
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
                _viewModel.PreviewSelected();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.C)
            {
                _viewModel.CopySelectedPath();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                _viewModel.CopySelected();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                _viewModel.HideWindow();
                e.Handled = true;
                return;
            }

            if (_viewModel.TryHandleResultsGesture(e.Key, e.SystemKey, Keyboard.Modifiers))
                e.Handled = true;
        }

        private void OpenFilePath(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSelectedPath();
        }

        private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
        {
            _viewModel.CopySelectedPath();
        }

        private void OpenWith(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSelectedWith();
        }

        private void ShowInEverything(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSelectedInEverything();
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            _viewModel.CopySelected();
        }

        private void SingleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (!_viewModel.IsDoubleClickToOpen)
                OpenWithMouseClick();
        }

        private void DoubleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (_viewModel.IsDoubleClickToOpen)
                OpenWithMouseClick();
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            _viewModel.OpenSelected();
        }

        private void OpenWithMouseClick()
        {
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Alt:
                    _viewModel.ShowSelectedProperties();
                    break;
                case ModifierKeys.Control:
                    _viewModel.OpenSelectedPath();
                    break;
                case ModifierKeys.Shift:
                    _viewModel.ShowSelectedInEverything();
                    break;
                default:
                    _viewModel.OpenSelected();
                    break;
            }
            SearchResultsListView.SelectedIndex = -1;
        }

        private void RunAsAdmin(object sender, RoutedEventArgs e)
        {
            _viewModel.RunSelectedAsAdmin();
        }

        private void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSelectedProperties();
        }

        private void ShowFileWindowsContextMenu(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowSelectedWindowsContextMenu();
        }

        private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            // Remove previously injected custom-action items (everything before the static separator),
            // rather than assuming a fixed count of trailing static entries.
            while (menuItem.Items.Count > 0 && menuItem.Items[0] is not Separator)
                menuItem.Items.RemoveAt(0);

            List<Rule> actions = _viewModel.LoadCustomActions();

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
                    actionMenuItem.Icon = new Image
                    {
                        Width = 16,
                        Height = 16,
                        Source = actionIcon,
                    };
                }
                actionMenuItem.Click += OpenWithCustomAction;
                menuItem.Items.Insert(i, actionMenuItem);
            }
        }

        private void OpenWithCustomAction(object sender, RoutedEventArgs e)
        {
            if (SelectedSearchResult == null)
                return;

            var menuItem = sender as MenuItem;
            var command = menuItem?.Tag?.ToString() ?? "";
            _viewModel.TryRunCustomAction(SelectedSearchResult, command);
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
            if (SelectedSearchResult == null)
                return false;

            var diff = _dragStart - currentPosition;

            if (
                Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance
            )
                return false;

            string[] files = [SelectedSearchResult.FullPathAndFileName];
            var data = new DataObject(DataFormats.FileDrop, files);
            data.SetData(DataFormats.Text, files[0]);
            DragDrop.DoDragDrop(SearchResultsListView, data, DragDropEffects.All);
            return true;
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (SelectedSearchResult == null)
                return;

            if (_viewModel.IsSystemContextMenuDefault != (Keyboard.Modifiers == ModifierKeys.Shift))
            {
                _viewModel.ShowSelectedWindowsContextMenu();
                e.Handled = true;
            }
        }

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (SelectedSearchResult == null)
                return;

            var runAsAdminItem = (sender as ContextMenu)
                ?.Items.OfType<MenuItem>()
                .FirstOrDefault(mi => mi.Name == "OpenAsAdminMenuItem");
            if (runAsAdminItem == null)
                return;

            string[] extensions = [".exe", ".bat", ".cmd"];
            var isExecutable =
                SelectedSearchResult.IsFile
                && extensions.Any(ext =>
                    SelectedSearchResult.FullPathAndFileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
                );

            runAsAdminItem.Visibility = isExecutable ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
