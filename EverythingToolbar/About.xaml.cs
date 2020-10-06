using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace EverythingToolbar
{
	public partial class About : Window
	{
		public About()
		{
			InitializeComponent();

			VersionTextBlock.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
	}
}
