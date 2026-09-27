using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Settings
{
    [ObservableObject]
    public partial class Search
    {
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();
        private readonly SearchState _searchState = Ioc.Default.GetRequiredService<SearchState>();
        private readonly EverythingClientRouter _everythingClient =
            Ioc.Default.GetRequiredService<EverythingClientRouter>();

        public bool IsResultOmissionsSupported => _everythingClient.IsPipeClientActive;

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
            Settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.IsForceLegacySdk))
            {
                OnPropertyChanged(nameof(IsResultOmissionsSupported));
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Settings.PropertyChanged -= OnSettingsChanged;
        }

        private void OnClearHistoryClicked(object sender, RoutedEventArgs e)
        {
            _searchState.ClearHistory();
        }
    }
}
