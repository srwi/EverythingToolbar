using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using NLog;

namespace EverythingToolbar.Controls
{
    public partial class UpdateBanner
    {
        private Version? _latestVersion;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<UpdateBanner>();
        private static readonly string ApiUrl = "https://api.github.com/repos/srwi/EverythingToolbar/releases";
        private static readonly string LatestReleaseUrl = "https://github.com/srwi/EverythingToolbar/releases/latest";
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        public UpdateBanner()
        {
            InitializeComponent();
        }

        private static async Task<Version?> GetLatestStableReleaseVersion()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("EverythingToolbar");

                var response = await client.GetAsync(ApiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonStream = await response.Content.ReadAsStreamAsync();
                    var serializer = new DataContractJsonSerializer(typeof(List<Release>));
                    var releases = serializer.ReadObject(jsonStream) as List<Release>;
                    var stableReleases = releases?.Where(r => !r.Prerelease).ToList();
                    var latestStableRelease = stableReleases?.FirstOrDefault();
                    if (latestStableRelease != null)
                    {
                        return new Version(latestStableRelease.TagName);
                    }
                }
            }
            catch
            {
                Logger.Info("Failed to get latest release version.");
            }

            return null;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_settings.IsUpdateNotificationsEnabled)
                    return;

                var latestVersion = await CheckForUpdateAsync();

                if (latestVersion == null || latestVersion == TryGetSkippedUpdate())
                    return;

                _latestVersion = latestVersion;
                if (FindName("Banner") is GenericBanner banner)
                {
                    banner.Text = $"{Properties.Resources.UpdateBannerText} {_latestVersion}";
                }
                Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check for updates: {Message}", ex.Message);
            }
        }

        private Version? TryGetSkippedUpdate()
        {
            try
            {
                return new Version(_settings.SkippedUpdate);
            }
            catch
            {
                return null;
            }
        }

        private void OnDownloadClicked(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(LatestReleaseUrl) { UseShellExecute = true });
        }

        private void OnSkipUpdateClicked(object sender, EventArgs e)
        {
            if (_latestVersion != null)
            {
                _settings.SkippedUpdate = _latestVersion.ToString();
            }
            Visibility = Visibility.Collapsed;
        }

        public static async Task<Version?> CheckForUpdateAsync()
        {
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var latestVersion = await GetLatestStableReleaseVersion();

            if (latestVersion == null)
                return null;
            if (assemblyVersion == null || assemblyVersion.CompareTo(latestVersion) >= 0)
                return null;

            return latestVersion;
        }

        [DataContract]
        private class Release
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; } = string.Empty;

            [DataMember(Name = "prerelease")]
            public bool Prerelease { get; set; }
        }
    }
}
