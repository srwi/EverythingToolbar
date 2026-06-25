using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.DependencyInjection;
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

    public class UserInterfaceViewModel : INotifyPropertyChanged
    {
        public SearchOptions SearchOptions { get; } = Ioc.Default.GetRequiredService<SearchOptions>();
        public ThemeOptions ThemeOptions { get; } = Ioc.Default.GetRequiredService<ThemeOptions>();
        public LanguageOptions LanguageOptions { get; } = Ioc.Default.GetRequiredService<LanguageOptions>();

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
            get => LanguageOptions.UILanguage;
            set
            {
                if (LanguageOptions.UILanguage != value)
                {
                    LanguageOptions.UILanguage = value;
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
            )
            {
                Icon = imageSource,
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
