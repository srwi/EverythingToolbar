using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.ViewModels;

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
        private readonly SearchBoxViewModel _viewModel = Ioc.Default.GetRequiredService<SearchBoxViewModel>();

        public SearchBox()
        {
            InitializeComponent();
            DataContext = _viewModel;

            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            _viewModel.Settings.PropertyChanged += OnSettingsChanged;
            WeakReferenceMessenger.Default.Register<FocusSearchBoxRequest>(this, (_, _) => OnFocusRequested());
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInternalTextChange)
                return;

            if (_viewModel.Settings.IsSearchAsYouType)
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
                UpdateSearchTerm(_viewModel.PreviousHistoryTerm());
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                UpdateSearchTerm(_viewModel.NextHistoryTerm());
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter && !_viewModel.Settings.IsSearchAsYouType)
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
                Keyboard.ClearFocus();
                _viewModel.Dismiss();
                e.Handled = true;
                return;
            }

            if (_viewModel.TryHandleResultsGesture(e.Key, e.SystemKey, Keyboard.Modifiers))
                e.Handled = true;
        }

        private void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var offset = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1;
                _viewModel.CycleFilters(offset);
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
            if (e.PropertyName == nameof(ISettings.IsShowQuickToggles))
                UpdateQuickTogglesVisibility();
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateQuickTogglesVisibility();
        }

        private void UpdateQuickTogglesVisibility()
        {
            if (_viewModel.Settings.IsShowQuickToggles && ActualWidth > 200)
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
            _viewModel.NotifySearchBoxFocused();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null) // New focus outside application
            {
                _viewModel.NotifyFocusLostToOutside();
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
