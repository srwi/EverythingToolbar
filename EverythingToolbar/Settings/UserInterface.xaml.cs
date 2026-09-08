using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Controls;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Icons;
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
            var viewModel = new UserInterfaceViewModel();
            DataContext = viewModel;
            ColorPickerPopup.DataContext = viewModel;
        }

        private void OnColorPickerButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is UIElement target)
            {
                ColorPickerPopup.PlacementTarget = target;
                ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
            }
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
        public List<KeyValuePair<string, string>> SearchWindowBackgroundOptions { get; } =
        [
            new(Resources.SearchWindowBackgroundAutomatic, "Automatic"),
            new(Resources.SearchWindowBackgroundCustom, "Custom"),
        ];

        public string SearchWindowBackground
        {
            get => Settings.SearchWindowBackground;
            set
            {
                if (Settings.SearchWindowBackground != value)
                {
                    Settings.SearchWindowBackground = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCustomColorSelected));
                }
            }
        }

        public bool IsCustomColorSelected => Settings.SearchWindowBackground == "Custom";

        public int CustomColorAlpha
        {
            get => Settings.SearchWindowBackgroundAlpha;
            set
            {
                int clamped = Math.Clamp(value, 0, 255);
                if (Settings.SearchWindowBackgroundAlpha != clamped)
                {
                    Settings.SearchWindowBackgroundAlpha = clamped;
                }
            }
        }

        public int CustomColorBrightness
        {
            get => Settings.SearchWindowBackgroundBrightness;
            set
            {
                int clamped = Math.Clamp(value, 0, 100);
                if (Settings.SearchWindowBackgroundBrightness != clamped)
                {
                    Settings.SearchWindowBackgroundBrightness = clamped;
                }
            }
        }

        private SolidColorBrush _customColorBrush = ColorHelper.ToFrozenBrush(ColorHelper.DefaultSearchWindowColor);
        public SolidColorBrush CustomColorBrush
        {
            get => _customColorBrush;
            private set => SetProperty(ref _customColorBrush, value);
        }

        private string _customColorHex = ColorHelper.ToHex(ColorHelper.DefaultSearchWindowColor);
        public string CustomColorHex
        {
            get => _customColorHex;
            private set => SetProperty(ref _customColorHex, value);
        }

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
            UpdateBrushAndHex();

            var themeService = Ioc.Default.GetService<ThemeService>();
            if (themeService != null)
            {
                themeService.ThemeChanged += (s, e) => UpdateBrushAndHex();
            }

            Settings.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ISettings.SearchWindowBackground):
                        OnPropertyChanged(nameof(SearchWindowBackground));
                        OnPropertyChanged(nameof(IsCustomColorSelected));
                        break;
                    case nameof(ISettings.SearchWindowBackgroundAlpha):
                        OnPropertyChanged(nameof(CustomColorAlpha));
                        UpdateBrushAndHex();
                        break;
                    case nameof(ISettings.SearchWindowBackgroundBrightness):
                        OnPropertyChanged(nameof(CustomColorBrightness));
                        UpdateBrushAndHex();
                        break;
                    case nameof(ISettings.ThemeOverride):
                        UpdateBrushAndHex();
                        break;
                }
            };

            BitmapImage imageSource = new(
                new Uri("pack://application:,,,/EverythingToolbar;component/Images/AppIcon.ico")
            );
            SampleSearchResult = new SearchResult(
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
            );

            ResultImageCache.Get(SampleSearchResult).SetFixedIcon(imageSource);
        }

        private void UpdateBrushAndHex()
        {
            var themeService = Ioc.Default.GetService<ThemeService>();
            var isLight = themeService?.IsLightTheme() ?? false;
            var color = ColorHelper.GetSearchWindowColor(
                Settings.SearchWindowBackgroundAlpha,
                Settings.SearchWindowBackgroundBrightness,
                isLight
            );
            CustomColorBrush = ColorHelper.ToFrozenBrush(color);
            CustomColorHex = ColorHelper.ToHex(color);
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
