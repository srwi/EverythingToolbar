using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            // IsEnabled property of matchWholeWord button needs to be handled
            // in code because DataTriggers are not compatible with DynamicResources as MenuItem styles
            Properties.Settings.Default.PropertyChanged += OnSettingsChanged;
            EverythingSearch.Instance.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled" || e.PropertyName == "CurrentFilter")
            {
                bool newEnabledState = !Properties.Settings.Default.isRegExEnabled && EverythingSearch.Instance.CurrentFilter.IsMatchWholeWord == null;
                IsMatchWholeWordMenuItem.IsEnabled = newEnabledState;
                IsMatchWholeWordButton.IsEnabled = newEnabledState;
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

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TextBox.Text))
                SearchWindow.Instance.Show();
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
