using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;

namespace EverythingToolbar.Settings
{
    public partial class Advanced : INotifyPropertyChanged
    {
        private bool _downloadUpdateButtonVisible;
        private bool _checkingForUpdatesVisible;
        private bool _noUpdatesBannerOpen;
        private string _latestVersionUrl = "";

        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();
        private readonly IAutostart _autostart = Ioc.Default.GetRequiredService<IAutostart>();
        private readonly IEverythingClient _everythingClient = Ioc.Default.GetRequiredService<IEverythingClient>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsLauncher => Application.Current != null;

        public bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

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

        private bool _isAutostartEnabled;

        public bool IsAutostartEnabled
        {
            get => _isAutostartEnabled;
            set
            {
                if (_isAutostartEnabled != value)
                {
                    _isAutostartEnabled = value;
                    _autostart.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

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

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
