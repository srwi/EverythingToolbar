using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Converters;
using EverythingToolbar.ViewModels;

namespace EverythingToolbar.Controls
{
    public partial class SearchBox
    {
        public static readonly DependencyProperty CornerRadiusRatioProperty = DependencyProperty.RegisterAttached(
            "CornerRadiusRatio",
            typeof(double),
            typeof(SearchBox),
            new PropertyMetadata(0.0, OnCornerRadiusRatioChanged)
        );

        public static double GetCornerRadiusRatio(DependencyObject element) =>
            (double)element.GetValue(CornerRadiusRatioProperty);

        public static void SetCornerRadiusRatio(DependencyObject element, double value) =>
            element.SetValue(CornerRadiusRatioProperty, value);

        private static void OnCornerRadiusRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border border)
                return;

            if (e.NewValue is double ratio && ratio > 0)
            {
                border.SetBinding(
                    Border.CornerRadiusProperty,
                    new Binding(nameof(Border.ActualHeight))
                    {
                        Source = border,
                        Converter = CornerRadiusFromHeightConverter.Instance,
                        ConverterParameter = ratio,
                    }
                );
            }
            else
            {
                BindingOperations.ClearBinding(border, Border.CornerRadiusProperty);
                border.SetResourceReference(Border.CornerRadiusProperty, "TextBoxCornerRadius");
            }
        }

        public static readonly DependencyProperty LeadingContentProperty = DependencyProperty.RegisterAttached(
            "LeadingContent",
            typeof(object),
            typeof(SearchBox),
            new PropertyMetadata(null)
        );

        public static object? GetLeadingContent(DependencyObject element) => element.GetValue(LeadingContentProperty);

        public static void SetLeadingContent(DependencyObject element, object? value) =>
            element.SetValue(LeadingContentProperty, value);

        public static readonly DependencyProperty TrailingContentProperty = DependencyProperty.RegisterAttached(
            "TrailingContent",
            typeof(object),
            typeof(SearchBox),
            new PropertyMetadata(null)
        );

        public static object? GetTrailingContent(DependencyObject element) => element.GetValue(TrailingContentProperty);

        public static void SetTrailingContent(DependencyObject element, object? value) =>
            element.SetValue(TrailingContentProperty, value);

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

                searchBox._isInternalTextChange = true;
                try
                {
                    searchBox.TextBox.Text = newValue;
                    searchBox.TextBox.CaretIndex = searchBox.TextBox.Text.Length;
                }
                finally
                {
                    searchBox._isInternalTextChange = false;
                }
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
            if (
                Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Enter
                && !_viewModel.Settings.IsSearchAsYouType
                && SearchTerm != TextBox.Text
            )
            {
                SearchTerm = TextBox.Text;
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
            QuickToggleButtons.Visibility =
                _viewModel.Settings.IsShowQuickToggles && ActualWidth > 200 ? Visibility.Visible : Visibility.Collapsed;
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
