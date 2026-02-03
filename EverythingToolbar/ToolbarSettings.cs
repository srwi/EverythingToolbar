using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Config.Net;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar
{
    public interface IToolbarSettings
    {
        [Option(DefaultValue = false)]
        bool IsMatchCase { get; set; }

        [Option(DefaultValue = false)]
        bool IsRegExEnabled { get; set; }

        [Option(DefaultValue = FocusBehavior.Repeat)]
        FocusBehavior ListFocusBehavior { get; set; }

        [Option(DefaultValue = false)]
        bool IsMatchPath { get; set; }

        [Option(DefaultValue = 1)]
        int SortBy { get; set; }

        [Option(DefaultValue = false)]
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
        bool IsAutoApplyCustomActions { get; set; }

        [Option(DefaultValue = 3)]
        int MaxTabItems { get; set; }

        [Option(DefaultValue = "")]
        string FilterOrder { get; set; }

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
        bool IsHomeEndNavigateResults { get; set; }



        [Option(DefaultValue = true)]
        bool IsSearchAsYouType { get; set; }

        [Option(DefaultValue = false)]
        bool IsForceCenterAlignment { get; set; }

        [Option(DefaultValue = false)]
        bool IsDoubleClickToOpen { get; set; }

        [Option(DefaultValue = false)]
        bool ForceWin10Theme { get; set; }

        [Option(DefaultValue = "")]
        string ThemeOverride { get; set; }

        [Option(DefaultValue = "")]
        string VersionBeforeUpdate { get; set; }
    }

    public sealed class ToolbarSettingsWrapper(IToolbarSettings settings) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsMatchCase
        {
            get => settings.IsMatchCase;
            set
            {
                if (settings.IsMatchCase != value)
                {
                    settings.IsMatchCase = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRegExEnabled
        {
            get => settings.IsRegExEnabled;
            set
            {
                if (settings.IsRegExEnabled != value)
                {
                    settings.IsRegExEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMatchPath
        {
            get => settings.IsMatchPath;
            set
            {
                if (settings.IsMatchPath != value)
                {
                    settings.IsMatchPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SortBy
        {
            get => settings.SortBy;
            set
            {
                if (settings.SortBy != value)
                {
                    settings.SortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSortDescending
        {
            get => settings.IsSortDescending;
            set
            {
                if (settings.IsSortDescending != value)
                {
                    settings.IsSortDescending = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMatchWholeWord
        {
            get => settings.IsMatchWholeWord;
            set
            {
                if (settings.IsMatchWholeWord != value)
                {
                    settings.IsMatchWholeWord = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PopupHeight
        {
            get => settings.PopupHeight;
            set
            {
                if (settings.PopupHeight != value)
                {
                    settings.PopupHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PopupWidth
        {
            get => settings.PopupWidth;
            set
            {
                if (settings.PopupWidth != value)
                {
                    settings.PopupWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public string EverythingPath
        {
            get => settings.EverythingPath;
            set
            {
                if (settings.EverythingPath != value)
                {
                    settings.EverythingPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ItemTemplate
        {
            get => settings.ItemTemplate;
            set
            {
                if (settings.ItemTemplate != value)
                {
                    settings.ItemTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAutoApplyCustomActions
        {
            get => settings.IsAutoApplyCustomActions;
            set
            {
                if (settings.IsAutoApplyCustomActions != value)
                {
                    settings.IsAutoApplyCustomActions = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxTabItems
        {
            get => settings.MaxTabItems;
            set
            {
                if (settings.MaxTabItems != value)
                {
                    settings.MaxTabItems = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FilterOrder
        {
            get => settings.FilterOrder;
            set
            {
                if (settings.FilterOrder != value)
                {
                    settings.FilterOrder = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FiltersPath
        {
            get => settings.FiltersPath;
            set
            {
                if (settings.FiltersPath != value)
                {
                    settings.FiltersPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsImportFilters
        {
            get => settings.IsImportFilters;
            set
            {
                if (settings.IsImportFilters != value)
                {
                    settings.IsImportFilters = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ShortcutModifiers
        {
            get => settings.ShortcutModifiers;
            set
            {
                if (settings.ShortcutModifiers != value)
                {
                    settings.ShortcutModifiers = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ShortcutKey
        {
            get => settings.ShortcutKey;
            set
            {
                if (settings.ShortcutKey != value)
                {
                    settings.ShortcutKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAnimationsDisabled
        {
            get => settings.IsAnimationsDisabled;
            set
            {
                if (settings.IsAnimationsDisabled != value)
                {
                    settings.IsAnimationsDisabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHideEmptySearchResults
        {
            get => settings.IsHideEmptySearchResults;
            set
            {
                if (settings.IsHideEmptySearchResults != value)
                {
                    settings.IsHideEmptySearchResults = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowResultsCount
        {
            get => settings.IsShowResultsCount;
            set
            {
                if (settings.IsShowResultsCount != value)
                {
                    settings.IsShowResultsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowQuickToggles
        {
            get => settings.IsShowQuickToggles;
            set
            {
                if (settings.IsShowQuickToggles != value)
                {
                    settings.IsShowQuickToggles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEnableHistory
        {
            get => settings.IsEnableHistory;
            set
            {
                if (settings.IsEnableHistory != value)
                {
                    settings.IsEnableHistory = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsReplaceStartMenuSearch
        {
            get => settings.IsReplaceStartMenuSearch;
            set
            {
                if (settings.IsReplaceStartMenuSearch != value)
                {
                    settings.IsReplaceStartMenuSearch = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRememberFilter
        {
            get => settings.IsRememberFilter;
            set
            {
                if (settings.IsRememberFilter != value)
                {
                    settings.IsRememberFilter = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastFilter
        {
            get => settings.LastFilter;
            set
            {
                if (settings.LastFilter != value)
                {
                    settings.LastFilter = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsThumbnailsEnabled
        {
            get => settings.IsThumbnailsEnabled;
            set
            {
                if (settings.IsThumbnailsEnabled != value)
                {
                    settings.IsThumbnailsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InstanceName
        {
            get => settings.InstanceName;
            set
            {
                if (settings.InstanceName != value)
                {
                    settings.InstanceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IconName
        {
            get => settings.IconName;
            set
            {
                settings.IconName = value;
                OnPropertyChanged();
            }
        }

        public string SkippedUpdate
        {
            get => settings.SkippedUpdate;
            set
            {
                if (settings.SkippedUpdate != value)
                {
                    settings.SkippedUpdate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUpdateNotificationsEnabled
        {
            get => settings.IsUpdateNotificationsEnabled;
            set
            {
                if (settings.IsUpdateNotificationsEnabled != value)
                {
                    settings.IsUpdateNotificationsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSetupAssistantDisabled
        {
            get => settings.IsSetupAssistantDisabled;
            set
            {
                if (settings.IsSetupAssistantDisabled != value)
                {
                    settings.IsSetupAssistantDisabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsTrayIconEnabled
        {
            get => settings.IsTrayIconEnabled;
            set
            {
                if (settings.IsTrayIconEnabled != value)
                {
                    settings.IsTrayIconEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAutoSelectFirstResult
        {
            get => settings.IsAutoSelectFirstResult;
            set
            {
                if (settings.IsAutoSelectFirstResult != value)
                {
                    settings.IsAutoSelectFirstResult = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsHomeEndNavigateResults
        {
            get => settings.IsHomeEndNavigateResults;
            set
            {
                if (settings.IsHomeEndNavigateResults != value)
                {
                    settings.IsHomeEndNavigateResults = value;
                    OnPropertyChanged();
                }
            }
        }



        public FocusBehavior ListFocusBehavior
        {
            get => settings.ListFocusBehavior;
            set
            {
                if (settings.ListFocusBehavior != value)
                {
                    settings.ListFocusBehavior = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSearchAsYouType
        {
            get => settings.IsSearchAsYouType;
            set
            {
                if (settings.IsSearchAsYouType != value)
                {
                    settings.IsSearchAsYouType = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsForceCenterAlignment
        {
            get => settings.IsForceCenterAlignment;
            set
            {
                if (settings.IsForceCenterAlignment != value)
                {
                    settings.IsForceCenterAlignment = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDoubleClickToOpen
        {
            get => settings.IsDoubleClickToOpen;
            set
            {
                if (settings.IsDoubleClickToOpen != value)
                {
                    settings.IsDoubleClickToOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ForceWin10Behavior
        {
            get => settings.ForceWin10Theme;
            set
            {
                if (settings.ForceWin10Theme != value)
                {
                    settings.ForceWin10Theme = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ThemeOverride
        {
            get => settings.ThemeOverride;
            set
            {
                if (settings.ThemeOverride != value)
                {
                    settings.ThemeOverride = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VersionBeforeUpdate
        {
            get => settings.VersionBeforeUpdate;
            set
            {
                if (settings.VersionBeforeUpdate != value)
                {
                    settings.VersionBeforeUpdate = value;
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

        public static readonly ToolbarSettingsWrapper User = new(UserSettings);
    }
}
