using System.Windows.Controls;
using System.Windows.Input;

namespace EverythingToolbar
{
	public partial class SearchBox : TextBox
	{
		public SearchBox()
		{
			InitializeComponent();

            DataContext = EverythingSearch.Instance;
        }

		private void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			EverythingSearch.Instance.Reset();
		}
	}
}
