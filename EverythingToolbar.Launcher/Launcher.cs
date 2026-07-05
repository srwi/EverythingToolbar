using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
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
        private static TrayIcon? _trayIcon;

        private class LauncherWindow : Window
        {
            private TaskbarWindow? _taskbarWindow;
            private readonly SearchWindow _searchWindow;
            private readonly TaskbarStateManager _taskbarState;
            private readonly SearchWindowPlacement? _searchWindowPlacementBehavior;
            private readonly WindowsPolicy _windowsPolicy;
            private readonly ISettings _settings;
            private bool _temporarilyInIconMode;
            private bool _closingTaskbarWindowIntentionally;
            private uint _taskbarCreatedMsg;

            private bool _suppressInitialTrayIcon;

            public LauncherWindow(TrayIcon icon)
            {
                ToolbarLogger.Initialize("Launcher");

                if (Application.Current != null)
                {
                    Application.Current.DispatcherUnhandledException += (_, args) =>
                        ToolbarLogger.LogUiThreadException(args.Exception);
                }

                _searchWindow = Ioc.Default.GetRequiredService<SearchWindow>();
                _taskbarState = Ioc.Default.GetRequiredService<TaskbarStateManager>();
                _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
                _settings = Ioc.Default.GetRequiredService<ISettings>();

                _trayIcon = icon;

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

                _searchWindowPlacementBehavior = new SearchWindowPlacement();
                Interaction.GetBehaviors(_searchWindow).Add(_searchWindowPlacementBehavior);

                if (_windowsPolicy.IsTaskbarWindowActive())
                    CreateTaskbarWindow();
                else
                    _taskbarState.IsIcon = true;

                StartToggleListener();

                if (
                    !Utils.IsTaskbarPinned()
                    && !_windowsPolicy.IsTaskbarWindowActive()
                    && (!_settings.IsSetupAssistantDisabled || !_settings.IsTrayIconEnabled)
                )
                {
                    _suppressInitialTrayIcon = true;
                    new SetupAssistant(icon).Show();
                }

                ShortcutManager.Initialize(FocusSearchBox);

                Ioc.Default.GetRequiredService<StartMenuIntegration>().Initialize();

                _searchWindow.Hiding += OnSearchWindowHiding;
                _searchWindow.Hidden += OnSearchWindowHidden;

                Dispatcher.BeginInvoke(_searchWindow.PreWarm, DispatcherPriority.ApplicationIdle);

                _settings.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(_settings.IsTrayIconEnabled))
                    {
                        if (
                            !_settings.IsTrayIconEnabled
                            && !Utils.IsTaskbarPinned()
                            && !_windowsPolicy.IsTaskbarWindowActive()
                        )
                        {
                            await FluentMessageBox
                                .CreateError(
                                    Properties.Resources.TrayIconDisableErrorMessage,
                                    Properties.Resources.TrayIconDisableErrorTitle
                                )
                                .ShowDialogAsync();

                            _settings.IsTrayIconEnabled = true;
                            return;
                        }

                        SetTrayIconVisible(_settings.IsTrayIconEnabled);
                    }
                };

                _settings.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(_settings.IconName))
                    {
                        var restartExplorer =
                            await FluentMessageBox
                                .CreateYesNo(
                                    Properties.Resources.SetupAssistantRestartExplorerDialogText,
                                    Properties.Resources.SetupAssistantRestartExplorerDialogTitle
                                )
                                .ShowDialogAsync() == MessageBoxResult.Primary;
                        Utils.ChangeTaskbarPinIcon(_settings.IconName, restartExplorer);
                    }
                };

                _settings.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(_settings.TaskbarWindowEnabled))
                    {
                        if (_windowsPolicy.IsTaskbarWindowActive())
                        {
                            CreateTaskbarWindow();
                        }
                        else
                        {
                            if (_searchWindow.IsVisible)
                                _searchWindow.Hide();

                            // Never leave the user with no way into search or settings.
                            if (!Utils.IsTaskbarPinned() && !_settings.IsTrayIconEnabled)
                            {
                                _settings.IsTrayIconEnabled = true;
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

                _taskbarCreatedMsg = PInvoke.RegisterWindowMessage("TaskbarCreated");
                if (PresentationSource.FromVisual(this) is HwndSource source)
                {
                    source.AddHook(WndProc);

                    if (_taskbarCreatedMsg != 0)
                        unsafe
                        {
                            PInvoke.ChangeWindowMessageFilterEx(
                                (HWND)source.Handle,
                                _taskbarCreatedMsg,
                                WINDOW_MESSAGE_FILTER_ACTION.MSGFLT_ALLOW,
                                null
                            );
                        }
                }

                Application.Current.MainWindow = this;
                if (!_suppressInitialTrayIcon)
                    SetTrayIconVisible(_settings.IsTrayIconEnabled);
            }

            private static void SetTrayIconVisible(bool visible)
            {
                if (_trayIcon == null)
                    return;

                if (visible)
                    _trayIcon.Show();
                else
                    _trayIcon.Hide();
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (_taskbarCreatedMsg != 0 && msg == _taskbarCreatedMsg)
                {
                    // Explorer restarted and dropped the tray icon; TaskbarCreated signals it's ready again.
                    _trayIcon?.HandleExplorerRestart();

                    if (_windowsPolicy.IsTaskbarWindowActive())
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
                }

                return IntPtr.Zero;
            }

            private void CreateTaskbarWindow()
            {
                if (!_windowsPolicy.IsTaskbarWindowActive() || _taskbarWindow != null)
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
                _taskbarState.IsIcon = false;
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

                _taskbarState.IsIcon = true;
                if (_searchWindowPlacementBehavior != null)
                    _searchWindowPlacementBehavior.PlacementTarget = null;
            }

            private void OnTaskbarWindowClosed(object? sender, EventArgs e)
            {
                if (_closingTaskbarWindowIntentionally)
                    return;

                _taskbarWindow = null;
                _taskbarState.IsIcon = true;
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
                if (_temporarilyInIconMode && _taskbarWindow != null)
                {
                    _taskbarState.IsIcon = false;
                    _temporarilyInIconMode = false;
                }
            }

            private void FocusSearchBox()
            {
                if (_taskbarWindow is { IsLoaded: true })
                {
                    if (_searchWindow.IsVisible)
                        _searchWindow.Hide();
                    else
                        WeakReferenceMessenger.Default.Send(new FocusSearchBoxRequest());
                }
                else
                {
                    _searchWindow.Toggle();
                }
            }

            private void ShowAsLauncher()
            {
                if (_taskbarWindow != null && _searchWindowPlacementBehavior != null)
                {
                    _temporarilyInIconMode = true;
                    _taskbarState.IsIcon = true;
                    _searchWindowPlacementBehavior.UseCursorPlacement = true;
                }

                _searchWindow.Show();
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
                    if (_searchWindow.IsVisible)
                        _searchWindow.Hide();
                    else
                        ShowAsLauncher();
                });
            }

            private void OpenSetupAssistant()
            {
                Dispatcher?.Invoke(() =>
                {
                    if (_trayIcon != null)
                        new SetupAssistant(_trayIcon).Show();
                });
            }

            protected override void OnClosed(EventArgs e)
            {
                CloseTaskbarWindow();
                base.OnClosed(e);
            }
        }

        private static void OpenSettingsWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is EverythingToolbar.Settings.SettingsWindow existing)
                {
                    if (existing.WindowState == WindowState.Minimized)
                        existing.WindowState = WindowState.Normal;

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
                    AppServices.Initialize();

                    // Apply saved UI language
                    CultureHelper.ApplyUILanguage(Ioc.Default.GetRequiredService<ISettings>().UILanguage);

                    EverythingToolbar.Settings.SettingsWindow.RegisterPage(
                        new EverythingToolbar.Settings.SettingsPageDescriptor(
                            EverythingToolbar.Properties.Resources.SettingsTaskbarIntegration,
                            Wpf.Ui.Controls.SymbolRegular.Pin24,
                            typeof(Settings.TaskbarIntegration),
                            typeof(EverythingToolbar.Settings.About)
                        )
                    );

                    var app = new Application();
                    using var trayIcon = new TrayIcon(OpenSettingsWindow, app.Shutdown);
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