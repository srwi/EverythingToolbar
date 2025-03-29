using System;
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

        private Filter _selectedFilter = FilterLoader.Instance.GetLastFilter();

        private int SelectedDefaultFilterIndex => FilterLoader.Instance.DefaultFilters.IndexOf(_selectedFilter);
        private int SelectedUserFilterIndex => FilterLoader.Instance.UserFilters.IndexOf(_selectedFilter);

        public FilterSelector()
        {
            InitializeComponent();
            DataContext = FilterLoader.Instance;

            Loaded += (s, e) => {
                // TODO: This can probably just be done in the constructor directly
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

        private void OnCurrentFilterChanged(object sender, FilterChangedEventArgs e)
        {
            // TODO: Probably here the event args should be used
            SelectCurrentFilter();
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0)
                return;

            if (!TabControl.IsFocused && !TabControl.IsMouseOver) {
                TabControl.SelectedIndex = -1;
                return;
            }

            _selectedFilter = TabControl.SelectedItem as Filter;
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0)
                return;

            if (!ComboBox.IsFocused && !ComboBox.IsMouseOver) { 
                ComboBox.SelectedIndex = -1;
                return;
            }

            _selectedFilter = ComboBox.SelectedItem as Filter;
        }
    }
}
