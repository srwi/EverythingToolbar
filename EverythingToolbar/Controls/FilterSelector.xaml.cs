using System.ComponentModel;
using System.Windows.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Controls
{
    public partial class FilterSelector
    {
        public FilterSelector()
        {
            InitializeComponent();
            DataContext = FilterLoader.Instance;
            Loaded += (s, e) => {
                SelectCurrentFilter();
                EverythingSearch.Instance.PropertyChanged += OnCurrentFilterChanged;
            };
        }

        public int SelectedDefaultFilterIndex
        {
            get => FilterLoader.Instance.DefaultFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
        }

        public int SelectedUserFilterIndex
        {
            get => FilterLoader.Instance.UserFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
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

        private void OnCurrentFilterChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EverythingSearch.Instance.CurrentFilter))
            {
                SelectCurrentFilter();
            }
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0)
                return;

            if (!TabControl.IsFocused && !TabControl.IsMouseOver) {
                TabControl.SelectedIndex = -1;
                return;
            }

            EverythingSearch.Instance.CurrentFilter = TabControl.SelectedItem as Filter;
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0)
                return;

            if (!ComboBox.IsFocused && !ComboBox.IsMouseOver) { 
                ComboBox.SelectedIndex = -1;
                return;
            }

            EverythingSearch.Instance.CurrentFilter = ComboBox.SelectedItem as Filter;
        }
    }
}
