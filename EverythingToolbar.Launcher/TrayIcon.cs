using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EverythingToolbar.Launcher.Properties;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using NotifyIcon = Wpf.Ui.Tray.Controls.NotifyIcon;

namespace EverythingToolbar.Launcher
{
    internal sealed class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ThemeService _themeService;
        private readonly ThemesDictionary _menuTheme;

        public TrayIcon(Action onOpenSettings, Action onQuit, ThemeService themeService)
        {
            _themeService = themeService;
            var contextMenu = new ContextMenu();

            var settingsItem = new MenuItem { Header = Resources.ContextMenuSettings };
            settingsItem.Click += (_, _) => onOpenSettings();
            contextMenu.Items.Add(settingsItem);

            var quitItem = new MenuItem { Header = Resources.ContextMenuQuit };
            quitItem.Click += (_, _) => onQuit();
            contextMenu.Items.Add(quitItem);

            _menuTheme = new ThemesDictionary { Theme = ToApplicationTheme(_themeService.GetEffectiveTheme(ThemeFlavor.App)) };
            contextMenu.Resources.MergedDictionaries.Add(new ControlsDictionary());
            contextMenu.Resources.MergedDictionaries.Add(_menuTheme);
            _themeService.ThemeChanged += OnThemeChanged;

            _notifyIcon = new NotifyIcon
            {
                Icon = new BitmapImage(new Uri(Utils.GetThemedAppIconPath(absolute: true))),
                Menu = contextMenu,
            };
        }

        public void Show() => _notifyIcon.Register();

        public void Hide() => _notifyIcon.Unregister();

        public void HandleExplorerRestart()
        {
            if (_notifyIcon.IsRegistered)
                _notifyIcon.Register();
        }

        public void Dispose()
        {
            _themeService.ThemeChanged -= OnThemeChanged;
            _notifyIcon.Dispose();
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) =>
            _menuTheme.Theme = ToApplicationTheme(e.AppTheme);

        private static ApplicationTheme ToApplicationTheme(Theme theme) =>
            theme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark;
    }
}