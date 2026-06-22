using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;

namespace EverythingToolbar.Controls
{
    public partial class ToolbarControl
    {
        // Dependency property to control layout mode
        public static readonly DependencyProperty IsFixedLayoutProperty = 
            DependencyProperty.Register(nameof(IsFixedLayout), typeof(bool), typeof(ToolbarControl),
                new PropertyMetadata(false, OnIsFixedLayoutChanged));

        public bool IsFixedLayout
        {
            get => (bool)GetValue(IsFixedLayoutProperty);
            set => SetValue(IsFixedLayoutProperty, value);
        }

        // Dependency property to control whether to add SearchWindowPlacement behavior
        public static readonly DependencyProperty AddPlacementBehaviorProperty = 
            DependencyProperty.Register(nameof(AddPlacementBehavior), typeof(bool), typeof(ToolbarControl),
                new PropertyMetadata(false));

        public bool AddPlacementBehavior
        {
            get => (bool)GetValue(AddPlacementBehaviorProperty);
            set => SetValue(AddPlacementBehaviorProperty, value);
        }

        public ToolbarControl()
        {
            InitializeComponent();

            SearchWindow.Instance.Hiding += OnSearchWindowHiding;
            ShortcutManager.Initialize(FocusSearchBox);

            StartMenuIntegration.Instance.Initialize();

            // Set TaskbarStateManager to indicate we're not in icon mode
            // (this hides the SearchBox in SearchWindow)
            TaskbarStateManager.Instance.IsIcon = false;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Add SearchWindowPlacement behavior if requested
            if (AddPlacementBehavior)
            {
                var behavior = new SearchWindowPlacement { PlacementTarget = this };
                Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);
            }
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
            if (IsFixedLayout)
            {
                // Fixed layout mode - show search icon, remove responsive visibility
                SearchIcon.Visibility = Visibility.Visible;
                SearchBox.ClearValue(VisibilityProperty);
                SearchBox.Visibility = Visibility.Visible;
                SearchBox.MinWidth = 200;
                SearchBox.MaxWidth = 400;
                SearchButton.Visibility = Visibility.Collapsed;
                
                // Use fixed layout grid style
                var grid = (Grid)Content;
                grid.Margin = new Thickness(4, 2, 4, 2);
            }
            else
            {
                // Responsive layout mode - hide search icon, use responsive visibility
                SearchIcon.Visibility = Visibility.Collapsed;
                SearchBox.ClearValue(MinWidthProperty);
                SearchBox.ClearValue(MaxWidthProperty);
                
                // Restore responsive bindings
                var searchBoxBinding = new System.Windows.Data.Binding("ActualWidth")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
                    Converter = FindResource("SearchBoxDoubleToVisibilityConverter") as System.Windows.Data.IValueConverter
                };
                SearchBox.SetBinding(VisibilityProperty, searchBoxBinding);

                var searchButtonBinding = new System.Windows.Data.Binding("ActualWidth")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.Self),
                    Converter = FindResource("SearchButtonDoubleToVisibilityConverter") as System.Windows.Data.IValueConverter
                };
                SearchButton.SetBinding(VisibilityProperty, searchButtonBinding);
                
                // Use responsive layout grid style
                var grid = (Grid)Content;
                grid.Margin = new Thickness(0);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateLayoutMode();
        }

        private void OnSearchWindowHiding(object? sender, EventArgs e)
        {
            Keyboard.Focus(KeyboardFocusCapture);
        }

        private void OnSearchBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            EventDispatcher.Instance.InvokeUnfocusRequested(sender, e);

            if (e.NewFocus == null) // New focus outside application
            {
                // Focus an invisible text box to prevent Windows from randomly focusing the search box
                // and causing visual distraction
                Keyboard.Focus(KeyboardFocusCapture);
            }
        }

        private void OnSearchBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            SearchWindow.Instance.Show();
        }

        private void FocusSearchBox()
        {
            if (TaskbarStateManager.Instance.IsIcon)
            {
                SearchWindow.Instance.Toggle();
            }
            else if (SearchBox.IsKeyboardFocusWithin)
            {
                SearchWindow.Instance.Hide();
            }
            else
            {
                EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
            }
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
            EventDispatcher.Instance.InvokeFocusRequested(sender, e);
        }
    }
}
