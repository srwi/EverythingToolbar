using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Controls;
using NLog;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EverythingToolbar.Launcher
{
    [ObservableObject]
    public partial class SetupAssistant
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private readonly TrayIcon _icon;
        private bool _iconUpdateRequired;
        private FileSystemWatcher? _watcher;
        private RegistryValueWatcher? _taskbarAlignmentWatcher;
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly IAutostart _autostart = Ioc.Default.GetRequiredService<IAutostart>();
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SetupAssistant>();

        public ISettings Settings => _settings;

        [ObservableProperty]
        private bool _windowsSearchHidden = !SystemSettings.GetWindowsSearchEnabledState();

        partial void OnWindowsSearchHiddenChanged(bool value)
        {
            SystemSettings.SetWindowsSearchEnabledState(!value);
        }

        [ObservableProperty]
        private bool _autostartEnabled;

        partial void OnAutostartEnabledChanged(bool value)
        {
            _autostart.IsEnabled = value;
        }

        public bool IsTaskbarWindowSupported =>
            _windowsPolicy.GetWindowsVersion() >= WindowsVersion.Windows11;

        public bool PreferencesUnlocked =>
            CurrentStep == 1 || (IsTaskbarWindowSupported && _settings.TaskbarWindowEnabled);

        public bool IsPinned => CurrentStep == 1;

        public bool IsPinOptionAvailable => IsPinned || !_settings.TaskbarWindowEnabled;
        public bool IsWindowOptionAvailable => _settings.TaskbarWindowEnabled || !IsPinned;

        // Display text is localized; the stored value stays the invariant "Left"/"Right".
        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
        [
            new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentLeft, "Left"),
            new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentRight, "Right"),
        ];

        public bool AllowLeftAlignment => _windowsPolicy.IsTaskbarCenterAligned();

        [ObservableProperty]
        private int _currentStep;

        partial void OnCurrentStepChanged(int value)
        {
            OnPropertyChanged(nameof(IsPinned));
            OnPropertyChanged(nameof(IsPinOptionAvailable));
            OnPropertyChanged(nameof(IsWindowOptionAvailable));
            OnPropertyChanged(nameof(PreferencesUnlocked));
        }

        internal SetupAssistant(TrayIcon icon)
        {
            if (!AllowLeftAlignment && _settings.TaskbarWindowAlignment == "Left")
                _settings.TaskbarWindowAlignment = "Right";

            _autostartEnabled = _autostart.IsEnabled;

            InitializeComponent();

            const double edgeMargin = 40;
            double available = SystemParameters.WorkArea.Width - edgeMargin;
            Width = Math.Min(IsTaskbarWindowSupported ? 960 : 600, available);

            DataContext = this;

            _icon = icon;
            _icon.Hide();

            _settings.PropertyChanged += OnToolbarSettingsChanged;

            CreateFileWatcher(_taskbarShortcutPath);
            CreateTaskbarAlignmentWatcher();

            if (File.Exists(_taskbarShortcutPath))
            {
                Dispatcher.Invoke(() =>
                {
                    CurrentStep = 1;
                });
            }
            else
            {
                SetAppIcon();
                Loaded += (_, _) => FlashTaskbarIcon();
            }
        }

        private void OnToolbarSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.TaskbarWindowEnabled))
            {
                OnPropertyChanged(nameof(PreferencesUnlocked));
                OnPropertyChanged(nameof(IsPinOptionAvailable));
                OnPropertyChanged(nameof(IsWindowOptionAvailable));
            }
        }

        private void FlashTaskbarIcon()
        {
            NativeMethods.FlashWindow(new WindowInteropHelper(this).Handle, true);
        }

        private void CreateTaskbarAlignmentWatcher()
        {
            if (!IsTaskbarWindowSupported)
                return;

            _taskbarAlignmentWatcher = new RegistryValueWatcher(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
            );
            _taskbarAlignmentWatcher.Changed += OnTaskbarAlignmentChanged;
        }

        private void OnTaskbarAlignmentChanged()
        {
            Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(AllowLeftAlignment)));
        }

        private void SetAppIcon()
        {
            try
            {
                var iconPath = Utils.GetThemedAppIconPath();
                var iconUri = new Uri("pack://application:,,,/" + iconPath);
                Icon = new BitmapImage(iconUri);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to set icon for setup assistant.");
            }
        }

        private void CreateFileWatcher(string taskbarShortcutPath)
        {
            string pinnedIconName = Path.GetFileName(taskbarShortcutPath);
            if (Path.GetDirectoryName(taskbarShortcutPath) is not { } pinnedIconsDir)
            {
                Logger.Error("Failed to get directory name for taskbar shortcut path.");
                return;
            }

            try
            {
                // The directory might not exist on some systems (#523)
                Directory.CreateDirectory(pinnedIconsDir);
            }
            catch (UnauthorizedAccessException e)
            {
                Logger.Error(e, "Failed to create pinned taskbar icons directory.");
                return;
            }

            _watcher = new FileSystemWatcher
            {
                Path = pinnedIconsDir,
                Filter = pinnedIconName,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true,
            };

            _watcher.Created += (_, _) =>
            {
                _iconUpdateRequired = true;
                Dispatcher.BeginInvoke(() =>
                {
                    CurrentStep = 1;
                });
            };
            _watcher.Deleted += (_, _) =>
            {
                _iconUpdateRequired = false;
                Dispatcher.BeginInvoke(() =>
                {
                    CurrentStep = 0;
                });
            };
        }

        private void OnSecondStepClicked(object sender, MouseButtonEventArgs e)
        {
            if (!PreferencesUnlocked)
            {
                var storyboard = (Storyboard)FindResource("WiggleStoryboard");
                storyboard.Begin();
                FlashTaskbarIcon();
                e.Handled = true;
            }
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            if (CurrentStep == 0 && !(IsTaskbarWindowSupported && _settings.TaskbarWindowEnabled))
            {
                var result = await FluentMessageBox
                    .CreateYesNo(
                        Properties.Resources.SetupAssistantExitWarningText,
                        Properties.Resources.SetupAssistantDisableWarningTitle
                    )
                    .ShowDialogAsync();
                var disableSetupAssistant = result == MessageBoxResult.Primary;
                if (disableSetupAssistant)
                {
                    _settings.IsSetupAssistantDisabled = disableSetupAssistant;
                    // Ensuring the user can access the setup assistant
                    _settings.IsTrayIconEnabled = disableSetupAssistant;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else if (_iconUpdateRequired)
            {
                _settings.IconName = Utils.GetThemedAppIconPath();
                WeakReferenceMessenger.Default.Send(new TaskbarPinIconChanged(_settings.IconName));
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _settings.PropertyChanged -= OnToolbarSettingsChanged;

            if (_settings.IsTrayIconEnabled)
                _icon.Show();
            else
                _icon.Hide();

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }

            if (_taskbarAlignmentWatcher != null)
            {
                _taskbarAlignmentWatcher.Changed -= OnTaskbarAlignmentChanged;
                _taskbarAlignmentWatcher.Dispose();
                _taskbarAlignmentWatcher = null;
            }
        }
    }
}
