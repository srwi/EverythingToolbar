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

        private bool _placementBehaviorAdded;

        public ToolbarControl()
        {
            InitializeComponent();

            // Global initialization (hotkey handler, start menu integration, icon-mode state)
            // is owned by the host (deskband/launcher), not by this passive control.
            Loaded += OnLoaded;
            Loaded += (_, _) => SearchWindow.Instance.Hiding += OnSearchWindowHiding;
            Unloaded += (_, _) => SearchWindow.Instance.Hiding -= OnSearchWindowHiding;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Add SearchWindowPlacement behavior if requested (only once, even if Loaded refires)
            if (AddPlacementBehavior && !_placementBehaviorAdded)
            {
                var behavior = new SearchWindowPlacement { PlacementTarget = this };
                Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);
                _placementBehaviorAdded = true;
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
            // Only fixed-layout hosts (taskbar window) need code-driven overrides. Responsive
            // hosts (deskband) already get their visibility bindings and zero margin from XAML,
            // and IsFixedLayout is set once from XAML and never toggled at runtime.
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

        public void FocusSearchBox()
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
