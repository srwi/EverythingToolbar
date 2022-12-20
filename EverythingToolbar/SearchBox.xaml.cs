using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SearchBox : TextBox
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
            SelectAll();
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

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Text))
                EventDispatcher.Instance.InvokeShowWindow();

            if (LastText == "")
                CaretIndex = Text.Length;

            LastText = Text;
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                    EverythingSearch.Instance.CycleFilters(-1);
                else
                    EverythingSearch.Instance.CycleFilters(1);
            }
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { Cut(); }
    }
}
