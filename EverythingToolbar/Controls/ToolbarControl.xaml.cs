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
            new PropertyMetadata(false, OnLayoutModeChanged)
        );

        public bool IsFixedLayout
        {
            get => (bool)GetValue(IsFixedLayoutProperty);
            set => SetValue(IsFixedLayoutProperty, value);
        }

        public static readonly DependencyProperty CollapseOnAutoHideProperty = DependencyProperty.Register(
            nameof(CollapseOnAutoHide),
            typeof(bool),
            typeof(ToolbarControl),
            new PropertyMetadata(false, OnLayoutModeChanged)
        );

        public bool CollapseOnAutoHide
        {
            get => (bool)GetValue(CollapseOnAutoHideProperty);
            set => SetValue(CollapseOnAutoHideProperty, value);
        }

        public static readonly DependencyProperty IsIconOnlyProperty = DependencyProperty.Register(
            nameof(IsIconOnly),
            typeof(bool),
            typeof(ToolbarControl),
            new PropertyMetadata(false)
        );

        public bool IsIconOnly
        {
            get => (bool)GetValue(IsIconOnlyProperty);
            private set => SetValue(IsIconOnlyProperty, value);
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

        private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToolbarControl control)
            {
                control.UpdateLayoutMode();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.WidthChanged)
            {
                UpdateLayoutMode();
            }
        }

        private void UpdateLayoutMode()
        {
            var grid = (Grid)Content;
            if (IsFixedLayout)
            {
                grid.Margin = new Thickness(4, 2, 4, 2);
                IsIconOnly = false;
                return;
            }

            grid.ClearValue(MarginProperty);

            if (CollapseOnAutoHide && NativeMethods.IsTaskbarAutoHiding())
            {
                IsIconOnly = true;
                return;
            }

            IsIconOnly = ActualWidth > 0 && ActualWidth < 70;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateLayoutMode();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutMode();

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
