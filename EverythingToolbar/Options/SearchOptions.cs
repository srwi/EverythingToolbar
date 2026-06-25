using System.ComponentModel;
using System.Runtime.CompilerServices;
using EverythingToolbar.Data;

namespace EverythingToolbar
{
    public sealed class SearchOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SearchOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsSearchAsYouType) or nameof(IsHomeEndNavigateResults)
                    or nameof(IsShowQuickToggles) or nameof(IsAutoSelectFirstResult)
                    or nameof(ListFocusBehavior) or nameof(IsDoubleClickToOpen)
                    or nameof(IsSystemContextMenuDefault) or nameof(IsHideEmptySearchResults)
                    or nameof(IsEnableHistory) or nameof(IsShowResultsCount)
                    or nameof(IsPreviewPaneEnabled))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsSearchAsYouType
        {
            get => ToolbarSettings.User.IsSearchAsYouType;
            set => ToolbarSettings.User.IsSearchAsYouType = value;
        }

        public bool IsHomeEndNavigateResults
        {
            get => ToolbarSettings.User.IsHomeEndNavigateResults;
            set => ToolbarSettings.User.IsHomeEndNavigateResults = value;
        }

        public bool IsShowQuickToggles
        {
            get => ToolbarSettings.User.IsShowQuickToggles;
            set => ToolbarSettings.User.IsShowQuickToggles = value;
        }

        public bool IsAutoSelectFirstResult
        {
            get => ToolbarSettings.User.IsAutoSelectFirstResult;
            set => ToolbarSettings.User.IsAutoSelectFirstResult = value;
        }

        public FocusBehavior ListFocusBehavior
        {
            get => ToolbarSettings.User.ListFocusBehavior;
            set => ToolbarSettings.User.ListFocusBehavior = value;
        }

        public bool IsDoubleClickToOpen
        {
            get => ToolbarSettings.User.IsDoubleClickToOpen;
            set => ToolbarSettings.User.IsDoubleClickToOpen = value;
        }

        public bool IsSystemContextMenuDefault
        {
            get => ToolbarSettings.User.IsSystemContextMenuDefault;
            set => ToolbarSettings.User.IsSystemContextMenuDefault = value;
        }

        public bool IsHideEmptySearchResults
        {
            get => ToolbarSettings.User.IsHideEmptySearchResults;
            set => ToolbarSettings.User.IsHideEmptySearchResults = value;
        }

        public bool IsEnableHistory
        {
            get => ToolbarSettings.User.IsEnableHistory;
            set => ToolbarSettings.User.IsEnableHistory = value;
        }

        public bool IsShowResultsCount
        {
            get => ToolbarSettings.User.IsShowResultsCount;
            set => ToolbarSettings.User.IsShowResultsCount = value;
        }

        public bool IsPreviewPaneEnabled
        {
            get => ToolbarSettings.User.IsPreviewPaneEnabled;
            set => ToolbarSettings.User.IsPreviewPaneEnabled = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}