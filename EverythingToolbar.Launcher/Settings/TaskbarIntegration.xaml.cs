using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Res = EverythingToolbar.Properties.Resources;

namespace EverythingToolbar.Launcher.Settings
{
    [ObservableObject]
    public partial class TaskbarIntegration
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private FileSystemWatcher? _watcher;
        private RegistryValueWatcher? _taskbarAlignmentWatcher;
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        public ISettings Settings => _settings;

        [ObservableProperty]
        private bool _isTaskbarIconPinned;

        public bool ShowTaskbarWindowSettings => _windowsPolicy.GetWindowsVersion() >= WindowsVersion.Windows11;

        [ObservableProperty]
        private bool _canEnableTaskbarWindow;

        [ObservableProperty]
        private bool _allowLeftAlignment;

        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
        [new(Res.SettingsTaskbarWindowAlignmentLeft, "Left"), new(Res.SettingsTaskbarWindowAlignmentRight, "Right")];

        [ObservableProperty]
        private IconItem? _selectedIconItem;

        partial void OnSelectedIconItemChanged(IconItem? value)
        {
            if (value != null)
            {
                _settings.IconName = value.Value;
                WeakReferenceMessenger.Default.Send(new TaskbarPinIconChanged(value.Value));
            }
        }

        [ObservableProperty]
        private bool _isWindowsSearchHidden = !SystemSettings.GetWindowsSearchEnabledState();

        partial void OnIsWindowsSearchHiddenChanged(bool value)
        {
            SystemSettings.SetWindowsSearchEnabledState(!value);
        }

        public List<IconItem> IconItems { get; } =
        [
            new()
            {
                DisplayName = "Light",
                IconPath = "pack://siteoforigin:,,,/Icons/Dark.ico",
                Value = "Icons/Dark.ico",
            },
            new()
            {
                DisplayName = "Dark",
                IconPath = "pack://siteoforigin:,,,/Icons/Light.ico",
                Value = "Icons/Light.ico",
            },
            new()
            {
                DisplayName = "Blue",
                IconPath = "pack://siteoforigin:,,,/Icons/Medium.ico",
                Value = "Icons/Medium.ico",
            },
        ];

        public TaskbarIntegration()
        {
            _canEnableTaskbarWindow = _windowsPolicy.CanEnableTaskbarWindow();
            UpdateTaskbarWindowAlignment();

            _isTaskbarIconPinned = File.Exists(_taskbarShortcutPath);

            // Assign the field, not the property: the change handler would re-pin the taskbar icon on open.
            _selectedIconItem = IconItems.FirstOrDefault(item => item.Value == _settings.IconName);

            InitializeComponent();
            DataContext = this;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IsTaskbarIconPinned = File.Exists(_taskbarShortcutPath);
            CanEnableTaskbarWindow = _windowsPolicy.CanEnableTaskbarWindow();
            UpdateTaskbarWindowAlignment();

            CreateFileWatcher();
            CreateTaskbarAlignmentWatcher();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            if (_taskbarAlignmentWatcher != null)
            {
                _taskbarAlignmentWatcher.Changed -= OnTaskbarAlignmentChanged;
                _taskbarAlignmentWatcher.Dispose();
                _taskbarAlignmentWatcher = null;
            }
        }

        private void CreateTaskbarAlignmentWatcher()
        {
            if (!ShowTaskbarWindowSettings || _taskbarAlignmentWatcher != null)
                return;

            _taskbarAlignmentWatcher = new RegistryValueWatcher(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
            );
            _taskbarAlignmentWatcher.Changed += OnTaskbarAlignmentChanged;
        }

        private void OnTaskbarAlignmentChanged()
        {
            Dispatcher.BeginInvoke(UpdateTaskbarWindowAlignment);
        }

        private void UpdateTaskbarWindowAlignment()
        {
            AllowLeftAlignment = _windowsPolicy.IsTaskbarCenterAligned();
            if (!AllowLeftAlignment && _settings.TaskbarWindowAlignment == "Left")
                _settings.TaskbarWindowAlignment = "Right";
        }

        private void CreateFileWatcher()
        {
            if (_watcher != null)
                return;

            var pinnedIconName = Path.GetFileName(_taskbarShortcutPath);
            if (Path.GetDirectoryName(_taskbarShortcutPath) is not { } pinnedIconsDir)
                return;

            try
            {
                Directory.CreateDirectory(pinnedIconsDir);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            _watcher = new FileSystemWatcher
            {
                Path = pinnedIconsDir,
                Filter = pinnedIconName,
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true,
            };
            _watcher.Created += (_, _) => Dispatcher.BeginInvoke(() => IsTaskbarIconPinned = true);
            _watcher.Deleted += (_, _) => Dispatcher.BeginInvoke(() => IsTaskbarIconPinned = false);
        }
    }

    public class IconItem
    {
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";
        public string Value { get; init; } = "";
    }
}
