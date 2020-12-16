using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Windows.Controls;
using System.Windows.Data;

namespace EverythingToolbar
{
	public partial class FilterSelector : UserControl
	{
		public FilterSelector()
		{
			InitializeComponent();
			DataContext = FilterLoader.Instance;
			EverythingSearch.Instance.PropertyChanged += OnCurrentFilterChanged;
		}

		private void OnCurrentFilterChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CurrentFilter")
			{
				TabControl.SelectionChanged -= OnTabItemSelected;
				TabControl.SelectedIndex = FilterLoader.Instance.DefaultFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
				TabControl.SelectionChanged += OnTabItemSelected;

				ComboBox.SelectionChanged -= OnComboBoxItemSelected;
				ComboBox.SelectedIndex = FilterLoader.Instance.UserFilters.IndexOf(EverythingSearch.Instance.CurrentFilter);
				ComboBox.SelectionChanged += OnComboBoxItemSelected;
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
