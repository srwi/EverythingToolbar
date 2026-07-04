using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Res = EverythingToolbar.Properties.Resources;

namespace EverythingToolbar.Launcher.Settings
{
    public partial class TaskbarIntegration : INotifyPropertyChanged
    {
        public bool IsTaskbarIconPinned => Utils.IsTaskbarPinned();

        public bool ShowTaskbarWindowSettings =>
            Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11;

        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(Res.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(Res.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        public bool AllowLeftAlignment => Helpers.Utils.IsTaskbarCenterAligned();

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

        private bool _isWindowsSearchHidden = !Helpers.Utils.GetWindowsSearchEnabledState();
        public bool IsWindowsSearchHidden
        {
            get => _isWindowsSearchHidden;
            set
            {
                if (_isWindowsSearchHidden != value)
                {
                    _isWindowsSearchHidden = value;
                    Helpers.Utils.SetWindowsSearchEnabledState(!value);
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

    public class IconItem
    {
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
