using System.Windows;

namespace EverythingToolbar
{
    public partial class InputDialog : Window
    {
        public InputDialog(string title = "Input", string text = "")
        {
            InitializeComponent();

            Title = title;
            ResponseText = text;
        }

        public string ResponseText
        {
            get
            {
                return ResponseTextBox.Text;
            }
            set
            {
                ResponseTextBox.Text = value;
            }
        }

        private void OnOkClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
