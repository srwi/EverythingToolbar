using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Application = System.Windows.Application;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EverythingToolbar.Launcher
{
    internal static class Launcher
    {
        private const string ToggleEventName = "EverythingToolbarToggleEvent";
        private const string StartSetupAssistantEventName = "StartSetupAssistantEvent";
        private const string MutexName = "EverythingToolbar.Launcher";
        private static TrayIcon? TrayIcon;

        private class LauncherWindow : Window
        {
            private readonly SearchWindow _searchWindow;
            private readonly SearchWindowController _controller;
            private readonly SearchHost _searchHost;
            private readonly WindowsPolicy _windowsPolicy;
            private readonly ISettings _settings;
            private readonly TaskbarWindowHost _taskbarWindowHost;
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
                _controller = Ioc.Default.GetRequiredService<SearchWindowController>();
                _searchHost = Ioc.Default.GetRequiredService<SearchHost>();
                _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
                _settings = Ioc.Default.GetRequiredService<ISettings>();
                _taskbarWindowHost = new TaskbarWindowHost(_windowsPolicy, _settings, _controller, _searchHost);

                TrayIcon = icon;

                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;

                _searchHost.Attach(placementTarget: null, iconMode: !_windowsPolicy.IsTaskbarWindowActive());

                if (_windowsPolicy.IsTaskbarWindowActive())
                    _taskbarWindowHost.Create();

                StartToggleListener();

                if (
                    !Utils.IsTaskbarPinned()
                    && !_taskbarWindowHost.IsRunning
                    && (!_settings.IsSetupAssistantDisabled || !_settings.IsTrayIconEnabled)
                )
                {
                    _suppressInitialTrayIcon = true;
                    new SetupAssistant(icon).Show();
                }

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

                WeakReferenceMessenger.Default.Register<TaskbarPinIconChanged>(
                    this,
                    async (_, message) =>
                    {
                        var restartExplorer =
                            await FluentMessageBox
                                .CreateYesNo(
                                    Properties.Resources.SetupAssistantRestartExplorerDialogText,
                                    Properties.Resources.SetupAssistantRestartExplorerDialogTitle
                                )
                                .ShowDialogAsync() == MessageBoxResult.Primary;
                        Utils.ChangeTaskbarPinIcon(message.IconName, restartExplorer);
                    }
                );

                _settings.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(_settings.TaskbarWindowEnabled))
                    {
                        if (_windowsPolicy.IsTaskbarWindowActive())
                        {
                            _taskbarWindowHost.Create();
                        }
                        else
                        {
                            if (_searchWindow.IsVisible)
                                _controller.Hide();

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

                            _taskbarWindowHost.Close();
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
                if (TrayIcon == null)
                    return;

                if (visible)
                    TrayIcon.Show();
                else
                    TrayIcon.Hide();
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (_taskbarCreatedMsg != 0 && msg == _taskbarCreatedMsg)
                {
                    // Explorer restarted and dropped the tray icon; TaskbarCreated signals it's ready again.
                    TrayIcon?.HandleExplorerRestart();
                    _taskbarWindowHost.HandleExplorerRestart();
                }

                return IntPtr.Zero;
            }

            private void StartToggleListener()
            {
                Task.Factory.StartNew(
                    () =>
                    {
                        var wh = new EventWaitHandle(false, EventResetMode.AutoReset, ToggleEventName);
                        while (true)
                        {
                            wh.WaitOne();
                            ToggleWindow();
                        }
                    },
                    TaskCreationOptions.LongRunning
                );
                Task.Factory.StartNew(
                    () =>
                    {
                        var wh = new EventWaitHandle(false, EventResetMode.AutoReset, StartSetupAssistantEventName);
                        while (true)
                        {
                            wh.WaitOne();
                            OpenSetupAssistant();
                        }
                    },
                    TaskCreationOptions.LongRunning
                );
            }

            private void ToggleWindow()
            {
                _controller.TogglePopupAtCursor();
            }

            private void OpenSetupAssistant()
            {
                Dispatcher?.Invoke(() =>
                {
                    if (TrayIcon != null)
                        new SetupAssistant(TrayIcon).Show();
                });
            }

            protected override void OnClosed(EventArgs e)
            {
                _taskbarWindowHost.Close();
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
                    using var trayIcon = new TrayIcon(
                        OpenSettingsWindow,
                        app.Shutdown,
                        Ioc.Default.GetRequiredService<ThemeService>()
                    );
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
                        FluentMessageBox
                            .CreateError(ex.Message, EverythingToolbar.Properties.Resources.MessageBoxErrorTitle)
                            .ShowDialogAsync();
                    }
                }
            }
        }
    }
}
