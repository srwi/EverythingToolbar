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
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SetupAssistant>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool WindowsSearchHidden
        {
            get => !Helpers.Utils.GetWindowsSearchEnabledState();
            set
            {
                Helpers.Utils.SetWindowsSearchEnabledState(!value);
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

        /// <summary>
        /// The taskbar search box is only available on Windows 11 and later.
        /// </summary>
        public bool IsTaskbarWindowSupported =>
            Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11;

        /// <summary>
        /// The preferences pane unlocks once the icon is pinned OR the user opts into the
        /// taskbar search box (a full alternative to pinning), so setup can complete either way.
        /// </summary>
        public bool PreferencesUnlocked =>
            CurrentStep == 1 || (IsTaskbarWindowSupported && ToolbarSettings.User.TaskbarWindowEnabled);

        /// <summary>
        /// True once the taskbar icon is pinned. Drives the "Pin to taskbar" option's status
        /// indicator and its instructions/image (shown only while not yet pinned).
        /// </summary>
        public bool IsPinned => CurrentStep == 1;

        // The two options read as an either/or choice, so pick one and the other grays out.
        // Only gray out in the XOR case: if both are already active (or neither is), leave both
        // enabled so the user can freely toggle either without a box appearing disabled.
        public bool IsPinOptionAvailable => IsPinned || !ToolbarSettings.User.TaskbarWindowEnabled;
        public bool IsWindowOptionAvailable => ToolbarSettings.User.TaskbarWindowEnabled || !IsPinned;

        // Display text is localized; the stored value stays the invariant "Left"/"Right".
        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(EverythingToolbar.Properties.Resources.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        public bool AllowLeftAlignment => Helpers.Utils.IsTaskbarCenterAligned();

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
            // A left-aligned Windows taskbar makes "Left" alignment meaningless, so force "Right".
            if (!AllowLeftAlignment && ToolbarSettings.User.TaskbarWindowAlignment == "Left")
                ToolbarSettings.User.TaskbarWindowAlignment = "Right";

            InitializeComponent();

            DataContext = this;

            _icon = icon;
            _icon.Visible = false;

            ToolbarSettings.User.PropertyChanged += OnToolbarSettingsChanged;

            CreateFileWatcher(_taskbarShortcutPath);

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
            if (e.PropertyName == nameof(ToolbarSettings.User.TaskbarWindowEnabled))
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
                Dispatcher.Invoke(() =>
                {
                    CurrentStep = 1;
                });
            };
            _watcher.Deleted += (_, _) =>
            {
                _iconUpdateRequired = false;
                Dispatcher.Invoke(() =>
                {
                    CurrentStep = 0;
                });
            };
        }

        // While neither option is active (setup not yet completed either way), the preferences
        // section is disabled. Clicking it wiggles the option cards to point the user back to the
        // choice they still need to make.
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
            // A user who enabled the taskbar search box has completed setup via the alternative
            // path, so no "you have not pinned" warning and no forced tray icon.
            if (CurrentStep == 0 && !(IsTaskbarWindowSupported && ToolbarSettings.User.TaskbarWindowEnabled))
            {
                var result = await FluentMessageBox
                    .CreateYesNo(
                        Properties.Resources.SetupAssistantDisableWarningText,
                        Properties.Resources.SetupAssistantDisableWarningTitle
                    )
                    .ShowDialogAsync();
                var disableSetupAssistant = result == MessageBoxResult.Primary;
                if (disableSetupAssistant)
                {
                    ToolbarSettings.User.IsSetupAssistantDisabled = disableSetupAssistant;
                    // Ensuring the user can access the setup assistant
                    ToolbarSettings.User.IsTrayIconEnabled = disableSetupAssistant;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else if (_iconUpdateRequired)
            {
                ToolbarSettings.User.IconName = Utils.GetThemedAppIconPath();
            }
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            ToolbarSettings.User.PropertyChanged -= OnToolbarSettingsChanged;

            _icon.Visible = ToolbarSettings.User.IsTrayIconEnabled;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
