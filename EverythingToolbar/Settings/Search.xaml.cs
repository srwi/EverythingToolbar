using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Data;
using EverythingToolbar.Search;

namespace EverythingToolbar.Settings
{
    public partial class Search
    {
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

        public List<KeyValuePair<string, FocusBehavior>> FocusBehaviorItems { get; } =
            [
                new(Properties.Resources.FocusBehaviorClamp, FocusBehavior.Clamp),
                new(Properties.Resources.FocusBehaviorRepeat, FocusBehavior.Repeat),
                new(Properties.Resources.FocusBehaviorRepeatWithSearch, FocusBehavior.RepeatWithSearch),
            ];

        public Search()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            Ioc.Default.GetRequiredService<SearchState>().ClearHistory();
        }
    }
}
