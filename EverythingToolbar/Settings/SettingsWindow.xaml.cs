using System.Diagnostics;
using System.Windows;

namespace EverythingToolbar.Settings
{
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            Loaded += (_, _) => Dispatcher.BeginInvoke(() => ThisNavigationView.Navigate(typeof(About)));
        }

        private void OnReportABugClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/srwi/EverythingToolbar/issues/new?template=bug_report.yml",
                UseShellExecute = true
            });
        }
    }
}