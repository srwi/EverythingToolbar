using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using NLog;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EverythingToolbar.Launcher
{
    public partial class SetupAssistant : INotifyPropertyChanged
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private readonly NotifyIcon _icon;
        private bool _iconUpdateRequired;
        private FileSystemWatcher? _watcher;
        private RegistryValueWatcher? _taskbarAlignmentWatcher;
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
        private readonly TaskbarWindowOptions _taskbarWindowOptions = Ioc.Default.GetRequiredService<TaskbarWindowOptions>();
        private readonly LauncherOptions _launcherOptions = Ioc.Default.GetRequiredService<LauncherOptions>();
        private readonly IconOptions _iconOptions = Ioc.Default.GetRequiredService<IconOptions>();
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SetupAssistant>();

        public TaskbarWindowOptions TaskbarWindowOptions => _taskbarWindowOptions;
        public LauncherOptions LauncherOptions => _launcherOptions;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool WindowsSearchHidden
        {
            get => !Helpers.SystemSettings.GetWindowsSearchEnabledState();
            set
            {
                Helpers.SystemSettings.SetWindowsSearchEnabledState(!value);
                OnPropertyChanged();
            }
        }

        public bool AutostartEnabled
        {
            get => Utils.GetAutostartState();
            set
            {
                Utils.SetAutostartState(value);
                OnPropertyChanged();
            }
        }

        public bool IsTaskbarWindowSupported =>
            _windowsPolicy.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11;

        public bool PreferencesUnlocked =>
            CurrentStep == 1 || (IsTaskbarWindowSupported && _taskbarWindowOptions.TaskbarWindowEnabled);

        public bool IsPinned => CurrentStep == 1;

        public bool IsPinOptionAvailable => IsPinned || !_taskbarWindowOptions.TaskbarWindowEnabled;
        public bool IsWindowOptionAvailable => _taskbarWindowOptions.TaskbarWindowEnabled || !IsPinned;

        // Display text is localized; the stored value stays the invariant "Left"/"Right".
        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        public bool AllowLeftAlignment => _windowsPolicy.IsTaskbarCenterAligned();

        private int _currentStep;
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPinned));
                    OnPropertyChanged(nameof(IsPinOptionAvailable));
                    OnPropertyChanged(nameof(IsWindowOptionAvailable));
                    OnPropertyChanged(nameof(PreferencesUnlocked));
                }
            }
        }

        public SetupAssistant(NotifyIcon icon)
        {
            if (!AllowLeftAlignment && _taskbarWindowOptions.TaskbarWindowAlignment == "Left")
                _taskbarWindowOptions.TaskbarWindowAlignment = "Right";

            InitializeComponent();

            const double edgeMargin = 40;
            double available = SystemParameters.WorkArea.Width - edgeMargin;
            Width = Math.Min(IsTaskbarWindowSupported ? 960 : 600, available);

            DataContext = this;

            _icon = icon;
            _icon.Visible = false;

            _taskbarWindowOptions.PropertyChanged += OnToolbarSettingsChanged;

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
            if (e.PropertyName == nameof(_taskbarWindowOptions.TaskbarWindowEnabled))
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

            _taskbarAlignmentWatcher = new Helpers.RegistryValueWatcher(
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
            if (CurrentStep == 0 && !(IsTaskbarWindowSupported && _taskbarWindowOptions.TaskbarWindowEnabled))
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
                    _launcherOptions.IsSetupAssistantDisabled = disableSetupAssistant;
                    // Ensuring the user can access the setup assistant
                    _launcherOptions.IsTrayIconEnabled = disableSetupAssistant;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else if (_iconUpdateRequired)
            {
                _iconOptions.IconName = Utils.GetThemedAppIconPath();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _taskbarWindowOptions.PropertyChanged -= OnToolbarSettingsChanged;

            _icon.Visible = _launcherOptions.IsTrayIconEnabled;

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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}