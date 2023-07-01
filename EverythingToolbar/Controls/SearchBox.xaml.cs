using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Controls
{
    public partial class SearchBox : UserControl
    {
        public event EventHandler<TextChangedEventArgs> TextChanged;

        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            // IsEnabled property of matchWholeWord button needs to be handled
            // in code because DataTriggers are not compatible with DynamicResources as MenuItem styles
            Settings.Default.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;

            // Forward TextBox.TextChanged to SearchBox.TextChanged
            TextBox.TextChanged += (s, e) => TextChanged?.Invoke(s, e);

            EventDispatcher.Instance.SearchTermReplaced += OnSearchTermReplaced;
        }

        private void OnSearchTermReplaced(object sender, string newSearchTerm)
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
            TextBox.Focus();
            Keyboard.Focus(TextBox);
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox.SelectAll();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
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
