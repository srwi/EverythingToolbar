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
			if (e.NewFocus == null)
			{
				EverythingSearch.Instance.Reset();
				return;
			}

			if (e.NewFocus.GetType() == typeof(ContextMenu) ||
				e.NewFocus.GetType() == typeof(ComboBox) ||
				e.NewFocus.GetType() == typeof(ListViewItem) ||
				e.NewFocus.GetType().ToString() == "System.Windows.Documents.TextEditorContextMenu+EditorContextMenu") 
				return;

			Keyboard.Focus(this);
		}
	}
}
