using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EverythingToolbar.Data;

namespace EverythingToolbar.Helpers
{
    public class FilterLoader : INotifyPropertyChanged
    {
        private readonly DefaultFilterLoader _defaultLoader;
        private readonly EverythingFilterLoader _everythingLoader;
        private readonly ISettings _settings;

        public ObservableCollection<Filter> Filters
        {
            get
            {
                if (_settings.IsRegExEnabled)
                    return new ObservableCollection<Filter>([_defaultLoader.AllFilter]);

                if (_settings.IsImportFilters)
                {
                    var everythingFilters = _everythingLoader.Filters;

                    if (everythingFilters?.Count > 0)
                        return everythingFilters;

                    return new ObservableCollection<Filter>([_defaultLoader.AllFilter]);
                }

                return _defaultLoader.Filters;
            }
        }

        public FilterLoader(DefaultFilterLoader defaultLoader, EverythingFilterLoader everythingLoader,
            ISettings settings)
        {
            _defaultLoader = defaultLoader;
            _everythingLoader = everythingLoader;
            _settings = settings;

            _settings.PropertyChanged += OnSettingsChanged;
            _everythingLoader.PropertyChanged += OnEverythingFiltersChanged;
            _defaultLoader.PropertyChanged += OnDefaultFiltersChanged;
        }

        private void OnDefaultFiltersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DefaultFilterLoader.Filters))
            {
                NotifyPropertyChanged(nameof(Filters));
            }
        }

        private void OnEverythingFiltersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EverythingFilterLoader.Filters))
            {
                NotifyPropertyChanged(nameof(Filters));
            }
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISettings.IsRegExEnabled):
                case nameof(ISettings.IsImportFilters):
                    NotifyPropertyChanged(nameof(Filters));
                    break;
            }
        }

        public Filter GetInitialFilter()
        {
            if (_settings.IsRememberFilter)
            {
                foreach (var filter in Filters)
                {
                    if (filter.Name == _settings.LastFilter)
                        return filter;
                }
            }

            return Filters[0];
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}