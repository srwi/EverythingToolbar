using EverythingToolbar.Helpers;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace EverythingToolbar
{
    public class SearchResult
    {
        public bool IsFile { get; set; }

        public string FullPathAndFileName { get; set; }

        public string Path => System.IO.Path.GetDirectoryName(FullPathAndFileName);

        public string HighlightedPath { get; set; }

        public string FileName => System.IO.Path.GetFileName(FullPathAndFileName);

        public string HighlightedFileName { get; set; }

        public string FileSize => IsFile ? Utils.GetHumanReadableFileSize(FullPathAndFileName) : "";

        public string DateModified => File.GetLastWriteTime(FullPathAndFileName).ToString("g");

        public ImageSource Icon => WindowsThumbnailProvider.GetThumbnail(FullPathAndFileName, 16, 16);

        public void Open()
        {
            try
            {
                Process.Start(FullPathAndFileName);
                EverythingSearch.Instance.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to open search result.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToOpen, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenPath()
        {
            try
            {
                ShellUtils.CreateProcessFromCommandLine("explorer.exe /select,\"" + FullPathAndFileName + "\"");
                EverythingSearch.Instance.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to open path.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToOpenPath, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenWith()
        {
            try
            {
                ShellUtils.OpenWithDialog(FullPathAndFileName);
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to open dialog.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToOpenDialog, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyToClipboard()
        {
            try
            {
                Clipboard.SetFileDropList(new StringCollection { FullPathAndFileName });
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to copy file.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToCopyFile, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyPathToClipboard()
        {
            try
            {
                Clipboard.SetText(FullPathAndFileName);
            }
            catch (Exception e)
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Failed to copy path.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToCopyPath, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowProperties()
        {
            ShellUtils.ShowFileProperties(FullPathAndFileName);
        }

        public void ShowInEverything()
        {
            EverythingSearch.Instance.OpenLastSearchInEverything(FullPathAndFileName);
        }
    }
}
