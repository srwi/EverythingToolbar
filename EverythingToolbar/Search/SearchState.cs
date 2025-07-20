using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar.Search
{
    public sealed class SearchState : INotifyPropertyChanged
    {
        public static readonly SearchState Instance = new SearchState();

        private string _searchTerm = "";
        public string SearchTerm
        {
            get
            {
                var searchTermWithReplacedMacros = _searchTerm;
                foreach (var f in FilterLoader.Instance.Filters)
                {
                    if (string.IsNullOrEmpty(f.Macro))
                        continue;

                    searchTermWithReplacedMacros = searchTermWithReplacedMacros.Replace(f.Macro + ":", f.Search + " ");
                }
                return searchTermWithReplacedMacros;
            }
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _sortBy = ToolbarSettings.User.SortBy;
        public int SortBy
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

        private bool _isSortDescending = ToolbarSettings.User.IsSortDescending;
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

        private bool _isMatchCase = ToolbarSettings.User.IsMatchCase;
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

        private bool _isMatchPath = ToolbarSettings.User.IsMatchPath;
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

        private bool _isMatchWholeWord = ToolbarSettings.User.IsMatchWholeWord;
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

        private bool _isRegExEnabled = ToolbarSettings.User.IsRegExEnabled;
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

        private Filter _currentFilter = FilterLoader.Instance.GetInitialFilter();
        public Filter Filter
        {
            get => _currentFilter;
            set
            {
                if (!_currentFilter.Equals(value))
                {
                    _currentFilter = value;
                    ToolbarSettings.User.LastFilter = value.Name;
                    OnPropertyChanged();
                }
            }
        }

        private SearchState()
        {
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
        }

        public void Reset()
        {
            if (ToolbarSettings.User.IsEnableHistory)
                HistoryManager.Instance.AddToHistory(SearchTerm);
            else
                SearchTerm = "";

            Filter = FilterLoader.Instance.GetInitialFilter();
        }

        public void CycleFilters(int offset = 1)
        {
            var filterCount = FilterLoader.Instance.Filters.Count;
            if (filterCount == 0) return;
            
            var currentIndex = FilterLoader.Instance.Filters.IndexOf(Filter);
            
            // If current filter is not found in the list, start from the first filter
            if (currentIndex < 0)
            {
                Filter = FilterLoader.Instance.Filters[0];
                return;
            }
            
            var newIndex = (currentIndex + offset + filterCount) % filterCount;
            Filter = FilterLoader.Instance.Filters[newIndex];
        }

        public void SelectFilterFromIndex(int index)
        {
            Filter = FilterLoader.Instance.Filters[index];
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ToolbarSettings.User.SortBy):
                    SortBy = ToolbarSettings.User.SortBy;
                    break;
                case nameof(ToolbarSettings.User.IsSortDescending):
                    IsSortDescending = ToolbarSettings.User.IsSortDescending;
                    break;
                case nameof(ToolbarSettings.User.IsMatchCase):
                    IsMatchCase = ToolbarSettings.User.IsMatchCase;
                    break;
                case nameof(ToolbarSettings.User.IsMatchPath):
                    IsMatchPath = ToolbarSettings.User.IsMatchPath;
                    break;
                case nameof(ToolbarSettings.User.IsMatchWholeWord):
                    IsMatchWholeWord = ToolbarSettings.User.IsMatchWholeWord;
                    break;
                case nameof(ToolbarSettings.User.IsRegExEnabled):
                    IsRegExEnabled = ToolbarSettings.User.IsRegExEnabled;
                    break;
                case nameof(ToolbarSettings.User.IsHideEmptySearchResults):
                    SearchTerm = "";
                    OnPropertyChanged(nameof(SearchTerm));
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}