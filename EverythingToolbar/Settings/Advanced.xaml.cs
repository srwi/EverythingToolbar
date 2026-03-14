using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using EverythingToolbar.Search;

namespace EverythingToolbar.Settings
{
    public partial class Advanced : INotifyPropertyChanged
    {
        private bool _downloadUpdateButtonVisible;
        private bool _checkingForUpdatesVisible;
        private bool _noUpdatesBannerOpen;
        private string _latestVersionUrl;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsLauncher => Application.Current != null;

        public int WindowsBuildVersion => Environment.OSVersion.Version.Build;

        public bool CheckingForUpdatesVisible
        {
            get => _checkingForUpdatesVisible;
            set
            {
                _checkingForUpdatesVisible = value;
                OnPropertyChanged();
            }
        }

        public bool DownloadUpdateButtonVisible
        {
            get => _downloadUpdateButtonVisible;
            set
            {
                _downloadUpdateButtonVisible = value;
                OnPropertyChanged();
            }
        }

        public bool NoUpdatesBannerOpen
        {
            get => _noUpdatesBannerOpen;
            set
            {
                // Setting the margin should be done using a style and trigger, but it's currently
                // hard to do while WPF UI styles are loaded as dynamic resources.
                NoUpdatesInfoBar.Margin = value ? new Thickness(0, 15, 0, 0) : new Thickness(0);

                _noUpdatesBannerOpen = value;
                OnPropertyChanged();
            }
        }

        private bool _isWindowsSearchHidden = !Utils.GetWindowsSearchEnabledState();
        public bool IsWindowsSearchHidden
        {
            get => _isWindowsSearchHidden;
            set
            {
                if (_isWindowsSearchHidden != value)
                {
                    _isWindowsSearchHidden = value;
                    Utils.SetWindowsSearchEnabledState(!value);
                    OnPropertyChanged();
                }
            }
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
            try
            {
                CheckingForUpdatesVisible = true;
                NoUpdatesBannerOpen = false;
                DownloadUpdateButtonVisible = false;

                Version? latestVersion = await UpdateBanner.CheckForUpdateAsync();
                CheckingForUpdatesVisible = false;

                if (latestVersion != null)
                {
                    _latestVersionUrl = "https://github.com/srwi/EverythingToolbar/releases/latest";
                    DownloadUpdateButtonVisible = true;
                }
                else
                {
                    NoUpdatesBannerOpen = true;
                }
            }
            catch
            {
                CheckingForUpdatesVisible = false;
                NoUpdatesBannerOpen = false;
                DownloadUpdateButtonVisible = false;
            }
        }

        private void OnDownloadUpdateClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_latestVersionUrl))
            {
                Process.Start(new ProcessStartInfo { FileName = _latestVersionUrl, UseShellExecute = true });
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
