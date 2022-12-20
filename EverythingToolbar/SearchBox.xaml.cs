using EverythingToolbar.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SearchBox : TextBox
    {
        private string LastText = "";
        private static SearchResultsWindow window = new SearchResultsWindow();

        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.DoNotCare);

            Loaded += SearchBox_Loaded;
        }

        private void SearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            window.ShowActivated = false;
            window.Show();
            window.Hide();
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == null)
            {
                HistoryManager.Instance.AddToHistory(EverythingSearch.Instance.SearchTerm);
                EverythingSearch.Instance.Reset();
            }

        }

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Text))
            {
                window.Show();
            }
            else
            {
                window.Hide();
            }

            if (LastText == "")
                CaretIndex = Text.Length;

            LastText = Text;
        }

        private void OnKeyReleased(object sender, KeyEventArgs e)
        {
            //if (!SearchResultsWindow.IsOpen)
            //    return;

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
