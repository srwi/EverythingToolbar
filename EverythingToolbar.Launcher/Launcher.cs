using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NHotkey;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace EverythingToolbar.Launcher
{
    class Launcher
    {
        private const string EVENT_NAME = "EverythingToolbarToggleEvent";
        private const string MUTEX_NAME = "EverythingToolbar.Launcher";

        public partial class LauncherWindow : Window
        {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

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
                       (Key)EverythingToolbar.Properties.Settings.Default.shortcutKey,
                       (ModifierKeys)EverythingToolbar.Properties.Settings.Default.shortcutModifiers,
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
                    EventWaitHandle wh = new EventWaitHandle(false, EventResetMode.AutoReset, EVENT_NAME);
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
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, MUTEX_NAME, out bool createdNew))
            {
                if (createdNew)
                {
                    using (System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon())
                    {
                        Application app = new Application();
                        icon.Icon = Icon.ExtractAssociatedIcon(Utils.GetThemedIconPath());
                        icon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] {
                            new System.Windows.Forms.MenuItem(Properties.Resources.ContextMenuRunSetupAssistant, (s, e) => { new TaskbarPinGuide().Show(); }),
                            new System.Windows.Forms.MenuItem(Properties.Resources.ContextMenuQuit, (s, e) => { app.Shutdown(); })
                        });
                        icon.Visible = true;
                        app.Run(new LauncherWindow());
                    }
                }
                else
                {
                    try
                    {
                        EventWaitHandle.OpenExisting(EVENT_NAME).Set();
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
