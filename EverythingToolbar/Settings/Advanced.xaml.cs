using System;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;

namespace EverythingToolbar.Settings
{
    [ObservableObject]
    public partial class Advanced
    {
        [ObservableProperty]
        private bool _checkingForUpdatesVisible;

        [ObservableProperty]
        private bool _downloadUpdateButtonVisible;

        [ObservableProperty]
        private bool _noUpdatesBannerOpen;

        partial void OnNoUpdatesBannerOpenChanged(bool value)
        {
            // Setting the margin should be done using a style and trigger, but it's currently
            // hard to do while WPF UI styles are loaded as dynamic resources.
            NoUpdatesInfoBar.Margin = value ? new Thickness(0, 15, 0, 0) : new Thickness(0);
        }

        [ObservableProperty]
        private bool _isAutostartEnabled;

        partial void OnIsAutostartEnabledChanged(bool value)
        {
            _autostart.IsEnabled = value;
        }

        private string _latestVersionUrl = "";

        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();
        private readonly IAutostart _autostart = Ioc.Default.GetRequiredService<IAutostart>();
        private readonly IEverythingClient _everythingClient = Ioc.Default.GetRequiredService<IEverythingClient>();

        public bool IsLauncher => Application.Current != null;

        public bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

        public Advanced()
        {
            InitializeComponent();
            _isAutostartEnabled = _autostart.IsEnabled;
            DataContext = this;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _everythingClient.SetInstanceName(Settings.InstanceName);
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
    }
}
