using System;
using EverythingToolbar.App.Data;
using EverythingToolbar.App.Helpers;
using EverythingToolbar.Core.Platform;
using EverythingToolbar.Core.Search;
using NLog;

namespace EverythingToolbar.App.Search
{
    public sealed class SearchResultActions(
        IEverythingClient everything,
        IClipboard clipboard,
        IShellDialogs shellDialogs,
        INotifier notifier,
        IFileLauncher fileLauncher,
        IFilePreviewer previewer,
        SearchState searchState,
        EverythingSearchLauncher launcher
    )
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchResultActions>();

        public void Open(SearchResult r)
        {
            try
            {
                fileLauncher.Open(r.FullPathAndFileName, r.Path);
                everything.IncrementRunCount(r.FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                notifier.ShowError("MessageBoxFailedToOpen", e.Message);
            }
        }

        public void RunAsAdmin(SearchResult r)
        {
            try
            {
                fileLauncher.OpenAsAdmin(r.FullPathAndFileName);
                everything.IncrementRunCount(r.FullPathAndFileName);
            }
            catch (OperationCanceledException)
            {
                // The user dismissed the UAC elevation prompt; nothing to run and nothing to report.
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                notifier.ShowError("MessageBoxFailedToOpen", e.Message);
            }
        }

        public void OpenPath(SearchResult r)
        {
            try
            {
                shellDialogs.OpenParentFolderAndSelect(r.FullPathAndFileName);
                everything.IncrementRunCount(r.FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open path.");
                notifier.ShowError("MessageBoxFailedToOpenPath", e.Message);
            }
        }

        public void OpenWith(SearchResult r)
        {
            try
            {
                shellDialogs.OpenWith(r.FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open dialog.");
                notifier.ShowError("MessageBoxFailedToOpenDialog", e.Message);
            }
        }

        public void CopyToClipboard(SearchResult r)
        {
            try
            {
                clipboard.SetFileDropList(new[] { r.FullPathAndFileName });
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to copy file.");
                notifier.ShowError("MessageBoxFailedToCopyFile", e.Message);
            }
        }

        public void CopyPathToClipboard(SearchResult r)
        {
            try
            {
                clipboard.SetText(r.FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to copy path.");
                notifier.ShowError("MessageBoxFailedToCopyPath", e.Message);
            }
        }

        public void ShowProperties(SearchResult r)
        {
            shellDialogs.ShowFileProperties(r.FullPathAndFileName);
        }

        public void ShowWindowsContextMenu(SearchResult r)
        {
            shellDialogs.ShowWindowsContextMenu(r.FullPathAndFileName);
        }

        public void ShowInEverything(SearchResult r)
        {
            launcher.OpenSearchInEverything(searchState, filenameToHighlight: r.FullPathAndFileName);
        }

        public void Preview(SearchResult r)
        {
            previewer.PreviewInQuickLook(r.FullPathAndFileName);
            previewer.PreviewInSeer(r.FullPathAndFileName);
        }
    }
}
