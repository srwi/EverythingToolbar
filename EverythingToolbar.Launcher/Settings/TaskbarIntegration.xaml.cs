using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using EverythingToolbar.Helpers;
using Res = EverythingToolbar.Properties.Resources;

namespace EverythingToolbar.Launcher.Settings
{
    public partial class TaskbarIntegration : INotifyPropertyChanged
    {
        private readonly string _taskbarShortcutPath = Utils.GetTaskbarShortcutPath();
        private FileSystemWatcher? _watcher;
        private RegistryValueWatcher? _taskbarAlignmentWatcher;
        private readonly WindowsPolicyService _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicyService>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();

        public ISettings Settings => _settings;

        private bool _isTaskbarIconPinned;

        public bool IsTaskbarIconPinned
        {
            get => _isTaskbarIconPinned;
            private set
            {
                if (_isTaskbarIconPinned != value)
                {
                    _isTaskbarIconPinned = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowTaskbarWindowSettings =>
            _windowsPolicy.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11;

        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(Res.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(Res.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        public bool AllowLeftAlignment => _windowsPolicy.IsTaskbarCenterAligned();

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

        public IconItem? SelectedIconItem
        {
            get => IconItems.FirstOrDefault(item => item.Value == _settings.IconName);
            set
            {
                if (value != null)
                {
                    _settings.IconName = value.Value;
                    WeakReferenceMessenger.Default.Send(new TaskbarPinIconChanged(value.Value));
                    OnPropertyChanged();
                }
            }
        }

        private bool _isWindowsSearchHidden = !SystemSettings.GetWindowsSearchEnabledState();
        public bool IsWindowsSearchHidden
        {
            get => _isWindowsSearchHidden;
            set
            {
                if (_isWindowsSearchHidden != value)
                {
                    _isWindowsSearchHidden = value;
                    SystemSettings.SetWindowsSearchEnabledState(!value);
                    OnPropertyChanged();
                }
            }
        }

        public TaskbarIntegration()
        {
            if (!AllowLeftAlignment && _settings.TaskbarWindowAlignment == "Left")
                _settings.TaskbarWindowAlignment = "Right";

            _isTaskbarIconPinned = File.Exists(_taskbarShortcutPath);

            InitializeComponent();
            DataContext = this;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IsTaskbarIconPinned = File.Exists(_taskbarShortcutPath);
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
            Dispatcher.BeginInvoke(() => OnPropertyChanged(nameof(AllowLeftAlignment)));
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

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class IconItem
    {
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";
        public string Value { get; init; } = "";
    }
}