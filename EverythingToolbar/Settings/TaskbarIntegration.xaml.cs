using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Settings
{
    public partial class TaskbarIntegration : INotifyPropertyChanged
    {
        public bool IsLauncher => Application.Current != null;

        /// <summary>
        /// Provides the taskbar pin state. Set by the launcher (which owns the Shell logic to detect
        /// pinning); left null in hosts where the taskbar-icon section is not shown anyway.
        /// </summary>
        public static Func<bool>? GetTaskbarPinnedCallback { get; set; }

        /// <summary>
        /// The search icon only affects the pinned taskbar icon, so its option is disabled when the
        /// icon is not pinned. Defaults to enabled when the pin state cannot be determined.
        /// </summary>
        public bool IsTaskbarIconPinned => GetTaskbarPinnedCallback?.Invoke() ?? true;

        /// <summary>
        /// The taskbar window settings only apply to the launcher on Windows 11+, where the
        /// feature is actually effective. Hidden elsewhere to avoid a no-op toggle.
        /// </summary>
        public bool ShowTaskbarWindowSettings =>
            IsLauncher && Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11;

        // Display text is localized; the stored value stays the invariant "Left"/"Right"
        // (compared literally in TaskbarWindow.CalculateHorizontalPosition).
        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(Properties.Resources.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(Properties.Resources.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        // "Left" alignment only has a distinct effect when the Windows taskbar is center-aligned;
        // with a left-aligned taskbar it collapses to the same placement as "Right". In that case
        // the setting is forced to "Right" (in the constructor) and the combo box is disabled.
        public bool AllowLeftAlignment => Utils.IsTaskbarCenterAligned();

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
            get => IconItems.FirstOrDefault(item => item.Value == ToolbarSettings.User.IconName);
            set
            {
                if (value != null)
                {
                    ToolbarSettings.User.IconName = value.Value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isWindowsSearchHidden = !Utils.GetWindowsSearchEnabledState();
        public bool IsWindowsSearchHidden
        {
            get => _isWindowsSearchHidden;
            set
            {
                if (_isWindowsSearchHidden != value)
                {
                    _isWindowsSearchHidden = value;
                    Utils.SetWindowsSearchEnabledState(!value);
                    OnPropertyChanged();
                }
            }
        }

        public TaskbarIntegration()
        {
            // A left-aligned Windows taskbar makes "Left" alignment meaningless, so force "Right".
            if (!AllowLeftAlignment && ToolbarSettings.User.TaskbarWindowAlignment == "Left")
                ToolbarSettings.User.TaskbarWindowAlignment = "Right";

            InitializeComponent();
            DataContext = this;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
