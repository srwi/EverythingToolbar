using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;
using EverythingToolbar.ViewModels;
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

        public SearchResult? SelectedSearchResult
        {
            get => (SearchResult?)GetValue(SelectedSearchResultProperty);
            set => SetValue(SelectedSearchResultProperty, value);
        }

        private SearchResult? SelectedItem => SelectedSearchResult;
        private Point _dragStart;
        private int _lastScrolledIndex = -1;
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

            SearchResultsListView.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));
            SearchResultsListView.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(OnScrollBarDragStarted));
            SearchResultsListView.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnScrollBarDragCompleted));

            _busyIndicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(BusyIndicatorDelayMilliseconds),
            };
            _busyIndicatorTimer.Tick += BusyIndicatorTimerElapsed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Session.Start(SynchronizationContext.Current ?? new SynchronizationContext());

            _viewModel.Session.AutoSelect();
        }

        private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchSession.IsBusy))
                OnCollectionIsBusyChanged();
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = SearchResultsListView.SelectedIndex;
            if (selectedIndex < 0 || SearchResultsListView.SelectedItem == null)
                return;

            if (selectedIndex == _lastScrolledIndex)
                return;

            _lastScrolledIndex = selectedIndex;
            SearchResultsListView.ScrollIntoView(SearchResultsListView.SelectedItem);
        }

        private void OnResultsReset()
        {
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
                _viewModel.Commands.PreviewSelected();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.C)
            {
                _viewModel.Commands.CopySelectedPath();
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                _viewModel.Commands.CopySelected();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                _viewModel.SearchWindowController.Hide();
                e.Handled = true;
                return;
            }

            if (_viewModel.Commands.TranslateResultsGesture(e.Key, e.SystemKey, Keyboard.Modifiers, fromSearchBox: false))
                e.Handled = true;
        }

        private void OpenSelectedSearchResult()
        {
            if (SelectedItem == null)
                return;

            if (!_viewModel.CustomActions.TryRun(SelectedItem))
                InvokeOnSelected(_viewModel.Actions.Open);

            _viewModel.SearchWindowController.Hide();
        }

        private void OpenFilePath(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.OpenPath);
            _viewModel.SearchWindowController.Hide();
        }

        private void InvokeOnSelected(Action<SearchResult> action)
        {
            if (SelectedItem is { } item)
                action(item);
        }

        private void CopyPathToClipBoard(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.CopyPathToClipboard);
        }

        private void OpenWith(object sender, RoutedEventArgs e)
        {
            _viewModel.Commands.OpenSelectedWith();
        }

        private void ShowInEverything(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.ShowInEverything);
            _viewModel.SearchWindowController.Hide();
        }

        private void CopyFile(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.CopyToClipboard);
        }

        private void SingleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (!_viewModel.Settings.IsDoubleClickToOpen)
                OpenWithMouseClick();
        }

        private void DoubleClickSearchResult(object sender, MouseEventArgs e)
        {
            if (_viewModel.Settings.IsDoubleClickToOpen)
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
                    InvokeOnSelected(_viewModel.Actions.ShowProperties);
                    _viewModel.SearchWindowController.Hide();
                    break;
                case ModifierKeys.Control:
                    InvokeOnSelected(_viewModel.Actions.OpenPath);
                    _viewModel.SearchWindowController.Hide();
                    break;
                case ModifierKeys.Shift:
                    InvokeOnSelected(_viewModel.Actions.ShowInEverything);
                    _viewModel.SearchWindowController.Hide();
                    break;
                default:
                    OpenSelectedSearchResult();
                    break;
            }
            SearchResultsListView.SelectedIndex = -1;
        }

        private void RunAsAdmin(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.RunAsAdmin);
            _viewModel.SearchWindowController.Hide();
        }

        private void ShowFileProperties(object sender, RoutedEventArgs e)
        {
            InvokeOnSelected(_viewModel.Actions.ShowProperties);
            _viewModel.SearchWindowController.Hide();
        }

        private void ShowFileWindowsContextMenu(object sender, RoutedEventArgs e)
        {
            _viewModel.Commands.ShowSelectedWindowsContextMenu();
        }

        private void OnOpenWithMenuLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            while (menuItem.Items.Count > 2)
                menuItem.Items.RemoveAt(0);

            List<Rule> actions = _viewModel.CustomActions.Load();

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
            _viewModel.CustomActions.TryRun(SelectedItem, command);
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

            if (_viewModel.Settings.IsSystemContextMenuDefault != (Keyboard.Modifiers == ModifierKeys.Shift))
            {
                _viewModel.Commands.ShowSelectedWindowsContextMenu();
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
    }
}
