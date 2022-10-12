using System.Windows;

namespace EverythingToolbar
{
    public partial class InputDialog : Window
    {
        public InputDialog(string title = "Input")
        {
            InitializeComponent();

            Title = title;
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
