using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Core.Search;

namespace EverythingToolbar.App.Search
{
    public sealed partial class SearchState : ObservableObject
    {
        [ObservableProperty]
        private string _searchTerm = "";

        public SortBy SortBy => (SortBy)_settings.SortBy;
        public bool IsSortDescending => _settings.IsSortDescending;
        public bool IsMatchCase => _settings.IsMatchCase;
        public bool IsMatchPath => _settings.IsMatchPath;
        public bool IsMatchWholeWord => _settings.IsMatchWholeWord;
        public bool IsRegExEnabled => _settings.IsRegExEnabled;

        private Filter _currentFilter;
        public Filter Filter
        {
            get => _currentFilter;
            set
            {
                if (SetProperty(ref _currentFilter, value))
                {
                    _settings.LastFilter = value.Name;
                }
            }
        }

        private readonly SearchHistoryService _history;
        private readonly FilterService _filterService;
        private readonly ISettings _settings;

        public SearchState(SearchHistoryService history, FilterService filterService, ISettings settings)
        {
            _history = history;
            _filterService = filterService;
            _settings = settings;

            _currentFilter = _filterService.GetInitialFilter();

            _settings.PropertyChanged += OnSettingsChanged;
        }

        public void Reset()
        {
            if (_settings.IsEnableHistory)
                _history.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            Filter = _filterService.GetInitialFilter();
        }

        public string GetPreviousSearchTerm() => _history.GetPreviousItem();

        public string GetNextSearchTerm() => _history.GetNextItem();

        public void ClearHistory() => _history.ClearHistory();

        public void CycleFilters(int offset = 1)
        {
            var filterCount = _filterService.Filters.Count;
            var currentIndex = _filterService.Filters.IndexOf(Filter);
            var newIndex = (currentIndex + offset + filterCount) % filterCount;
            Filter = _filterService.Filters[newIndex];
        }

        public void SelectFilterFromIndex(int index)
        {
            if (index < 0 || index >= _filterService.Filters.Count)
                return;

            Filter = _filterService.Filters[index];
        }

        private string ApplyMacros(string searchTerm)
        {
            var result = searchTerm;

            foreach (var f in _filterService.Filters)
            {
                if (string.IsNullOrEmpty(f.Macro))
                    continue;

                result = result.Replace(f.Macro + ":", f.Search + " ");
            }

            var defaultMacros = new Dictionary<string, string>
            {
                // Macros quot:, gt: and lt: are not supported by the SDK
                { "apos:", "'" },
                { "amp:", "&" },
            };
            foreach (var defaultMacro in defaultMacros)
            {
                result = result.Replace(defaultMacro.Key, defaultMacro.Value);
            }

            return result;
        }

        public string BuildSearchTerm()
        {
            var rawSearchTerm = Filter.GetSearchPrefix(IsMatchCase, IsMatchWholeWord, IsMatchPath, IsRegExEnabled) + SearchTerm;
            var searchTermWithAppliedMacros = ApplyMacros(rawSearchTerm);
            return searchTermWithAppliedMacros;
        }

        public SearchQuery BuildSearchQuery()
        {
            return new SearchQuery(
                BuildSearchTerm(),
                SortBy,
                IsSortDescending,
                IsMatchCase,
                IsMatchPath,
                IsMatchWholeWord,
                IsRegExEnabled
            );
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISettings.IsMatchCase):
                    OnPropertyChanged(nameof(IsMatchCase));
                    break;
                case nameof(ISettings.IsMatchPath):
                    OnPropertyChanged(nameof(IsMatchPath));
                    break;
                case nameof(ISettings.IsMatchWholeWord):
                    OnPropertyChanged(nameof(IsMatchWholeWord));
                    break;
                case nameof(ISettings.IsRegExEnabled):
                    OnPropertyChanged(nameof(IsRegExEnabled));
                    break;
                case nameof(ISettings.SortBy):
                    OnPropertyChanged(nameof(SortBy));
                    break;
                case nameof(ISettings.IsSortDescending):
                    OnPropertyChanged(nameof(IsSortDescending));
                    break;
                case nameof(ISettings.IsHideEmptySearchResults):
                    SearchTerm = "";
                    OnPropertyChanged(nameof(SearchTerm));
                    break;
            }
        }

    }
}