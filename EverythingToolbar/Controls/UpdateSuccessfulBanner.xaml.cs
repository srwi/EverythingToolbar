using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Controls
{
    public partial class UpdateSuccessfulBanner
    {
        private static readonly string DonateUrl = "https://github.com/srwi/EverythingToolbar#-support";
        private static readonly string CurrentVersion = GetCurrentVersion();
        private readonly UpdateOptions _updateOptions = Ioc.Default.GetRequiredService<UpdateOptions>();

        public UpdateSuccessfulBanner()
        {
            InitializeComponent();
        }

        private static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version is { } version
                ? $"{version.Major}.{version.Minor}.{version.MajorRevision}"
                : "";
        }

        private bool ShouldShowUpdateNotification()
        {
            string versionBeforeUpdate = _updateOptions.VersionBeforeUpdate;

            if (string.IsNullOrEmpty(versionBeforeUpdate))
            {
                _updateOptions.VersionBeforeUpdate = CurrentVersion;
                return false;
            }

            if (versionBeforeUpdate != CurrentVersion)
            {
                return true;
            }

            return false;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Banner.Headline = Properties.Resources.UpdateSuccessfulBannerHeader.Replace("{version}", CurrentVersion);

            if (ShouldShowUpdateNotification())
            {
                Visibility = Visibility.Visible;
            }
        }

        private void OnDonateClicked(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(DonateUrl) { UseShellExecute = true });
        }

        private void OnDismissClicked(object sender, EventArgs e)
        {
            _updateOptions.VersionBeforeUpdate = CurrentVersion;
            Visibility = Visibility.Collapsed;
        }
    }
}
