using EverythingToolbar.Controls;
using EverythingToolbar.Search;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace EverythingToolbar.Settings
{
    public partial class Advanced : INotifyPropertyChanged
    {
        private bool _isCheckingForUpdates;
        private bool _isUpdateAvailable;
        private string _latestVersionUrl;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            set { _isCheckingForUpdates = value; OnPropertyChanged(); }
        }

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set { _isUpdateAvailable = value; OnPropertyChanged(); }
        }

        public Advanced()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            SearchResultProvider.SetInstanceName(ToolbarSettings.User.InstanceName);
        }

        private async void OnCheckForUpdatesClicked(object sender, RoutedEventArgs e)
        {
            IsCheckingForUpdates = true;
            IsUpdateAvailable = false;

            var latestVersion = await UpdateBanner.CheckForUpdateAsync();
            IsCheckingForUpdates = false;
            if (latestVersion != null)
            {
                // Placeholder: set a dummy URL for now
                _latestVersionUrl = "https://example.com/download";
                IsUpdateAvailable = true;
            }
            else
            {
                MessageBox.Show("You are already using the latest version.");
            }
        }

        private void OnDownloadUpdateClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_latestVersionUrl))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _latestVersionUrl,
                    UseShellExecute = true
                });
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}