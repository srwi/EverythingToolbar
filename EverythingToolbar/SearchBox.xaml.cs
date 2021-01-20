using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
    public partial class SearchBox : TextBox
    {
        public SearchBox()
        {
            InitializeComponent();

            DataContext = EverythingSearch.Instance;
            InputMethod.SetPreferredImeState(this, InputMethodState.On);
        }

        private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            EverythingSearch.Instance.Reset();
        }

        private void OnMenuItemClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void OnPasteClicked(object sender, RoutedEventArgs args) { Paste(); }
        private void OnCopyClicked(object sender, RoutedEventArgs args) { Copy(); }
        private void OnCutClicked(object sender, RoutedEventArgs args) { Cut(); }
    }
}
