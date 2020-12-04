using System.Windows;
using System.Windows.Controls;

namespace EverythingToolbar
{
	public partial class FilterSelector : UserControl
	{
		private readonly int tabItemNumber = 3;

		public FilterSelector()
		{
			InitializeComponent();

			Properties.Settings.Default.PropertyChanged += OnPropertiesChanged;
			LoadFilters();
		}

		private void OnPropertiesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "isRegExEnabled")
			{
				LoadFilters();

				if (Properties.Settings.Default.isRegExEnabled)
					(TabControl.Items[0] as TabItem).IsSelected = true;
			}
		}

		private void LoadFilters()
		{
			TabControl.Items.Clear();
			ComboBox.Items.Clear();

			for (int i = 0; i < EverythingSearch.Instance.Filters.Count; i++)
			{
				if (i < tabItemNumber)
					TabControl.Items.Add(new TabItem { Header = EverythingSearch.Instance.Filters[i].Name });
				else
					ComboBox.Items.Add(new ComboBoxItem { Content = EverythingSearch.Instance.Filters[i].Name });
			}
		}

		private void OnTabItemSelected(object sender, SelectionChangedEventArgs e)
		{
			if (TabControl.SelectedIndex < 0)
				return;

			ComboBox.SelectedIndex = -1;
			EverythingSearch.Instance.CurrentFilter = EverythingSearch.Instance.Filters[TabControl.SelectedIndex];
		}

		private void OnComboBoxItemSelected(object sender, SelectionChangedEventArgs e)
		{
			if (ComboBox.SelectedIndex < 0)
				return;

			TabControl.SelectedIndex = -1;
			EverythingSearch.Instance.CurrentFilter = EverythingSearch.Instance.Filters[tabItemNumber + ComboBox.SelectedIndex];
		}
    }
}
