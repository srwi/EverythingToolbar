using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Windows.Controls;

namespace EverythingToolbar
{
    public partial class FilterSelector : UserControl
    {
        bool preventInitialSelectionChange = true;

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

        private void OnCurrentFilterChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentFilter")
            {
                SelectCurrentFilter();
            }
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (preventInitialSelectionChange)
            {
                preventInitialSelectionChange = false;
                return;
            }

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

        private void OnTabItemClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = (sender as Border).DataContext;
            int index = TabControl.Items.IndexOf(item);
            TabControl.SelectedItem = TabControl.Items[index];
        }
    }
}
