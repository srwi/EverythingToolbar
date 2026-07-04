using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shell;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using EverythingToolbar.Launcher.Properties;
using Microsoft.Xaml.Behaviors;
using Application = System.Windows.Application;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Timer = System.Timers.Timer;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string ToggleEventName = "EverythingToolbarToggleEvent";
        private const string StartSetupAssistantEventName = "StartSetupAssistantEvent";
        private const string MutexName = "EverythingToolbar.Launcher";
        private static bool _searchWindowRecentlyClosed;
        private static Timer? _searchWindowRecentlyClosedTimer;
        private static NotifyIcon? _notifyIcon;

        private class LauncherWindow : Window
        {
            private TaskbarWindow? _taskbarWindow;
            private SearchWindowPlacement? _searchWindowPlacementBehavior;
            private bool _temporarilyInIconMode;

            public LauncherWindow(NotifyIcon icon)
            {
                ToolbarLogger.Initialize("Launcher");

                _notifyIcon = icon;
                SetupJumpList();

                _searchWindowRecentlyClosedTimer = new Timer(500);
                _searchWindowRecentlyClosedTimer.AutoReset = false;
                _searchWindowRecentlyClosedTimer.Elapsed += (_, _) =>
                {
                    _searchWindowRecentlyClosed = false;
                };

                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;

                // Initialize TaskbarWindow for Windows 11+ if enabled
                if (ToolbarSettings.User.TaskbarWindowEnabled && 
                    Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11)
                {
                    _taskbarWindow = new TaskbarWindow();
                    _taskbarWindow.Show();
                    TaskbarStateManager.Instance.IsIcon = false;
                }
                else
                {
                    // Set IsIcon to true for launcher mode (no taskbar window)
                    TaskbarStateManager.Instance.IsIcon = true;
                }

                // Create placement behavior with optional target from TaskbarWindow
                _searchWindowPlacementBehavior = new SearchWindowPlacement
                {
                    PlacementTarget = _taskbarWindow?.PlacementTarget
                };
                Interaction.GetBehaviors(SearchWindow.Instance).Add(_searchWindowPlacementBehavior);

                StartToggleListener();

                if (
                    !Utils.IsTaskbarPinned()
                    && (!ToolbarSettings.User.IsSetupAssistantDisabled || !ToolbarSettings.User.IsTrayIconEnabled)
                )
                    new SetupAssistant(icon).Show();

                ShortcutManager.Initialize(FocusSearchBox);

                StartMenuIntegration.Instance.Initialize();

                SetupAutostartStateCallback();

                SearchWindow.Instance.Hiding += OnSearchWindowHiding;
                SearchWindow.Instance.Hidden += OnSearchWindowHidden;

                ToolbarSettings.User.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(ToolbarSettings.User.IsTrayIconEnabled))
                    {
                        if (!ToolbarSettings.User.IsTrayIconEnabled
                            && !Utils.IsTaskbarPinned()
                            && !Helpers.Utils.IsTaskbarWindowActive())
                        {
                            await FluentMessageBox
                                .CreateError(
                                    Properties.Resources.TrayIconDisableErrorText,
                                    Properties.Resources.TrayIconDisableErrorTitle
                                )
                                .ShowDialogAsync();

                            ToolbarSettings.User.IsTrayIconEnabled = true;
                            return;
                        }

                        _notifyIcon.Visible = ToolbarSettings.User.IsTrayIconEnabled;
                    }
                    else if (e.PropertyName == nameof(ToolbarSettings.User.IconName))
                    {
                        var restartExplorer =
                            await FluentMessageBox
                                .CreateYesNo(
                                    Properties.Resources.SetupAssistantRestartExplorerDialogText,
                                    Properties.Resources.SetupAssistantRestartExplorerDialogTitle
                                )
                                .ShowDialogAsync() == MessageBoxResult.Primary;
                        Utils.ChangeTaskbarPinIcon(ToolbarSettings.User.IconName, restartExplorer);
                    }
                    else if (e.PropertyName == nameof(ToolbarSettings.User.TaskbarWindowEnabled))
                    {
                        // Handle TaskbarWindow enable/disable
                        if (ToolbarSettings.User.TaskbarWindowEnabled && 
                            Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11)
                        {
                            if (_taskbarWindow == null)
                            {
                                _taskbarWindow = new TaskbarWindow();
                                _taskbarWindow.Show();
                                TaskbarStateManager.Instance.IsIcon = false;
                            }
                            // Update placement to use TaskbarWindow's control
                            if (_searchWindowPlacementBehavior != null)
                                _searchWindowPlacementBehavior.PlacementTarget = _taskbarWindow.PlacementTarget;
                        }
                        else
                        {
                            // Hide any popup anchored to the vanishing taskbar box before teardown.
                            if (SearchWindow.Instance.IsVisible)
                                SearchWindow.Instance.Hide();

                            // Enforce the access-surface invariant: never leave the user with no
                            // visible way into search or settings.
                            if (!Utils.IsTaskbarPinned() && !ToolbarSettings.User.IsTrayIconEnabled)
                            {
                                ToolbarSettings.User.IsTrayIconEnabled = true;
                                await FluentMessageBox
                                    .CreateRegular(
                                        Properties.Resources.TaskbarWindowDisabledTrayEnabledText,
                                        Properties.Resources.TaskbarWindowDisabledTrayEnabledTitle
                                    )
                                    .ShowDialogAsync();
                            }

                            _taskbarWindow?.Close();
                            _taskbarWindow = null;
                            // Restore launcher mode
                            TaskbarStateManager.Instance.IsIcon = true;
                            // Update placement to use cursor-based positioning
                            if (_searchWindowPlacementBehavior != null)
                                _searchWindowPlacementBehavior.PlacementTarget = null;
                        }
                    }
                };
            }

            private void SetupJumpList()
            {
                var jumpList = new JumpList();
                jumpList.JumpItems.Add(
                    new JumpTask
                    {
                        Title = Properties.Resources.ContextMenuRunSetupAssistant,
                        Description = Properties.Resources.ContextMenuRunSetupAssistant,
                        ApplicationPath = Environment.ProcessPath,
                        Arguments = "--run-setup-assistant",
                    }
                );
                JumpList.SetJumpList(Application.Current, jumpList);
            }

            private void OnSearchWindowHiding(object? sender, EventArgs e)
            {
                _searchWindowRecentlyClosed = true;
                _searchWindowRecentlyClosedTimer?.Start();
            }

            private void OnSearchWindowHidden(object? sender, EventArgs e)
            {
                // Restore surface mode after the launcher popup closes. Done on Hidden (after
                // the hide animation) so the popup's search box doesn't vanish mid-animation.
                if (_temporarilyInIconMode && _taskbarWindow != null)
                {
                    TaskbarStateManager.Instance.IsIcon = false;
                    _temporarilyInIconMode = false;
                }
            }

            private void FocusSearchBox()
            {
                // Global hotkey: when a taskbar search box exists, jump keyboard focus into it
                // (results anchored to the box), mirroring the deskband. Otherwise toggle the popup.
                if (_taskbarWindow != null)
                {
                    if (SearchWindow.Instance.IsVisible)
                        SearchWindow.Instance.Hide();
                    else
                        EventDispatcher.Instance.InvokeSearchBoxFocused(this, EventArgs.Empty);
                }
                else
                {
                    SearchWindow.Instance.Toggle();
                }
            }

            /// <summary>
            /// Opens the classic launcher popup: cursor-positioned with its own search box.
            /// When a taskbar window surface exists (IsIcon is normally false), temporarily
            /// switch to icon mode for this show; it is restored in OnSearchWindowHidden.
            /// </summary>
            private void ShowAsLauncher()
            {
                if (_taskbarWindow != null && _searchWindowPlacementBehavior != null)
                {
                    _temporarilyInIconMode = true;
                    TaskbarStateManager.Instance.IsIcon = true;
                    _searchWindowPlacementBehavior.UseCursorPlacement = true;
                }

                SearchWindow.Instance.Show();
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

            private void SetupAutostartStateCallback()
            {
                Settings.Advanced.GetAutostartStateCallback = () => Utils.GetAutostartState();
                Settings.Advanced.SetAutostartStateCallback = (state) => Utils.SetAutostartState(state);
            }

            private void ToggleWindow()
            {
                // Prevent search window from reappearing after clicking the icon to close
                if (_searchWindowRecentlyClosed)
                    return;

                Dispatcher?.Invoke(() =>
                {
                    if (SearchWindow.Instance.IsVisible)
                        SearchWindow.Instance.Hide();
                    else
                        ShowAsLauncher();
                });
            }

            private void OpenSetupAssistant()
            {
                Dispatcher?.Invoke(() =>
                {
                    if (_notifyIcon != null)
                        new SetupAssistant(_notifyIcon).Show();
                });
            }

            protected override void OnClosed(EventArgs e)
            {
                _taskbarWindow?.Close();
                _taskbarWindow = null;
                // Ensure IsIcon is restored to true when launcher closes
                TaskbarStateManager.Instance.IsIcon = true;
                base.OnClosed(e);
            }
        }

        private static void OpenSettingsWindow()
        {
            // Tray-menu escape hatch: Settings is otherwise only reachable via the
            // SearchWindow gear button, so this guarantees it stays accessible.
            foreach (Window window in Application.Current.Windows)
            {
                if (window is EverythingToolbar.Settings.SettingsWindow existing)
                {
                    existing.Activate();
                    return;
                }
            }

            new EverythingToolbar.Settings.SettingsWindow().Show();
        }

        [STAThread]
        private static void Main(string[] args)
        {
            using (new Mutex(false, MutexName, out var createdNew))
            {
                if (createdNew)
                {
                    // Apply saved UI language
                    CultureHelper.ApplyUILanguage(ToolbarSettings.User.UILanguage);

                    using var trayIcon = new NotifyIcon();
                    var app = new Application();
                    trayIcon.Icon = new Icon(Utils.GetThemedAppIconPath(absolute: true));
                    trayIcon.ContextMenuStrip = new ContextMenuStrip();
                    var setupItem = new ToolStripMenuItem(
                        Resources.ContextMenuRunSetupAssistant,
                        null,
                        (_, _) =>
                        {
                            new SetupAssistant(trayIcon).Show();
                        }
                    );
                    trayIcon.ContextMenuStrip.Items.Add(setupItem);
                    var settingsItem = new ToolStripMenuItem(
                        Resources.ContextMenuSettings,
                        null,
                        (_, _) =>
                        {
                            OpenSettingsWindow();
                        }
                    );
                    trayIcon.ContextMenuStrip.Items.Add(settingsItem);
                    var quitItem = new ToolStripMenuItem(
                        Resources.ContextMenuQuit,
                        null,
                        (_, _) =>
                        {
                            app.Shutdown();
                        }
                    );
                    trayIcon.ContextMenuStrip.Items.Add(quitItem);
                    trayIcon.Visible = ToolbarSettings.User.IsTrayIconEnabled;
                    app.Run(new LauncherWindow(trayIcon));
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
                        FluentMessageBox.CreateError(ex.Message, "Error").ShowDialogAsync();
                    }
                }
            }
        }
    }
}
