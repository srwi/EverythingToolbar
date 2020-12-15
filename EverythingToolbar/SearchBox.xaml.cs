using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System;

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
				e.NewFocus.GetType() == typeof(Rules) ||
				e.NewFocus.GetType() == typeof(About) ||
				e.NewFocus.GetType() == typeof(ShortcutSelector) ||
				e.NewFocus.GetType().ToString() == "System.Windows.Documents.TextEditorContextMenu+EditorContextMenu") 
				return;

			Keyboard.Focus(this);
		}
	}
}
