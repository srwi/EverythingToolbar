using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Controls
{
    public partial class SearchBox : UserControl
    {
        public event EventHandler<TextChangedEventArgs> TextChanged;

        private int LastCaretIndex = 0;

        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            // IsEnabled property of matchWholeWord button needs to be handled
            // in code because DataTriggers are not compatible with DynamicResources as MenuItem styles
            Settings.Default.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;

            EventDispatcher.Instance.SearchTermReplaced += (s, searchTerm) => { UpdateSearchTerm(searchTerm); };
            EventDispatcher.Instance.SearchBoxFocusRequested += OnFocusRequested;

            // Forward TextBox.TextChanged to SearchBox.TextChanged
            TextBox.TextChanged += (s, e) => TextChanged?.Invoke(s, e);
        }

        private void OnFocusRequested(object sender, EventArgs e)
        {
            // Only visible SearchBoxes should respond to focus requests
            if (Visibility == Visibility.Visible)
                Focus();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
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
            else if ((e.Key == Key.Home || e.Key == Key.End) && Keyboard.Modifiers != ModifierKeys.Shift && Settings.Default.isAutoSelectFirstResult ||
                e.Key == Key.PageDown || e.Key == Key.PageUp ||
                e.Key == Key.Up || e.Key == Key.Down ||
                e.Key == Key.Escape || e.Key == Key.Enter ||
                (e.Key >= Key.D0 && e.Key <= Key.D9 && Keyboard.Modifiers == ModifierKeys.Control))
            {
                EventDispatcher.Instance.InvokeGlobalKeyEvent(this, e);
                e.Handled = true;
            }
        }

        private void UpdateSearchTerm(string newSearchTerm)
        {
            TextBox.Text = newSearchTerm;
            TextBox.CaretIndex = TextBox.Text.Length;
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isShowQuickToggles")
                UpdateQuickTogglesVisibility();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateQuickTogglesVisibility();
        }

        private void UpdateQuickTogglesVisibility()
        {
            if (Settings.Default.isShowQuickToggles && ActualWidth > 200)
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
            NativeMethods.SetForegroundWindow(((HwndSource)PresentationSource.FromVisual(this)).Handle);
            TextBox.Focus();
            Keyboard.Focus(TextBox);
        }

        public void RestoreCaretIndex()
        {
            TextBox.CaretIndex = Math.Min(LastCaretIndex, TextBox.Text.Length);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox.SelectAll();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            LastCaretIndex = TextBox.CaretIndex;

            if (e.NewFocus == null)  // New focus outside application
            {
                SearchWindow.Instance.Hide();
            }
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = (sender as TextBox);
            if (textBox != null)
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    textBox.Focus();
                }
            }
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { TextBox.Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { TextBox.Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { TextBox.Cut(); }
    }
}
