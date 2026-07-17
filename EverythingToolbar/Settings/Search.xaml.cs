using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Settings
{
    public partial class Search
    {
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();
        private readonly SearchState _searchState = Ioc.Default.GetRequiredService<SearchState>();

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
            _searchState.ClearHistory();
        }
    }
}
