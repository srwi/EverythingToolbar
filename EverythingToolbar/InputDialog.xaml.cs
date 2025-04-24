using System.Windows;

namespace EverythingToolbar
{
    public partial class InputDialog
    {
        public InputDialog(string title = "Input", string text = "")
        {
            InitializeComponent();

            Title = title;
            ResponseText = text;
        }

        public string ResponseText
        {
            get => ResponseTextBox.Text;
            private set => ResponseTextBox.Text = value;
        }

        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}