using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class ToolbarControl
    {
        public static readonly DependencyProperty IsFixedLayoutProperty = DependencyProperty.Register(
            nameof(IsFixedLayout),
            typeof(bool),
            typeof(ToolbarControl),
            new PropertyMetadata(false, OnIsFixedLayoutChanged)
        );

        public bool IsFixedLayout
        {
            get => (bool)GetValue(IsFixedLayoutProperty);
            set => SetValue(IsFixedLayoutProperty, value);
        }

        private readonly ToolbarControlViewModel _viewModel = Ioc.Default.GetRequiredService<ToolbarControlViewModel>();

        private Action? _searchBoxFocus;
        private Func<bool>? _searchBoxIsFocused;

        public ToolbarControl()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private static void OnIsFixedLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToolbarControl control)
            {
                control.UpdateLayoutMode();
            }
        }

        private void UpdateLayoutMode()
        {
            if (!IsFixedLayout)
                return;

            SearchBox.ClearValue(VisibilityProperty);
            SearchBox.Visibility = Visibility.Visible;
            SearchBox.MinWidth = 200;
            SearchBox.MaxWidth = 400;
            SearchButton.Visibility = Visibility.Collapsed;

            var grid = (Grid)Content;
            grid.Margin = new Thickness(4, 2, 4, 2);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateLayoutMode();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Hiding -= OnSearchWindowHiding;
            _viewModel.Hiding += OnSearchWindowHiding;

            _searchBoxFocus ??= SearchBox.Focus;
            _searchBoxIsFocused ??= () => SearchBox.IsKeyboardFocusWithin;
            _viewModel.RegisterSearchBox(_searchBoxIsFocused, _searchBoxFocus);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Hiding -= OnSearchWindowHiding;

            if (_searchBoxFocus != null)
                _viewModel.UnregisterSearchBox(_searchBoxFocus);
        }

        private void OnSearchWindowHiding(object? sender, EventArgs e)
        {
            Keyboard.Focus(KeyboardFocusCapture);
        }

        private void OnSearchBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _viewModel.NotifyToolbarFocusChanged(false);

            if (e.NewFocus == null) // New focus outside application
            {
                Keyboard.Focus(KeyboardFocusCapture);
            }
        }

        private void OnSearchBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            _viewModel.ShowSearchWindow();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent explorer crash when pressing Alt + F4
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.NotifyToolbarFocusChanged(true);
        }
    }
}
