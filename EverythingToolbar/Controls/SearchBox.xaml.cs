using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;

namespace EverythingToolbar.Controls
{
    public partial class SearchBox
    {
        public static readonly DependencyProperty SearchTermProperty = DependencyProperty.Register(
            nameof(SearchTerm),
            typeof(string),
            typeof(SearchBox),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSearchTermPropertyChanged
            )
        );

        public string SearchTerm
        {
            get => (string)GetValue(SearchTermProperty);
            set => SetValue(SearchTermProperty, value);
        }

        /// <summary>
        /// Determines when this SearchBox should respond to focus requests.
        /// When true, responds when IsIcon=true (icon/launcher mode - SearchWindow's search box).
        /// When false, responds when IsIcon=false (toolbar mode - ToolbarControl's search box).
        /// This allows having multiple visible SearchBoxes while only the appropriate one gets focus.
        /// </summary>
        public static readonly DependencyProperty RespondsInIconModeProperty = DependencyProperty.Register(
            nameof(RespondsInIconMode),
            typeof(bool),
            typeof(SearchBox),
            new PropertyMetadata(false)
        );

        public bool RespondsInIconMode
        {
            get => (bool)GetValue(RespondsInIconModeProperty);
            set => SetValue(RespondsInIconModeProperty, value);
        }

        private static void OnSearchTermPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchBox searchBox && e.NewValue is string newValue)
            {
                if (searchBox.TextBox.Text == newValue)
                    return;

                searchBox.TextBox.Text = newValue;
                searchBox.TextBox.CaretIndex = searchBox.TextBox.Text.Length;
            }
        }

        private bool _isInternalTextChange;

        public SearchBox()
        {
            InitializeComponent();

            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
            EventDispatcher.Instance.SearchBoxFocusRequested += OnFocusRequested;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalTextChange)
                return;

            if (ToolbarSettings.User.IsSearchAsYouType)
            {
                SearchTerm = TextBox.Text;
            }
        }

        private void OnFocusRequested(object? sender, EventArgs e)
        {
            // Only respond if visible AND this SearchBox matches the current mode
            // - RespondsInIconMode=true: responds when IsIcon=true (SearchWindow's search box)
            // - RespondsInIconMode=false: responds when IsIcon=false (toolbar search boxes)
            if (Visibility == Visibility.Visible && 
                RespondsInIconMode == TaskbarStateManager.Instance.IsIcon)
            {
                Focus();
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                UpdateSearchTerm(HistoryManager.Instance.GetPreviousItem());
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                UpdateSearchTerm(HistoryManager.Instance.GetNextItem());
                e.Handled = true;
            }
            else if (
                Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Enter
                && !ToolbarSettings.User.IsSearchAsYouType
            )
            {
                SearchTerm = TextBox.Text;
                e.Handled = true;
            }
            else if (
                e.Key is Key.Home or Key.End
                    && Keyboard.Modifiers != ModifierKeys.Shift
                    && ToolbarSettings.User.IsHomeEndNavigateResults
                || e.Key == Key.PageDown
                || e.Key == Key.PageUp
                || e.Key == Key.Up
                || e.Key == Key.Down
                || e.Key == Key.Escape
                || e.Key == Key.Enter
                || e.SystemKey == Key.Enter // When Alt is held
                || (
                    e.Key is >= Key.D0 and <= Key.D9 or Key.I or Key.B or Key.U or Key.R
                    && Keyboard.Modifiers == ModifierKeys.Control
                )
            )
            {
                EventDispatcher.Instance.InvokeGlobalKeyEvent(this, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                // The down stroke of the Tab key is not always consistent. Therefore it's handled by the up stroke event.
                e.Handled = true;
            }
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var offset = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
                SearchState.Instance.CycleFilters(offset);
                e.Handled = true;
            }
        }

        private void UpdateSearchTerm(string newSearchTerm)
        {
            _isInternalTextChange = true;
            TextBox.Text = newSearchTerm;
            TextBox.CaretIndex = TextBox.Text.Length;
            SearchTerm = newSearchTerm;
            _isInternalTextChange = false;
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsShowQuickToggles))
                UpdateQuickTogglesVisibility();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateQuickTogglesVisibility();
        }

        private void UpdateQuickTogglesVisibility()
        {
            if (ToolbarSettings.User.IsShowQuickToggles && ActualWidth > 200)
            {
                QuickToggleButtons.Visibility = Visibility.Visible;
                TextBox.Padding = new Thickness(37, 0, 130, 0);
            }
            else
            {
                QuickToggleButtons.Visibility = Visibility.Collapsed;
                TextBox.Padding = new Thickness(37, 0, 10, 0);
            }
        }

        public new void Focus()
        {
            if (PresentationSource.FromVisual(TextBox) is HwndSource hwnd)
            {
                NativeMethods.ForciblySetForegroundWindow(hwnd.Handle);
            }

            TextBox.Focus();
            Keyboard.Focus(TextBox);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox.SelectAll();
            EventDispatcher.Instance.InvokeSearchBoxFocusedNotification(this, EventArgs.Empty);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null) // New focus outside application
            {
                SearchWindow.Instance.Hide();
            }
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox { IsKeyboardFocusWithin: false } textBox)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args)
        {
            TextBox.Paste();
        }

        private void OnCopyClicked(object sender, RoutedEventArgs args)
        {
            TextBox.Copy();
        }

        private void OnCutClicked(object sender, RoutedEventArgs args)
        {
            TextBox.Cut();
        }
    }
}
