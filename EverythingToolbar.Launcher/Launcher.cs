using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Resources = EverythingToolbar.Launcher.Properties.Resources;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string EventName = "EverythingToolbarToggleEvent";
        private const string MutexName = "EverythingToolbar.Launcher";

        private class LauncherWindow : Window
        {
            public LauncherWindow()
            {
                ToolbarLogger.Initialize();

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
                    new TaskbarPinGuide().Show();

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
            }

            private void FocusSearchBox(object sender, HotkeyEventArgs e)
            {
                SearchWindow.Instance.Show();
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
                Dispatcher?.Invoke(() =>
                {
                    if (SearchWindow.Instance.Visibility == Visibility.Visible)
                        SearchWindow.Instance.Hide();
                    else
                        SearchWindow.Instance.Show();
                });
            }
        }

        [STAThread]
        private static void Main()
        {
            using (new Mutex(false, MutexName, out var createdNew))
            {
                if (createdNew)
                {
                    using (var icon = new NotifyIcon())
                    {
                        var app = new Application();
                        icon.Icon = Icon.ExtractAssociatedIcon(Utils.GetThemedIconPath());
                        icon.ContextMenu = new ContextMenu(new [] {
                            new MenuItem(Resources.ContextMenuRunSetupAssistant, (s, e) => { new TaskbarPinGuide().Show(); }),
                            new MenuItem(Resources.ContextMenuQuit, (s, e) => { app.Shutdown(); })
                        });
                        icon.Visible = true;
                        app.Run(new LauncherWindow());
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
