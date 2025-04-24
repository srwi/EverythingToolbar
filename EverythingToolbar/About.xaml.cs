using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace EverythingToolbar
{
    public partial class About
    {
        public About()
        {
            InitializeComponent();

            VersionTextBlock.Text = Properties.Resources.AboutVersion + " " + Assembly.GetExecutingAssembly().GetName().Version;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}