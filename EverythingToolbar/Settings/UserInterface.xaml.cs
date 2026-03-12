using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

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
        public string DisplayName { get; set; }
        public string IconPath { get; set; }
        public string Value { get; set; }
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

        public UserInterfaceViewModel()
        {
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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
