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
            EverythingSearch.Instance.PropertyChanged += OnCurrentFilterChanged;
            Loaded += (s, e) => { SelectCurrentFilter(); };
        }

        private void SelectCurrentFilter()
        {
            TabControl.SelectionChanged -= OnTabItemSelected;
            TabControl.SelectedIndex = FilterLoader.Instance.DefaultFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
            TabControl.SelectionChanged += OnTabItemSelected;

            ComboBox.SelectionChanged -= OnComboBoxItemSelected;
            ComboBox.SelectedIndex = FilterLoader.Instance.UserFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
            ComboBox.SelectionChanged += OnComboBoxItemSelected;
        }

        private void OnCurrentFilterChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentFilter")
            {
                SelectCurrentFilter();
            }
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0)
                return;

            EverythingSearch.Instance.CurrentFilter = TabControl.SelectedItem as Filter;
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0)
                return;

            EverythingSearch.Instance.CurrentFilter = ComboBox.SelectedItem as Filter;
        }
    }
}
