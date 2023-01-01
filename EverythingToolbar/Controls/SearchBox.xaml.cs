using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SearchBox : UserControl
    {
        private string LastText = "";

        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //EventDispatcher.Instance.InvokeHideWindow();
        }

        private void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox.SelectAll();
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

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TextBox.Text))
                EventDispatcher.Instance.InvokeShowWindow();

            if (LastText == "")
                TextBox.CaretIndex = TextBox.Text.Length;

            LastText = TextBox.Text;
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { TextBox.Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { TextBox.Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { TextBox.Cut(); }
    }
}
