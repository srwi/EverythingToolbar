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
            HistoryManager.Instance.AddToHistory(EverythingSearch.Instance.SearchTerm);
            EverythingSearch.Instance.Reset();
        }

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (LastText == "")
                CaretIndex = Text.Length;

            LastText = Text;
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { Cut(); }
    }
}
