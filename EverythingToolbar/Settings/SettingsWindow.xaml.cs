using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web;
using System.Windows;
using EverythingToolbar.Search;
using Wpf.Ui.Controls;

namespace EverythingToolbar.Settings
{
    public readonly record struct SettingsPageDescriptor(
        string Title,
        SymbolRegular Icon,
        Type PageType,
        Type? InsertAfterPageType
    );

    public partial class SettingsWindow
    {
        private static readonly List<SettingsPageDescriptor> ExternalPages = new();

        public static void RegisterPage(SettingsPageDescriptor descriptor)
        {
            ExternalPages.Add(descriptor);
        }

        public SettingsWindow()
        {
            InitializeComponent();
            AddExternalPages();

            Loaded += (_, _) => Dispatcher.BeginInvoke(() => ThisNavigationView.Navigate(typeof(About)));
        }

        private void AddExternalPages()
        {
            foreach (var descriptor in ExternalPages)
            {
                var item = new NavigationViewItem
                {
                    Content = descriptor.Title,
                    Icon = new SymbolIcon { Symbol = descriptor.Icon },
                    TargetPageType = descriptor.PageType,
                };

                var index = -1;
                if (descriptor.InsertAfterPageType != null)
                {
                    for (var i = 0; i < ThisNavigationView.MenuItems.Count; i++)
                    {
                        if (
                            ThisNavigationView.MenuItems[i] is NavigationViewItem existing
                            && existing.TargetPageType == descriptor.InsertAfterPageType
                        )
                        {
                            index = i + 1;
                            break;
                        }
                    }
                }

                if (index >= 0)
                    ThisNavigationView.MenuItems.Insert(index, item);
                else
                    ThisNavigationView.MenuItems.Add(item);
            }
        }

        private void OnReportABugClicked(object sender, RoutedEventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            string everythingVersion = SearchResultProvider.GetEverythingVersion().ToString();
            string osVersion = Environment.OSVersion.ToString();

            string url =
                $"https://github.com/srwi/EverythingToolbar/issues/new?template=bug_report.yml"
                + $"&version={HttpUtility.UrlEncode(version)}"
                + $"&et_version={HttpUtility.UrlEncode(everythingVersion)}"
                + $"&windows_version={HttpUtility.UrlEncode(osVersion)}";

            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
