using System.Collections.Generic;
using System.Windows;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Settings
{
    public partial class Search
    {
        public Search()
        {
            InitializeComponent();
            DataContext = new SearchViewModel();
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            HistoryManager.Instance.ClearHistory();
        }
    }

    public class SearchViewModel
    {
        public List<KeyValuePair<string, FocusBehavior>> FocusBehaviorItems { get; } =
        [
            new(Resources.FocusBehaviorClamp, FocusBehavior.Clamp),
            new(Resources.FocusBehaviorRepeat, FocusBehavior.Repeat),
            new(Resources.FocusBehaviorRepeatWithSearch, FocusBehavior.RepeatWithSearch),
        ];
    }
}
