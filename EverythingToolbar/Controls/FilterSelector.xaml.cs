using System;
using System.Windows;
using System.Windows.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public class FilterChangedEventArgs : EventArgs
    {
        public Filter NewFilter { get; set; }
    }

    public partial class FilterSelector
    {
        public event EventHandler<FilterChangedEventArgs> FilterChanged;

        public static readonly DependencyProperty SelectedFilterProperty = 
            DependencyProperty.Register(
                nameof(SelectedFilter), 
                typeof(Filter), 
                typeof(FilterSelector), 
                new PropertyMetadata(FilterLoader.Instance.GetLastFilter(), OnSelectedFilterChanged));

        private static void OnSelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterSelector filterSelector && e.NewValue is Filter newFilter)
            {
                filterSelector.OnSelectedFilterChanged(newFilter);
            }
        }

        private void OnSelectedFilterChanged(Filter newFilter)
        {
            // Make sure we don't infinitely recurse
            if (_isInternalChange) return;

            _selectedFilter = newFilter;
            FilterChanged?.Invoke(this, new FilterChangedEventArgs { NewFilter = _selectedFilter });
            SelectCurrentFilter();
        }

        public Filter SelectedFilter
        {
            get => (Filter)GetValue(SelectedFilterProperty);
            set => SetValue(SelectedFilterProperty, value);
        }

        private int SelectedDefaultFilterIndex => FilterLoader.Instance.DefaultFilters.IndexOf(_selectedFilter);
        private int SelectedUserFilterIndex => FilterLoader.Instance.UserFilters.IndexOf(_selectedFilter);

        private Filter _selectedFilter = FilterLoader.Instance.GetLastFilter();
        private bool _isInternalChange;

        public FilterSelector()
        {
            InitializeComponent();
            DataContext = FilterLoader.Instance;

            Loaded += (s, e) => {
                // Set initial filter
                _selectedFilter = SelectedFilter;
                SelectCurrentFilter();
            };
        }

        private void SelectCurrentFilter()
        {
            TabControl.SelectionChanged -= OnTabItemSelected;
            TabControl.SelectedIndex = SelectedDefaultFilterIndex;
            TabControl.SelectionChanged += OnTabItemSelected;

            ComboBox.SelectionChanged -= OnComboBoxItemSelected;
            ComboBox.SelectedIndex = SelectedUserFilterIndex;
            ComboBox.SelectionChanged += OnComboBoxItemSelected;
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0)
                return;

            if (!TabControl.IsFocused && !TabControl.IsMouseOver) {
                TabControl.SelectedIndex = -1;
                return;
            }

            _isInternalChange = true;
            _selectedFilter = TabControl.SelectedItem as Filter;
            SelectedFilter = _selectedFilter;
            _isInternalChange = false;
            
            FilterChanged?.Invoke(this, new FilterChangedEventArgs { NewFilter = _selectedFilter });
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0)
                return;

            if (!ComboBox.IsFocused && !ComboBox.IsMouseOver) { 
                ComboBox.SelectedIndex = -1;
                return;
            }

            _isInternalChange = true;
            _selectedFilter = ComboBox.SelectedItem as Filter;
            SelectedFilter = _selectedFilter;
            _isInternalChange = false;
            
            FilterChanged?.Invoke(this, new FilterChangedEventArgs { NewFilter = _selectedFilter });
        }
    }
}
