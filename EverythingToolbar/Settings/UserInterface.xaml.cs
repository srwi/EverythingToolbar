using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Controls;
using EverythingToolbar.Core.Data;
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

    public partial class UserInterfaceViewModel : ObservableObject
    {
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();

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
            get => Settings.UILanguage;
            set
            {
                if (Settings.UILanguage != value)
                {
                    Settings.UILanguage = value;
                    OnPropertyChanged();
                    OnUILanguageChanged();
                }
            }
        }

        public SearchResult SampleSearchResult { get; }

        public bool IsLauncher => Application.Current != null;

        public UserInterfaceViewModel()
        {
            BitmapImage imageSource = new(
                new Uri("pack://application:,,,/EverythingToolbar;component/Images/AppIcon.ico")
            );
            SampleSearchResult = new SearchResult(
                new SearchResultData(
                    HighlightedPath: @"C:\Program Files\EverythingToolbar\Everything*Toolbar*.exe",
                    HighlightedFileName: "Everything*Toolbar*",
                    FullPathAndFileName: @"C:\Program Files\EverythingToolbar\EverythingToolbar.exe",
                    IsFile: true,
                    FileSize: 12345678,
                    DateModified: new FILETIME
                    {
                        dwHighDateTime = DateTimeToFileTime(DateTime.Now).dwHighDateTime,
                        dwLowDateTime = DateTimeToFileTime(DateTime.Now).dwLowDateTime,
                    }
                )
            );

            ResultImageCache.Get(SampleSearchResult).SetFixedIcon(imageSource);
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

    }
}