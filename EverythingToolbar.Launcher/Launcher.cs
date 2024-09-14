using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Shell;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Resources = EverythingToolbar.Launcher.Properties.Resources;
using Timer = System.Timers.Timer;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string ToggleEventName = "EverythingToolbarToggleEvent";
        private const string StartSetupAssistantEventName = "StartSetupAssistantEvent";
        private const string MutexName = "EverythingToolbar.Launcher";
        private static bool _searchWindowRecentlyClosed;
        private static Timer _searchWindowRecentlyClosedTimer;
        private static NotifyIcon notifyIcon;

        private class LauncherWindow : Window
        {
            public LauncherWindow(NotifyIcon icon)
            {
                ToolbarLogger.Initialize("Launcher");

                notifyIcon = icon;
                SetupJumpList();

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

                if (!ToolbarSettings.User.IsSetupAssistantDisabled && !File.Exists(Utils.GetTaskbarShortcutPath()))
                    new SetupAssistant(icon).Show();

                if (!ShortcutManager.Instance.AddOrReplace("FocusSearchBox",
                       (Key)ToolbarSettings.User.ShortcutKey,
                       (ModifierKeys)ToolbarSettings.User.ShortcutModifiers,
                       FocusSearchBox))
                {
                    ShortcutManager.Instance.SetShortcut(Key.None, ModifierKeys.None);
                    MessageBox.Show(EverythingToolbar.Properties.Resources.MessageBoxFailedToRegisterHotkey,
                        EverythingToolbar.Properties.Resources.MessageBoxErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                ShortcutManager.Instance.SetFocusCallback(FocusSearchBox);
                if (ToolbarSettings.User.IsReplaceStartMenuSearch)
                    ShortcutManager.Instance.HookStartMenu();

                SearchWindow.Instance.Hiding += OnSearchWindowHiding;
            }

            private void SetupJumpList()
            {
                JumpList jumpList = new JumpList();
                jumpList.JumpItems.Add(new JumpTask
                {
                    Title = Properties.Resources.ContextMenuRunSetupAssistant,
                    Description = Properties.Resources.ContextMenuRunSetupAssistant,
                    ApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location,
                    Arguments = "--run-setup-assistant"
                });
                JumpList.SetJumpList(Application.Current, jumpList);
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
                    var wh = new EventWaitHandle(false, EventResetMode.AutoReset, ToggleEventName);
                    while (true)
                    {
                        wh.WaitOne();
                        ToggleWindow();
                    }
                });
                Task.Factory.StartNew(() =>
                {
                    var wh = new EventWaitHandle(false, EventResetMode.AutoReset, StartSetupAssistantEventName);
                    while (true)
                    {
                        wh.WaitOne();
                        OpenSetupAssistant();
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

            private void OpenSetupAssistant()
            {
                Dispatcher?.Invoke(() =>
                {
                    new SetupAssistant(notifyIcon).Show();
                });
            }
        }

        private static string GetIconPath()
        {
            var processPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            
            if (string.IsNullOrEmpty(ToolbarSettings.User.IconName))
                return Path.Combine(processPath, "..", "Icons", "Medium.ico");
            
            return Path.Combine(processPath, "..", ToolbarSettings.User.IconName);
        }

        [STAThread]
        private static void Main(string[] args)
        {
            using (new Mutex(false, MutexName, out var createdNew))
            {
                if (createdNew)
                {
                    using (var trayIcon = new NotifyIcon())
                    {
                        var app = new Application();
                        trayIcon.Icon = Icon.ExtractAssociatedIcon(GetIconPath());
                        trayIcon.ContextMenu = new ContextMenu(new[] {
                            new MenuItem(Resources.ContextMenuRunSetupAssistant, (s, e) => { new SetupAssistant(trayIcon).Show(); }),
                            new MenuItem(Resources.ContextMenuQuit, (s, e) => { app.Shutdown(); })
                        });
                        trayIcon.Visible = ToolbarSettings.User.IsTrayIconEnabled;
                        app.Run(new LauncherWindow(trayIcon));
                    }
                }
                else
                {
                    try
                    {
                        if (args.Length > 0 && args[0] == "--run-setup-assistant")
                        {
                            EventWaitHandle.OpenExisting(StartSetupAssistantEventName).Set();
                        }
                        else
                        {
                            EventWaitHandle.OpenExisting(ToggleEventName).Set();
                        }
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
