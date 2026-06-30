using System.IO;
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

        [Option(DefaultValue = false)]
        bool IsSystemContextMenuDefault { get; set; }

        [Option(DefaultValue = false)]
        bool IsPreviewPaneEnabled { get; set; }

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

        [Option(DefaultValue = "")]
        string UILanguage { get; set; }

        [Option(DefaultValue = false)]
        bool TaskbarWindowEnabled { get; set; }

        [Option(DefaultValue = "Left")]
        string TaskbarWindowAlignment { get; set; }
    }

    public abstract class ToolbarSettings
    {
        private static readonly IToolbarSettings UserSettings = new ConfigurationBuilder<IToolbarSettings>()
            .UseIniFile(Path.Combine(ConfigPaths.GetConfigDirectory(), "settings.ini"))
            .Build();

        public static readonly ISettings User = SettingsProxy.Create(UserSettings);
    }
}
