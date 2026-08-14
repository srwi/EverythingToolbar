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
        public SortBy EffectiveSortBy => _useSettingsSortKey ? SortBy : Filter.SortBy ?? SortBy;
        public bool IsSortDescending => _settings.IsSortDescending;
        public bool EffectiveIsSortDescending =>
            _useSettingsSortDirection ? IsSortDescending
            : Filter.SortBy is not null ? Filter.SortDescending
            : IsSortDescending;
        public bool IsMatchCase => _settings.IsMatchCase;
        public bool IsMatchPath => _settings.IsMatchPath;
        public bool IsMatchWholeWord => _settings.IsMatchWholeWord;
        public bool IsMatchDiacritics => _settings.IsMatchDiacritics;
        public bool IsMatchPrefix => _settings.IsMatchPrefix;
        public bool IsMatchSuffix => _settings.IsMatchSuffix;
        public bool IsIgnorePunctuation => _settings.IsIgnorePunctuation;
        public bool IsIgnoreWhitespace => _settings.IsIgnoreWhitespace;
        public bool IsRegExEnabled => _settings.IsRegExEnabled;

        private bool _useSettingsSortKey;
        private bool _useSettingsSortDirection;

        private Filter _currentFilter;
        public Filter Filter
        {
            get => _currentFilter;
            set
            {
                if (SetProperty(ref _currentFilter, value))
                {
                    _settings.LastFilter = value.Name;
                    _useSettingsSortKey = false;
                    _useSettingsSortDirection = false;
                }
            }
        }

        private readonly SearchHistory _history;
        private readonly FilterProvider _filterProvider;
        private readonly ISettings _settings;
        private readonly IEverythingClient _everythingClient;

        public SearchState(
            SearchHistory history,
            FilterProvider filterProvider,
            ISettings settings,
            IEverythingClient everythingClient
        )
        {
            _history = history;
            _filterProvider = filterProvider;
            _settings = settings;
            _everythingClient = everythingClient;

            _currentFilter = _filterProvider.GetInitialFilter();

            _settings.PropertyChanged += OnSettingsChanged;
        }

        public void Reset()
        {
            if (_settings.IsEnableHistory)
                _history.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            Filter = _filterProvider.GetInitialFilter();
        }

        public string GetPreviousSearchTerm() => _history.GetPreviousItem();

        public string GetNextSearchTerm() => _history.GetNextItem();

        public void ClearHistory() => _history.ClearHistory();

        public void CycleFilters(int offset = 1)
        {
            var filterCount = _filterProvider.Filters.Count;
            if (filterCount == 0)
                return;

            var currentIndex = _filterProvider.Filters.IndexOf(Filter);
            var newIndex = (currentIndex + offset + filterCount) % filterCount;
            Filter = _filterProvider.Filters[newIndex];
        }

        public void SelectFilterFromIndex(int index)
        {
            if (index < 0 || index >= _filterProvider.Filters.Count)
                return;

            Filter = _filterProvider.Filters[index];
        }

        public void UseSettingsSortKey() => _useSettingsSortKey = true;

        public void UseSettingsSortDirection() => _useSettingsSortDirection = true;

        private string ApplyMacros(string searchTerm)
        {
            var result = searchTerm;

            foreach (var f in _filterProvider.Filters)
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
            var supportsEverything15 = _everythingClient.GetEverythingVersion().Minor >= 5;
            // The filter prefix comes first so the filter's own match settings aren't overridden by
            // the global modifiers below. Its search text starts from Everything's defaults, hence
            // the false values.
            var rawSearchTerm =
                Filter.GetSearchPrefix(
                    IsMatchCase,
                    IsMatchWholeWord,
                    IsMatchPath,
                    IsRegExEnabled,
                    false,
                    false,
                    false,
                    false,
                    false,
                    supportsEverything15
                ) + GetGlobalModifierPrefix(supportsEverything15);
            var searchTermWithAppliedMacros = ApplyMacros(rawSearchTerm);
            return searchTermWithAppliedMacros;
        }

        private string GetGlobalModifierPrefix(bool supportsEverything15)
        {
            if (IsRegExEnabled)
                return SearchTerm;

            var modifiers = "";

            if (IsMatchDiacritics)
                modifiers += "diacritics:";

            if (supportsEverything15)
            {
                if (IsMatchPrefix)
                    modifiers += "prefix:";
                if (IsMatchSuffix)
                    modifiers += "suffix:";
                if (IsIgnorePunctuation)
                    modifiers += "ignore-punctuation:";
                if (IsIgnoreWhitespace)
                    modifiers += "ignore-whitespace:";
            }

            if (string.IsNullOrEmpty(modifiers) || string.IsNullOrEmpty(SearchTerm))
                return SearchTerm;

            return $"{modifiers}<{SearchTerm}>";
        }

        public SearchQuery BuildSearchQuery()
        {
            return new SearchQuery(
                BuildSearchTerm(),
                EffectiveSortBy,
                EffectiveIsSortDescending,
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
                case nameof(ISettings.IsMatchDiacritics):
                    OnPropertyChanged(nameof(IsMatchDiacritics));
                    break;
                case nameof(ISettings.IsMatchPrefix):
                    OnPropertyChanged(nameof(IsMatchPrefix));
                    break;
                case nameof(ISettings.IsMatchSuffix):
                    OnPropertyChanged(nameof(IsMatchSuffix));
                    break;
                case nameof(ISettings.IsIgnorePunctuation):
                    OnPropertyChanged(nameof(IsIgnorePunctuation));
                    break;
                case nameof(ISettings.IsIgnoreWhitespace):
                    OnPropertyChanged(nameof(IsIgnoreWhitespace));
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
