using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Config.Net;
using EverythingToolbar.Helpers;

namespace EverythingToolbar
{
    public interface IToolbarSettings
    {
        [Option(DefaultValue = false)]
        bool IsMatchCase { get; set; }

        [Option(DefaultValue = false)]
        bool IsRegExEnabled { get; set; }

        [Option(DefaultValue = false)]
        bool IsMatchPath { get; set; }

        [Option(DefaultValue = 9)]  // Default is run count
        int SortBy { get; set; }

        [Option(DefaultValue = true)]
        bool IsSortDescending { get; set; }

        [Option(DefaultValue = false)]
        bool IsMatchWholeWord { get; set; }

        [Option(DefaultValue = 700)]
        int PopupHeight { get; set; }

        [Option(DefaultValue = 700)]
        int PopupWidth { get; set; }

        [Option(DefaultValue = "C:\\Program Files\\Everything\\Everything.exe")]
        string EverythingPath { get; set; }

        [Option(DefaultValue = "Normal")]
        string ItemTemplate { get; set; }

        [Option(DefaultValue = false)]
        bool IsAutoApplyRules { get; set; }

        [Option(DefaultValue = "")]
        string FiltersPath { get; set; }

        [Option(DefaultValue = false)]
        bool IsImportFilters { get; set; }

        [Option(DefaultValue = 9)]
        int ShortcutModifiers { get; set; }

        [Option(DefaultValue = 62)]
        int ShortcutKey { get; set; }

        [Option(DefaultValue = false)]
        bool IsAnimationsDisabled { get; set; }

        [Option(DefaultValue = false)]
        bool IsHideEmptySearchResults { get; set; }

        [Option(DefaultValue = false)]
        bool IsShowResultsCount { get; set; }

        [Option(DefaultValue = false)]
        bool IsShowQuickToggles { get; set; }

        [Option(DefaultValue = false)]
        bool IsEnableHistory { get; set; }

        [Option(DefaultValue = false)]
        bool IsReplaceStartMenuSearch { get; set; }

        [Option(DefaultValue = false)]
        bool IsRememberFilter { get; set; }

        [Option(DefaultValue = "")]
        string LastFilter { get; set; }

        [Option(DefaultValue = false)]
        bool IsThumbnailsEnabled { get; set; }

        [Option(DefaultValue = "")]
        string InstanceName { get; set; }

        [Option(DefaultValue = "")]
        string IconName { get; set; }

        [Option(DefaultValue = "0")]
        string SkippedUpdate { get; set; }

        [Option(DefaultValue = true)]
        bool IsUpdateNotificationsEnabled { get; set; }

        [Option(DefaultValue = false)]
        bool IsSetupAssistantDisabled { get; set; }

        [Option(DefaultValue = false)]
        bool IsTrayIconEnabled { get; set; }

        [Option(DefaultValue = true)]
        bool IsAutoSelectFirstResult { get; set; }

        [Option(DefaultValue = true)]
        bool IsSearchAsYouType { get; set; }

        [Option(DefaultValue = false)]
        bool IsForceCenterAlignment { get; set; }

        [Option(DefaultValue = false)]
        bool IsDoubleClickToOpen { get; set; }

        [Option(DefaultValue = 0)]
        int OsBuildNumberOverride { get; set; }

        [Option(DefaultValue = "")]
        string ThemeOverride { get; set; }
    }

    public sealed class ToolbarSettingsWrapper : INotifyPropertyChanged
    {
        private readonly IToolbarSettings _settings;

        public event PropertyChangedEventHandler PropertyChanged;

        public ToolbarSettingsWrapper(IToolbarSettings settings)
        {
            _settings = settings;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsMatchCase
        {
            get => _settings.IsMatchCase;
            set
            {
                if (_settings.IsMatchCase != value)
                {
                    _settings.IsMatchCase = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRegExEnabled
        {
            get => _settings.IsRegExEnabled;
            set
            {
                if (_settings.IsRegExEnabled != value)
                {
                    _settings.IsRegExEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMatchPath
        {
            get => _settings.IsMatchPath;
            set
            {
                if (_settings.IsMatchPath != value)
                {
                    _settings.IsMatchPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SortBy
        {
            get => _settings.SortBy;
            set
            {
                if (_settings.SortBy != value)
                {
                    _settings.SortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSortDescending
        {
            get => _settings.IsSortDescending;
            set
            {
                if (_settings.IsSortDescending != value)
                {
                    _settings.IsSortDescending = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMatchWholeWord
        {
            get => _settings.IsMatchWholeWord;
            set
            {
                if (_settings.IsMatchWholeWord != value)
                {
                    _settings.IsMatchWholeWord = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PopupHeight
        {
            get => _settings.PopupHeight;
            set
            {
                if (_settings.PopupHeight != value)
                {
                    _settings.PopupHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PopupWidth
        {
            get => _settings.PopupWidth;
            set
            {
                if (_settings.PopupWidth != value)
                {
                    _settings.PopupWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EverythingPath
        {
            get => _settings.EverythingPath;
            set
            {
                if (_settings.EverythingPath != value)
                {
                    _settings.EverythingPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemTemplate
        {
            get => _settings.ItemTemplate;
            set
            {
                if (_settings.ItemTemplate != value)
                {
                    _settings.ItemTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAutoApplyRules
        {
            get => _settings.IsAutoApplyRules;
            set
            {
                if (_settings.IsAutoApplyRules != value)
                {
                    _settings.IsAutoApplyRules = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FiltersPath
        {
            get => _settings.FiltersPath;
            set
            {
                if (_settings.FiltersPath != value)
                {
                    _settings.FiltersPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsImportFilters
        {
            get => _settings.IsImportFilters;
            set
            {
                if (_settings.IsImportFilters != value)
                {
                    _settings.IsImportFilters = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ShortcutModifiers
        {
            get => _settings.ShortcutModifiers;
            set
            {
                if (_settings.ShortcutModifiers != value)
                {
                    _settings.ShortcutModifiers = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ShortcutKey
        {
            get => _settings.ShortcutKey;
            set
            {
                if (_settings.ShortcutKey != value)
                {
                    _settings.ShortcutKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAnimationsDisabled
        {
            get => _settings.IsAnimationsDisabled;
            set
            {
                if (_settings.IsAnimationsDisabled != value)
                {
                    _settings.IsAnimationsDisabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHideEmptySearchResults
        {
            get => _settings.IsHideEmptySearchResults;
            set
            {
                if (_settings.IsHideEmptySearchResults != value)
                {
                    _settings.IsHideEmptySearchResults = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowResultsCount
        {
            get => _settings.IsShowResultsCount;
            set
            {
                if (_settings.IsShowResultsCount != value)
                {
                    _settings.IsShowResultsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowQuickToggles
        {
            get => _settings.IsShowQuickToggles;
            set
            {
                if (_settings.IsShowQuickToggles != value)
                {
                    _settings.IsShowQuickToggles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEnableHistory
        {
            get => _settings.IsEnableHistory;
            set
            {
                if (_settings.IsEnableHistory != value)
                {
                    _settings.IsEnableHistory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsReplaceStartMenuSearch
        {
            get => _settings.IsReplaceStartMenuSearch;
            set
            {
                if (_settings.IsReplaceStartMenuSearch != value)
                {
                    _settings.IsReplaceStartMenuSearch = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRememberFilter
        {
            get => _settings.IsRememberFilter;
            set
            {
                if (_settings.IsRememberFilter != value)
                {
                    _settings.IsRememberFilter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastFilter
        {
            get => _settings.LastFilter;
            set
            {
                if (_settings.LastFilter != value)
                {
                    _settings.LastFilter = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsThumbnailsEnabled
        {
            get => _settings.IsThumbnailsEnabled;
            set
            {
                if (_settings.IsThumbnailsEnabled != value)
                {
                    _settings.IsThumbnailsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InstanceName
        {
            get => _settings.InstanceName;
            set
            {
                if (_settings.InstanceName != value)
                {
                    _settings.InstanceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IconName
        {
            get => _settings.IconName;
            set
            {
                if (_settings.IconName != value)
                {
                    _settings.IconName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SkippedUpdate
        {
            get => _settings.SkippedUpdate;
            set
            {
                if (_settings.SkippedUpdate != value)
                {
                    _settings.SkippedUpdate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUpdateNotificationsEnabled
        {
            get => _settings.IsUpdateNotificationsEnabled;
            set
            {
                if (_settings.IsUpdateNotificationsEnabled != value)
                {
                    _settings.IsUpdateNotificationsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSetupAssistantDisabled
        {
            get => _settings.IsSetupAssistantDisabled;
            set
            {
                if (_settings.IsSetupAssistantDisabled != value)
                {
                    _settings.IsSetupAssistantDisabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTrayIconEnabled
        {
            get => _settings.IsTrayIconEnabled;
            set
            {
                if (_settings.IsTrayIconEnabled != value)
                {
                    _settings.IsTrayIconEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAutoSelectFirstResult
        {
            get => _settings.IsAutoSelectFirstResult;
            set
            {
                if (_settings.IsAutoSelectFirstResult != value)
                {
                    _settings.IsAutoSelectFirstResult = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSearchAsYouType
        {
            get => _settings.IsSearchAsYouType;
            set
            {
                if (_settings.IsSearchAsYouType != value)
                {
                    _settings.IsSearchAsYouType = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsForceCenterAlignment
        {
            get => _settings.IsForceCenterAlignment;
            set
            {
                if (_settings.IsForceCenterAlignment != value)
                {
                    _settings.IsForceCenterAlignment = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDoubleClickToOpen
        {
            get => _settings.IsDoubleClickToOpen;
            set
            {
                if (_settings.IsDoubleClickToOpen != value)
                {
                    _settings.IsDoubleClickToOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OsBuildNumberOverride
        {
            get => _settings.OsBuildNumberOverride;
            set
            {
                if (_settings.OsBuildNumberOverride != value)
                {
                    _settings.OsBuildNumberOverride = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ThemeOverride
        {
            get => _settings.ThemeOverride;
            set
            {
                if (_settings.ThemeOverride != value)
                {
                    _settings.ThemeOverride = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public abstract class ToolbarSettings
    {
        private static readonly IToolbarSettings UserSettings = new ConfigurationBuilder<IToolbarSettings>()
            .UseIniFile(Path.Combine(Utils.GetConfigDirectory(), "settings.ini"))
            .Build();

        public static readonly ToolbarSettingsWrapper User = new ToolbarSettingsWrapper(UserSettings);
    }
}