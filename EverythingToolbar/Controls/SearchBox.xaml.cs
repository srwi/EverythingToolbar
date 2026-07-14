using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
        private readonly SearchState _searchState = Ioc.Default.GetRequiredService<SearchState>();
        private readonly SearchOptions _searchOptions = Ioc.Default.GetRequiredService<SearchOptions>();
        public MatchOptions MatchOptions { get; } = Ioc.Default.GetRequiredService<MatchOptions>();

        private static ISearchWindowController SearchWindowController =>
            Ioc.Default.GetRequiredService<ISearchWindowController>();

        private static SearchCommands Commands => Ioc.Default.GetRequiredService<SearchCommands>();

        public SearchBox()
        {
            InitializeComponent();
            DataContext = this;

            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            _searchOptions.PropertyChanged += OnSettingsChanged;
            WeakReferenceMessenger.Default.Register<FocusSearchBoxRequest>(this, (_, _) => OnFocusRequested());
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalTextChange)
                return;

            if (_searchOptions.IsSearchAsYouType)
            {
                SearchTerm = TextBox.Text;
            }
        }

        private void OnFocusRequested()
        {
            if (Visibility == Visibility.Visible)
            {
                Focus();
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                UpdateSearchTerm(_searchState.GetPreviousSearchTerm());
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                UpdateSearchTerm(_searchState.GetNextSearchTerm());
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter && !_searchOptions.IsSearchAsYouType)
            {
                SearchTerm = TextBox.Text;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                SearchWindowController.Hide();
                e.Handled = true;
                return;
            }

            if (Commands.TranslateResultsGesture(e.Key, e.SystemKey, Keyboard.Modifiers, fromSearchBox: true))
                e.Handled = true;
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var offset = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
                _searchState.CycleFilters(offset);
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
            if (e.PropertyName == nameof(SearchOptions.IsShowQuickToggles))
                UpdateQuickTogglesVisibility();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateQuickTogglesVisibility();
        }

        private void UpdateQuickTogglesVisibility()
        {
            if (_searchOptions.IsShowQuickToggles && ActualWidth > 200)
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
            WeakReferenceMessenger.Default.Send(new SearchBoxFocusedNotification());
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null) // New focus outside application
            {
                SearchWindowController.Hide();
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