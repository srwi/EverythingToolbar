using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Data;
using EverythingToolbar.Properties;
using EverythingToolbar.Search;

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
            Ioc.Default.GetRequiredService<SearchState>().ClearHistory();
        }
    }

    public class SearchViewModel
    {
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

        public List<KeyValuePair<string, FocusBehavior>> FocusBehaviorItems { get; } =
            [
                new(Resources.FocusBehaviorClamp, FocusBehavior.Clamp),
                new(Resources.FocusBehaviorRepeat, FocusBehavior.Repeat),
                new(Resources.FocusBehaviorRepeatWithSearch, FocusBehavior.RepeatWithSearch),
            ];
    }
}
