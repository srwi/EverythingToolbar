using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Web;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.Win32;
using Windows.Win32.Foundation;
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
        private readonly IEverythingClient _everythingClient = Ioc.Default.GetRequiredService<IEverythingClient>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        private const double MinWindowWidth = 640;
        private const double MinWindowHeight = 400;
        private const double ScreenMargin = 12;

        public static void RegisterPage(SettingsPageDescriptor descriptor)
        {
            ExternalPages.Add(descriptor);
        }

        public SettingsWindow()
        {
            InitializeComponent();
            AddExternalPages();
            Width = Math.Max(MinWindowWidth, _settings.SettingsWindowWidth);
            Height = Math.Max(MinWindowHeight, _settings.SettingsWindowHeight);
            Closing += (_, _) => SaveSize();

            Loaded += (_, _) => Dispatcher.BeginInvoke(() => ThisNavigationView.Navigate(typeof(About)));
        }

        // Opens next to the given window, on the side with more room, so both stay visible. If there isn't
        // enough room, the window gets narrower first and only then overlaps the other window.
        public void PlaceBeside(System.Windows.Window other)
        {
            var hwnd = new WindowInteropHelper(other).Handle;
            if (hwnd == IntPtr.Zero || !PInvoke.GetWindowRect((HWND)hwnd, out RECT rect))
                return;

            var dpi = VisualTreeHelper.GetDpi(other);
            var workArea = System.Windows.Forms.Screen.FromHandle(hwnd).WorkingArea;
            var area = new Rect(
                workArea.Left / dpi.DpiScaleX,
                workArea.Top / dpi.DpiScaleY,
                workArea.Width / dpi.DpiScaleX,
                workArea.Height / dpi.DpiScaleY
            );
            var target = new Rect(
                rect.left / dpi.DpiScaleX,
                rect.top / dpi.DpiScaleY,
                (rect.right - rect.left) / dpi.DpiScaleX,
                (rect.bottom - rect.top) / dpi.DpiScaleY
            );

            double roomLeft = target.Left - area.Left - 2 * ScreenMargin;
            double roomRight = area.Right - target.Right - 2 * ScreenMargin;
            bool placeLeft = roomLeft >= roomRight;

            Width = Math.Max(MinWindowWidth, Math.Min(Width, Math.Max(roomLeft, roomRight)));
            Height = Math.Max(MinWindowHeight, Math.Min(Height, area.Height - 2 * ScreenMargin));

            double left = placeLeft ? target.Left - ScreenMargin - Width : target.Right + ScreenMargin;
            double top = target.Bottom - Height;

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Math.Max(area.Left + ScreenMargin, Math.Min(left, area.Right - ScreenMargin - Width));
            Top = Math.Max(area.Top + ScreenMargin, Math.Min(top, area.Bottom - ScreenMargin - Height));
        }

        // Only the size is kept. The position follows the search window.
        private void SaveSize()
        {
            var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            if (bounds.IsEmpty)
                return;

            _settings.SettingsWindowWidth = (int)bounds.Width;
            _settings.SettingsWindowHeight = (int)bounds.Height;
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
            string everythingVersion = _everythingClient.GetEverythingVersion().ToString();
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
