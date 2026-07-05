using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
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
            private bool _closingTaskbarWindowIntentionally;
            private uint _taskbarCreatedMsg;

            public LauncherWindow(NotifyIcon icon)
            {
                ToolbarLogger.Initialize("Launcher");

                _notifyIcon = icon;

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

                // Placement behavior is created first so CreateTaskbarWindow can point it at the surface.
                _searchWindowPlacementBehavior = new SearchWindowPlacement();
                Interaction.GetBehaviors(SearchWindow.Instance).Add(_searchWindowPlacementBehavior);

                // Initialize TaskbarWindow for Windows 11+ if enabled
                if (Helpers.Utils.IsTaskbarWindowActive())
                    CreateTaskbarWindow();
                else
                    TaskbarStateManager.Instance.IsIcon = true;

                StartToggleListener();

                if (
                    !Utils.IsTaskbarPinned()
                    && !Helpers.Utils.IsTaskbarWindowActive()
                    && (!ToolbarSettings.User.IsSetupAssistantDisabled || !ToolbarSettings.User.IsTrayIconEnabled)
                )
                    new SetupAssistant(icon).Show();

                ShortcutManager.Initialize(FocusSearchBox);

                StartMenuIntegration.Instance.Initialize();

                SetupSettingsCallbacks();

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
                                    Properties.Resources.TrayIconDisableErrorMessage,
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
                        if (Helpers.Utils.IsTaskbarWindowActive())
                        {
                            CreateTaskbarWindow();
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

                            CloseTaskbarWindow();
                        }
                    }
                };
            }

            protected override void OnSourceInitialized(EventArgs e)
            {
                base.OnSourceInitialized(e);

                // Listen for the shell's "TaskbarCreated" broadcast so we can rebuild the taskbar
                // child window after explorer restarts (which destroys our Shell_TrayWnd child).
                _taskbarCreatedMsg = RegisterWindowMessage("TaskbarCreated");
                if (PresentationSource.FromVisual(this) is HwndSource source)
                {
                    source.AddHook(WndProc);

                    if (_taskbarCreatedMsg != 0)
                        ChangeWindowMessageFilterEx(source.Handle, _taskbarCreatedMsg, MSGFLT_ALLOW, IntPtr.Zero);
                }
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (_taskbarCreatedMsg != 0 && msg == _taskbarCreatedMsg && Helpers.Utils.IsTaskbarWindowActive())
                {
                    // Delay so the new taskbar's UIA tree (Widgets button / tray) is ready for positioning.
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    timer.Tick += (_, _) =>
                    {
                        timer.Stop();
                        CloseTaskbarWindow();
                        CreateTaskbarWindow();
                    };
                    timer.Start();
                }

                return IntPtr.Zero;
            }

            private void CreateTaskbarWindow()
            {
                if (!Helpers.Utils.IsTaskbarWindowActive() || _taskbarWindow != null)
                    return;

                _taskbarWindow = new TaskbarWindow();
                _taskbarWindow.Closed += OnTaskbarWindowClosed;

                new WindowInteropHelper(_taskbarWindow).EnsureHandle();
                if (!_taskbarWindow.IsAttachedToTaskbar)
                {
                    CloseTaskbarWindow();
                    return;
                }

                _taskbarWindow.Show();
                TaskbarStateManager.Instance.IsIcon = false;
                if (_searchWindowPlacementBehavior != null)
                    _searchWindowPlacementBehavior.PlacementTarget = _taskbarWindow.PlacementTarget;
            }

            private void CloseTaskbarWindow()
            {
                if (_taskbarWindow != null)
                {
                    _closingTaskbarWindowIntentionally = true;
                    try
                    {
                        _taskbarWindow.Close();
                    }
                    catch
                    {
                        // Window may already be destroyed (e.g. explorer restarted); ignore.
                    }
                    finally
                    {
                        _taskbarWindow = null;
                        _closingTaskbarWindowIntentionally = false;
                    }
                }

                TaskbarStateManager.Instance.IsIcon = true;
                if (_searchWindowPlacementBehavior != null)
                    _searchWindowPlacementBehavior.PlacementTarget = null;
            }

            private void OnTaskbarWindowClosed(object? sender, EventArgs e)
            {
                if (_closingTaskbarWindowIntentionally)
                    return;

                // Window was destroyed externally (explorer took its Shell_TrayWnd child with it).
                // Fall back to launcher-popup behavior; recreation is handled by TaskbarCreated.
                _taskbarWindow = null;
                TaskbarStateManager.Instance.IsIcon = true;
                if (_searchWindowPlacementBehavior != null)
                    _searchWindowPlacementBehavior.PlacementTarget = null;
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
                // Global hotkey: when a live taskbar search box exists, jump keyboard focus into it
                // (results anchored to the box), mirroring the deskband. Otherwise toggle the popup.
                // The IsLoaded check guards against a dead-but-not-yet-nulled window eating the hotkey.
                if (_taskbarWindow is { IsLoaded: true })
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

            private void SetupSettingsCallbacks()
            {
                EverythingToolbar.Settings.Advanced.GetAutostartStateCallback = () => Utils.GetAutostartState();
                EverythingToolbar.Settings.Advanced.SetAutostartStateCallback = (state) => Utils.SetAutostartState(state);

                EverythingToolbar.Settings.SettingsWindow.RegisterPage(
                    new EverythingToolbar.Settings.SettingsPageDescriptor(
                        EverythingToolbar.Properties.Resources.SettingsTaskbarIntegration,
                        Wpf.Ui.Controls.SymbolRegular.Pin24,
                        typeof(Settings.TaskbarIntegration),
                        typeof(EverythingToolbar.Settings.UserInterface)));
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
                // Ensure IsIcon is restored to true when launcher closes
                CloseTaskbarWindow();
                base.OnClosed(e);
            }

            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern uint RegisterWindowMessage(string lpString);

            private const uint MSGFLT_ALLOW = 1;

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool ChangeWindowMessageFilterEx(IntPtr hwnd, uint message, uint action, IntPtr pChangeFilterStruct);
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
