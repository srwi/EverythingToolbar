using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Core.Search;

namespace EverythingToolbar.App.Search
{
    public sealed partial class SearchSession : ObservableObject, IDisposable
    {
        private const int PageSize = 256;

        private readonly SearchState _searchState;
        private readonly IEverythingClient _everythingClient;
        private readonly ISettings _settings;

        private VirtualizingCollection<SearchResult>? _collection;
        private bool _started;

        public SearchSession(SearchState searchState, IEverythingClient everythingClient, ISettings settings)
        {
            _searchState = searchState;
            _everythingClient = everythingClient;
            _settings = settings;

            _searchState.PropertyChanged += (_, _) => Rebuild();
        }

        public IList? Results => _collection;

        public int TotalCount { get; private set; }

        public bool IsBusy => _collection is { IsBusy: true };

        public event Action? ResultsReset;

        [ObservableProperty]
        private int _selectedIndex = -1;

        public SearchResult? SelectedResult =>
            _collection != null && SelectedIndex >= 0 && SelectedIndex < _collection.Count
                ? _collection[SelectedIndex]
                : null;

        public int VisiblePageCount { get; set; } = 1;

        public bool KeepSearchBoxFocused => _settings.IsAutoSelectFirstResult && _settings.IsSearchAsYouType;

        private FocusBehavior EffectiveListFocusBehavior =>
            KeepSearchBoxFocused && _settings.ListFocusBehavior == FocusBehavior.RepeatWithSearch
                ? FocusBehavior.Repeat
                : _settings.ListFocusBehavior;

        public void AutoSelect()
        {
            SelectedIndex = _settings.IsAutoSelectFirstResult && TotalCount > 0 ? 0 : -1;
        }

        public void ClearSelection() => SelectedIndex = -1;

        public void MoveDown()
        {
            if (TotalCount == 0)
                return;

            if (SelectedIndex == TotalCount - 1)
            {
                switch (EffectiveListFocusBehavior)
                {
                    case FocusBehavior.Repeat:
                        SelectedIndex = 0;
                        break;
                    case FocusBehavior.RepeatWithSearch:
                        SelectedIndex = -1;
                        break;
                }
            }
            else
            {
                SelectedIndex += 1; // from -1 â†’ 0 (select first), otherwise next
            }
        }

        public void MoveUp()
        {
            if (TotalCount == 0)
                return;

            if (SelectedIndex > 0)
            {
                SelectedIndex -= 1;
            }
            else if (SelectedIndex == 0)
            {
                switch (EffectiveListFocusBehavior)
                {
                    case FocusBehavior.Repeat:
                        SelectedIndex = TotalCount - 1; // jump to end
                        break;
                    case FocusBehavior.RepeatWithSearch:
                        SelectedIndex = -1;
                        break;
                    case FocusBehavior.Clamp:
                    default:
                        if (!_settings.IsAutoSelectFirstResult)
                            SelectedIndex = -1;
                        break;
                }
            }
            else // no selection
            {
                if (EffectiveListFocusBehavior != FocusBehavior.Clamp)
                    SelectedIndex = TotalCount - 1; // jump to end
            }
        }

        public void SelectFirst()
        {
            if (TotalCount > 0)
                SelectedIndex = 0;
        }

        public void SelectLast()
        {
            if (TotalCount > 0)
                SelectedIndex = TotalCount - 1;
        }

        public void PageDown() => SelectByOffset(VisiblePageCount);

        public void PageUp() => SelectByOffset(-VisiblePageCount);

        private void SelectByOffset(int offset)
        {
            if (TotalCount == 0)
                return;

            var baseIndex = SelectedIndex < 0 ? 0 : SelectedIndex;
            SelectedIndex = Math.Clamp(baseIndex + offset, 0, TotalCount - 1);
        }

        public bool IsAsync
        {
            set
            {
                if (_collection != null)
                    _collection.IsAsync = value;
            }
        }

        public void Start()
        {
            _started = true;
            Rebuild();
        }

        private void Rebuild()
        {
            if (!_started)
                return;

            if (_settings.IsHideEmptySearchResults && string.IsNullOrEmpty(_searchState.SearchTerm))
            {
                _collection?.Dispose();
                _collection = null;
                TotalCount = 0;
                OnPropertyChanged(nameof(Results));
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(IsBusy));
                return;
            }

            var newProvider = new EverythingItemsProvider(_everythingClient, _searchState.BuildSearchQuery());

            if (_collection == null)
            {
                _collection = new VirtualizingCollection<SearchResult>(newProvider, PageSize);
                _collection.CollectionChanged += OnCollectionChanged;
                _collection.PropertyChanged += OnCollectionPropertyChanged;
            }
            else
            {
                _collection.UpdateProvider(newProvider);
            }

            OnPropertyChanged(nameof(Results));
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Reset)
                return;

            TotalCount = _collection?.Count ?? 0;
            OnPropertyChanged(nameof(TotalCount));
            ResultsReset?.Invoke();
        }

        private void OnCollectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VirtualizingCollection<SearchResult>.IsBusy))
                OnPropertyChanged(nameof(IsBusy));
        }

        public void Dispose()
        {
            _collection?.Dispose();
            _collection = null;
        }
    }
}
