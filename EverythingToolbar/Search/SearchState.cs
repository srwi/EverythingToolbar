using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Search
{
    public sealed class SearchState : INotifyPropertyChanged
    {

        private string _searchTerm = "";
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                }
            }
        }

        private SortBy _sortBy;
        public SortBy SortBy
        {
            get => _sortBy;
            private set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isSortDescending;
        public bool IsSortDescending
        {
            get => _isSortDescending;
            private set
            {
                if (_isSortDescending != value)
                {
                    _isSortDescending = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchCase;
        public bool IsMatchCase
        {
            get => _isMatchCase;
            private set
            {
                if (_isMatchCase != value)
                {
                    _isMatchCase = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchPath;
        public bool IsMatchPath
        {
            get => _isMatchPath;
            private set
            {
                if (_isMatchPath != value)
                {
                    _isMatchPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isMatchWholeWord;
        public bool IsMatchWholeWord
        {
            get => _isMatchWholeWord;
            private set
            {
                if (_isMatchWholeWord != value)
                {
                    _isMatchWholeWord = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isRegExEnabled;
        public bool IsRegExEnabled
        {
            get => _isRegExEnabled;
            private set
            {
                if (_isRegExEnabled != value)
                {
                    _isRegExEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private Filter _currentFilter;
        public Filter Filter
        {
            get => _currentFilter;
            set
            {
                if (!_currentFilter.Equals(value))
                {
                    _currentFilter = value;
                    _filterOptions.LastFilter = value.Name;
                    OnPropertyChanged();
                }
            }
        }

        private readonly HistoryManager _history;
        private readonly FilterLoader _filterLoader;
        private readonly FilterOptions _filterOptions;
        private readonly MatchOptions _matchOptions;
        private readonly SortOptions _sortOptions;
        private readonly SearchOptions _searchOptions;

        public SearchState(HistoryManager history, FilterLoader filterLoader, FilterOptions filterOptions,
            MatchOptions matchOptions, SortOptions sortOptions, SearchOptions searchOptions)
        {
            _history = history;
            _filterLoader = filterLoader;
            _filterOptions = filterOptions;
            _matchOptions = matchOptions;
            _sortOptions = sortOptions;
            _searchOptions = searchOptions;

            _sortBy = (SortBy)_sortOptions.SortBy;
            _isSortDescending = _sortOptions.IsSortDescending;
            _isMatchCase = _matchOptions.IsMatchCase;
            _isMatchPath = _matchOptions.IsMatchPath;
            _isMatchWholeWord = _matchOptions.IsMatchWholeWord;
            _isRegExEnabled = _matchOptions.IsRegExEnabled;

            _currentFilter = _filterLoader.GetInitialFilter();

            _matchOptions.PropertyChanged += OnMatchOptionsChanged;
            _sortOptions.PropertyChanged += OnSortOptionsChanged;
            _searchOptions.PropertyChanged += OnSearchOptionsChanged;
        }

        public void Reset()
        {
            if (_searchOptions.IsEnableHistory)
                _history.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            Filter = _filterLoader.GetInitialFilter();
        }

        public string GetPreviousSearchTerm() => _history.GetPreviousItem();

        public string GetNextSearchTerm() => _history.GetNextItem();

        public void ClearHistory() => _history.ClearHistory();

        public void CycleFilters(int offset = 1)
        {
            var filterCount = _filterLoader.Filters.Count;
            var currentIndex = _filterLoader.Filters.IndexOf(Filter);
            var newIndex = (currentIndex + offset + filterCount) % filterCount;
            Filter = _filterLoader.Filters[newIndex];
        }

        public void SelectFilterFromIndex(int index)
        {
            if (index < 0 || index >= _filterLoader.Filters.Count)
                return;

            Filter = _filterLoader.Filters[index];
        }

        private string ApplyMacros(string searchTerm)
        {
            var result = searchTerm;

            foreach (var f in _filterLoader.Filters)
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

        private void OnMatchOptionsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MatchOptions.IsMatchCase):
                    IsMatchCase = _matchOptions.IsMatchCase;
                    break;
                case nameof(MatchOptions.IsMatchPath):
                    IsMatchPath = _matchOptions.IsMatchPath;
                    break;
                case nameof(MatchOptions.IsMatchWholeWord):
                    IsMatchWholeWord = _matchOptions.IsMatchWholeWord;
                    break;
                case nameof(MatchOptions.IsRegExEnabled):
                    IsRegExEnabled = _matchOptions.IsRegExEnabled;
                    break;
            }
        }

        private void OnSortOptionsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SortOptions.SortBy):
                    SortBy = (SortBy)_sortOptions.SortBy;
                    break;
                case nameof(SortOptions.IsSortDescending):
                    IsSortDescending = _sortOptions.IsSortDescending;
                    break;
            }
        }

        private void OnSearchOptionsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchOptions.IsHideEmptySearchResults))
            {
                SearchTerm = "";
                OnPropertyChanged(nameof(SearchTerm));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}