using System.Windows;
using System.Windows.Controls;

namespace EverythingToolbar
{
	public partial class SearchButton : Button
	{
		public SearchButton()
		{
			InitializeComponent();
		}

		private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (EverythingSearch.Instance.SearchTerm == null)
				EverythingSearch.Instance.SearchTerm = "";
			else
				EverythingSearch.Instance.SearchTerm = null;
		}

		private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Properties.Settings.Default.isIconOnly = (bool)e.NewValue;
		}
	}
}
