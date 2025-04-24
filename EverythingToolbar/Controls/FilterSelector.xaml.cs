using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace EverythingToolbar.Controls
{
    public partial class FilterSelector
    {
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register(
                nameof(SelectedFilter),
                typeof(Filter),
                typeof(FilterSelector),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFilterChanged));

        private static void OnSelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FilterSelector)d;
            control.UpdateSelectedItems();
        }

        public Filter SelectedFilter
        {
            get => (Filter)GetValue(SelectedFilterProperty);
            set => SetValue(SelectedFilterProperty, value);
        }

        public FilterSelector()
        {
            InitializeComponent();

            Loaded += (s, e) => UpdateSelectedItems();
        }

        private void UpdateSelectedItems()
        {
            if (SelectedFilter == null) return;

            TabControl.SelectionChanged -= OnTabItemSelected;
            ComboBox.SelectionChanged -= OnComboBoxItemSelected;

            TabControl.SelectedIndex = FilterLoader.Instance.DefaultFilters.IndexOf(SelectedFilter);
            ComboBox.SelectedIndex = FilterLoader.Instance.UserFilters.IndexOf(SelectedFilter);

            TabControl.SelectionChanged += OnTabItemSelected;
            ComboBox.SelectionChanged += OnComboBoxItemSelected;
        }

        private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (TabControl.SelectedIndex < 0) return;

            if (!TabControl.IsFocused && !TabControl.IsMouseOver)
            {
                TabControl.SelectedIndex = -1;
                return;
            }

            if (TabControl.SelectedItem is Filter selectedFilter)
                SelectedFilter = selectedFilter;
        }

        private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBox.SelectedIndex < 0) return;

            if (!ComboBox.IsFocused && !ComboBox.IsMouseOver)
            {
                ComboBox.SelectedIndex = -1;
                return;
            }

            if (ComboBox.SelectedItem is Filter selectedFilter)
                SelectedFilter = selectedFilter;
        }
    }
}