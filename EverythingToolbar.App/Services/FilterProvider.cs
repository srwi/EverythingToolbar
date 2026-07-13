using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.App.Services
{
    public class FilterProvider : ObservableObject
    {
        private readonly DefaultFilterProvider _defaultLoader;
        private readonly EverythingFilterProvider _everythingLoader;
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

        public ObservableCollection<Filter> VisibleFilters => new(Filters.Take(_settings.MaxTabItems));

        public ObservableCollection<Filter> OverflowFilters => new(Filters.Skip(_settings.MaxTabItems));

        public FilterProvider(DefaultFilterProvider defaultLoader, EverythingFilterProvider everythingLoader,
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
            if (e.PropertyName == nameof(DefaultFilterProvider.Filters))
            {
                NotifyFilterCollectionsChanged();
            }
        }

        private void OnEverythingFiltersChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EverythingFilterProvider.Filters))
            {
                NotifyFilterCollectionsChanged();
            }
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISettings.IsRegExEnabled):
                case nameof(ISettings.IsImportFilters):
                case nameof(ISettings.MaxTabItems):
                    NotifyFilterCollectionsChanged();
                    break;
            }
        }

        private void NotifyFilterCollectionsChanged()
        {
            OnPropertyChanged(nameof(Filters));
            OnPropertyChanged(nameof(VisibleFilters));
            OnPropertyChanged(nameof(OverflowFilters));
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
    }
}