using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using NLog;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Resources = EverythingToolbar.Launcher.Properties.Resources;
using Timer = System.Timers.Timer;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string EventName = "EverythingToolbarToggleEvent";
        private const string MutexName = "EverythingToolbar.Launcher";
        private static bool _searchWindowRecentlyClosed;
        private static Timer _searchWindowRecentlyClosedTimer;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger("Launcher");

        private class LauncherWindow : Window
        {
            public LauncherWindow(NotifyIcon icon)
            {
                ToolbarLogger.Initialize();
                Logger.Info($"EverythingToolbar Launcher {Assembly.GetExecutingAssembly().GetName().Version} started. OS: {Environment.OSVersion}");

                _searchWindowRecentlyClosedTimer = new Timer(500);
                _searchWindowRecentlyClosedTimer.AutoReset = false;
                _searchWindowRecentlyClosedTimer.Elapsed += (s, e) => { _searchWindowRecentlyClosed = false; };

                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;

                TaskbarStateManager.Instance.IsIcon = true;

                var behavior = new SearchWindowPlacement();
                Interaction.GetBehaviors(SearchWindow.Instance).Add(behavior);

                StartToggleListener();

                if (!File.Exists(Utils.GetTaskbarShortcutPath()))
                    new SetupAssistant(icon).Show();

                if (!ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                       (Key)Settings.Default.shortcutKey,
                       (ModifierKeys)Settings.Default.shortcutModifiers,
                       FocusSearchBox))
                {
                    ShortcutManager.Instance.SetShortcut(Key.None, ModifierKeys.None);
                    MessageBox.Show(EverythingToolbar.Properties.Resources.MessageBoxFailedToRegisterHotkey,
                        EverythingToolbar.Properties.Resources.MessageBoxErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                ShortcutManager.Instance.SetFocusCallback(FocusSearchBox);
                if (Settings.Default.isReplaceStartMenuSearch)
                    ShortcutManager.Instance.HookStartMenu();

                SearchWindow.Instance.Hiding += OnSearchWindowHiding;
            }

            private static void OnSearchWindowHiding(object sender, EventArgs e)
            {
                _searchWindowRecentlyClosed = true;
                _searchWindowRecentlyClosedTimer.Start();
            }

            private static void FocusSearchBox(object sender, HotkeyEventArgs e)
            {
                SearchWindow.Instance.Toggle();
            }

            private void StartToggleListener()
            {
                Task.Factory.StartNew(() =>
                {
                    var wh = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
                    while (true)
                    {
                        wh.WaitOne();
                        ToggleWindow();
                    }
                });
            }

            private void ToggleWindow()
            {
                // Prevent search window from reappearing after clicking the icon to close
                if (_searchWindowRecentlyClosed)
                    return;
                
                Dispatcher?.Invoke(() =>
                {
                    SearchWindow.Instance.Toggle();
                });
            }
        }

        private static string GetIconPath()
        {
            var processPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            
            if (string.IsNullOrEmpty(Settings.Default.iconName))
                return Path.Combine(processPath, "..", "Icons", "Medium.ico");
            
            return Path.Combine(processPath, "..", Settings.Default.iconName);
        }

        [STAThread]
        private static void Main()
        {
            using (new Mutex(false, MutexName, out var createdNew))
            {
                if (createdNew)
                {
                    using (var trayIcon = new NotifyIcon())
                    {
                        var app = new Application();
                        trayIcon.Icon = Icon.ExtractAssociatedIcon(GetIconPath());
                        trayIcon.ContextMenu = new ContextMenu(new [] {
                            new MenuItem(Resources.ContextMenuRunSetupAssistant, (s, e) => { new SetupAssistant(trayIcon).Show(); }),
                            new MenuItem(Resources.ContextMenuQuit, (s, e) => { app.Shutdown(); })
                        });
                        trayIcon.Visible = true;
                        app.Run(new LauncherWindow(trayIcon));
                    }
                }
                else
                {
                    try
                    {
                        EventWaitHandle.OpenExisting(EventName).Set();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
