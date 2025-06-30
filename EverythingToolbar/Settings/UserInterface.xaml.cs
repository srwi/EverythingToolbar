using EverythingToolbar.Data;
using EverythingToolbar.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media.Imaging;
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

    public class UserInterfaceViewModel : INotifyPropertyChanged
    {
        public List<KeyValuePair<string, string>> ItemTemplates { get; } =
        [
            new(Resources.ItemTemplateCompact, "Compact"),
            new(Resources.ItemTemplateCompactDetailed, "CompactDetailed"),
            new(Resources.ItemTemplateNormal, "Normal"),
            new(Resources.ItemTemplateNormalDetailed, "NormalDetailed")
        ];

        public SearchResult SampleSearchResult { get; }

        public UserInterfaceViewModel()
        {
            BitmapImage imageSource = new(new Uri("pack://application:,,,/EverythingToolbar;component/Images/AppIcon.ico"));
            SampleSearchResult = new SearchResult {
                HighlightedPath = @"C:\Program Files\EverythingToolbar\Everything*Toolbar*.exe",
                HighlightedFileName = "Everything*Toolbar*",
                IsFile = true,
                FileSize = 12345678,
                Icon = imageSource,
                DateModified = new FILETIME {
                    dwHighDateTime = DateTimeToFileTime(DateTime.Now).dwHighDateTime,
                    dwLowDateTime = DateTimeToFileTime(DateTime.Now).dwLowDateTime
                },
            };
        }

        private static FILETIME DateTimeToFileTime(DateTime dateTime)
        {
            long fileTime = dateTime.ToFileTimeUtc();
            return new FILETIME
            {
                dwLowDateTime = (int)(fileTime & 0xFFFFFFFF),
                dwHighDateTime = (int)(fileTime >> 32)
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}