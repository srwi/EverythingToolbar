using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
	public partial class SearchButton : Button
	{
		private bool popupWasOpen = false;

		public SearchButton()
		{
			InitializeComponent();
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			popupWasOpen = EverythingSearch.Instance.SearchTerm != null;
		}

		private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			EverythingSearch.Instance.SearchTerm = popupWasOpen ? null : "";
			popupWasOpen = false;
		}

		private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Properties.Settings.Default.isIconOnly = (bool)e.NewValue;
		}
	}
}
