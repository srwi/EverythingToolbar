using System;
using System.IO;
using System.NativeTray;
using System.Windows;
using EverythingToolbar.Launcher.Properties;

namespace EverythingToolbar.Launcher
{
    internal sealed class TrayIcon : IDisposable
    {
        private readonly TrayIconHost _notifyIcon;
        private Win32Icon? _iconSource;

        public TrayIcon(Action onOpenSettings, Action onQuit)
        {
            _notifyIcon = new TrayIconHost
            {
                ToolTipText = "EverythingToolbar",
                ThemeMode = TrayThemeMode.System,
                IconSource = _iconSource = CreateThemedIcon(),
                IsVisible = false,
                Menu = new TrayMenu
                {
                    new TrayMenuItem
                    {
                        Header = Resources.ContextMenuSettings,
                        Command = new TrayCommand(_ => Dispatch(onOpenSettings)),
                    },
                    new TrayMenuItem
                    {
                        Header = Resources.ContextMenuQuit,
                        Command = new TrayCommand(_ => Dispatch(onQuit)),
                    },
                },
            };

            _notifyIcon.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public void Show() => _notifyIcon.IsVisible = true;

        public void Hide() => _notifyIcon.IsVisible = false;

        // NativeTray already re-adds the icon on TaskbarCreated.
        public void HandleExplorerRestart() { }

        public void Dispose()
        {
            _notifyIcon.UserPreferenceChanged -= OnUserPreferenceChanged;
            _notifyIcon.Dispose();
            _iconSource?.Dispose();
            _iconSource = null;
        }

        private void OnUserPreferenceChanged(object? sender, EventArgs e) => ApplyThemedIcon();

        private void ApplyThemedIcon()
        {
            var next = CreateThemedIcon();
            var previous = _iconSource;
            _iconSource = next;
            _notifyIcon.IconSource = next;
            previous?.Dispose();
        }

        private static Win32Icon CreateThemedIcon() =>
            new(File.ReadAllBytes(Utils.GetThemedAppIconPath(absolute: true)));

        private static void Dispatch(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(action);
        }
    }
}
