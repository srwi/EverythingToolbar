using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using System.Windows.Controls;

namespace EverythingToolbar
{
	public partial class FilterSelector : UserControl
	{
		public FilterSelector()
		{
			InitializeComponent();

			DataContext = FilterLoader.Instance;
		}

		private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
		{
			if (TabControl.SelectedIndex < 0)
				return;

			ComboBox.SelectedIndex = -1;
			EverythingSearch.Instance.CurrentFilter = TabControl.SelectedItem as Filter;
		}

		private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
		{
			if (ComboBox.SelectedIndex < 0)
				return;

			TabControl.SelectedIndex = -1;
			EverythingSearch.Instance.CurrentFilter = ComboBox.SelectedItem as Filter;
		}
    }
}
