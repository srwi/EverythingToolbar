using System.ComponentModel;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.App
{
    public interface ISettings : INotifyPropertyChanged
    {
        bool IsMatchCase { get; set; }
        bool IsRegExEnabled { get; set; }
        bool IsMatchPath { get; set; }
        bool IsMatchWholeWord { get; set; }
        int SortBy { get; set; }
        bool IsSortDescending { get; set; }
        int PopupHeight { get; set; }
        int PopupWidth { get; set; }
        string EverythingPath { get; set; }
        string ItemTemplate { get; set; }
        bool IsAutoApplyCustomActions { get; set; }
        int MaxTabItems { get; set; }
        string FilterOrder { get; set; }
        string FiltersPath { get; set; }
        bool IsImportFilters { get; set; }
        int ShortcutModifiers { get; set; }
        int ShortcutKey { get; set; }
        bool IsAnimationsDisabled { get; set; }
        bool IsHideEmptySearchResults { get; set; }
        bool IsShowResultsCount { get; set; }
        bool IsShowQuickToggles { get; set; }
        bool IsEnableHistory { get; set; }
        bool IsReplaceStartMenuSearch { get; set; }
        bool IsRememberFilter { get; set; }
        string LastFilter { get; set; }
        bool IsThumbnailsEnabled { get; set; }
        bool IsSystemContextMenuDefault { get; set; }
        bool IsPreviewPaneEnabled { get; set; }
        string InstanceName { get; set; }
        string IconName { get; set; }
        string SkippedUpdate { get; set; }
        bool IsUpdateNotificationsEnabled { get; set; }
        bool IsSetupAssistantDisabled { get; set; }
        bool IsTrayIconEnabled { get; set; }
        bool IsAutoSelectFirstResult { get; set; }
        bool IsHomeEndNavigateResults { get; set; }
        FocusBehavior ListFocusBehavior { get; set; }
        bool IsSearchAsYouType { get; set; }
        bool IsForceCenterAlignment { get; set; }
        bool IsDoubleClickToOpen { get; set; }
        bool ForceWin10Behavior { get; set; }
        string ThemeOverride { get; set; }
        string VersionBeforeUpdate { get; set; }
        string UILanguage { get; set; }
        bool TaskbarWindowEnabled { get; set; }
        string TaskbarWindowAlignment { get; set; }
    }
}