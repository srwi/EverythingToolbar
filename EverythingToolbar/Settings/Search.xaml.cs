using EverythingToolbar.Helpers;
using System.Windows;

namespace EverythingToolbar.Settings
{
    public partial class Search
    {
        public Search()
        {
            InitializeComponent();
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            HistoryManager.Instance.ClearHistory();
        }
    }
}