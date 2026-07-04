using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using EverythingToolbar.Controls;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace EverythingToolbar.Settings
{
    public partial class UserInterface
    {
        public UserInterface()
        {
            InitializeComponent();
            DataContext = new UserInterfaceViewModel();
        }
    }

    public class IconItem
    {
        public string DisplayName { get; set; } = "";
        public string IconPath { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class UserInterfaceViewModel : INotifyPropertyChanged
    {
        public List<KeyValuePair<string, string>> ItemTemplates { get; } =
            [
                new(Resources.ItemTemplateCompact, "Compact"),
                new(Resources.ItemTemplateCompactDetailed, "CompactDetailed"),
                new(Resources.ItemTemplateNormal, "Normal"),
                new(Resources.ItemTemplateNormalDetailed, "NormalDetailed"),
            ];
        public List<KeyValuePair<string, string>> Languages { get; } = CultureHelper.GetAvailableLanguages();

        public string SelectedLanguage
        {
            get => ToolbarSettings.User.UILanguage;
            set
            {
                if (ToolbarSettings.User.UILanguage != value)
                {
                    ToolbarSettings.User.UILanguage = value;
                    OnPropertyChanged();
                    OnUILanguageChanged();
                }
            }
        }

        // Display text is localized; the stored value stays the invariant "Left"/"Right"
        // (compared literally in TaskbarWindow.CalculateHorizontalPosition).
        public List<KeyValuePair<string, string>> TaskbarWindowAlignmentOptions { get; } =
            [
                new(Resources.SettingsTaskbarWindowAlignmentLeft, "Left"),
                new(Resources.SettingsTaskbarWindowAlignmentRight, "Right"),
            ];

        // "Left" alignment only has a distinct effect when the Windows taskbar is center-aligned;
        // with a left-aligned taskbar it collapses to the same placement as "Right". In that case
        // the setting is forced to "Right" (in the constructor) and the combo box is disabled.
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

        public SearchResult SampleSearchResult { get; }

        public IconItem? SelectedIconItem
        {
            get => IconItems.FirstOrDefault(item => item.Value == ToolbarSettings.User.IconName);
            set
            {
                if (value != null)
                {
                    ToolbarSettings.User.IconName = value.Value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIconItem)));
                }
            }
        }

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
            IsLauncher && Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11;

        public UserInterfaceViewModel()
        {
            // A left-aligned Windows taskbar makes "Left" alignment meaningless, so force "Right".
            if (!AllowLeftAlignment && ToolbarSettings.User.TaskbarWindowAlignment == "Left")
                ToolbarSettings.User.TaskbarWindowAlignment = "Right";

            BitmapImage imageSource = new(
                new Uri("pack://application:,,,/EverythingToolbar;component/Images/AppIcon.ico")
            );
            SampleSearchResult = new SearchResult
            {
                HighlightedPath = @"C:\Program Files\EverythingToolbar\Everything*Toolbar*.exe",
                HighlightedFileName = "Everything*Toolbar*",
                IsFile = true,
                FileSize = 12345678,
                Icon = imageSource,
                DateModified = new FILETIME
                {
                    dwHighDateTime = DateTimeToFileTime(DateTime.Now).dwHighDateTime,
                    dwLowDateTime = DateTimeToFileTime(DateTime.Now).dwLowDateTime,
                },
            };
        }

        private static FILETIME DateTimeToFileTime(DateTime dateTime)
        {
            long fileTime = dateTime.ToFileTimeUtc();
            return new FILETIME
            {
                dwLowDateTime = (int)(fileTime & 0xFFFFFFFF),
                dwHighDateTime = (int)(fileTime >> 32),
            };
        }

        private async void OnUILanguageChanged()
        {
            var result = await FluentMessageBox
                .CreateYesNo(Resources.MessageBoxRestartMessage, Resources.MessageBoxRestartTitle)
                .ShowDialogAsync();

            if (result != MessageBoxResult.Primary)
                return;

            string? executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            if (IsLauncher && executablePath != null)
            {
                // Start a new instance with a delay to allow the current one to exit and release the Mutex
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c timeout /t 1 /nobreak && start \"\" \"{executablePath}\"",
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                    }
                );
            }

            // Always restart explorer to provide consistent visual feedback/refresh
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("explorer"))
            {
                process.Kill();
            }

            if (IsLauncher)
            {
                Application.Current.Shutdown();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
