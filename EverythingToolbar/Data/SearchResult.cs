using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using NLog;
using Peter;
using Clipboard = System.Windows.Clipboard;
using DataObject = System.Windows.DataObject;
using MessageBox = System.Windows.MessageBox;

namespace EverythingToolbar.Data
{
    public class SearchResult
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchResult>();

        public bool IsFile { get; set; }

        public string FullPathAndFileName { get; set; }

        public string Path => System.IO.Path.GetDirectoryName(FullPathAndFileName);

        public string HighlightedPath { get; set; }

        public string FileName => System.IO.Path.GetFileName(FullPathAndFileName);

        public string HighlightedFileName { get; set; }

        public long FileSize { get; set; }

        public FILETIME DateModified { get; set; }

        public string HumanReadableFileSize
        {
            get
            {
                if (!IsFile || FileSize < 0)
                    return string.Empty;

                return Utils.GetHumanReadableFileSize(FileSize);
            }
        }

        public string HumanReadableDateModified
        {
            get
            {
                var dateModified = ((long)DateModified.dwHighDateTime << 32) | (uint)DateModified.dwLowDateTime;
                return DateTime.FromFileTime(dateModified).ToString("g");
            }
        }

        public ImageSource Icon => WindowsThumbnailProvider.GetThumbnail(FullPathAndFileName, 16, 16);

        public void Open()
        {
            try
            {
                Process.Start(new ProcessStartInfo(FullPathAndFileName)
                {
                    WorkingDirectory = Path
                });
                EverythingSearch.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                MessageBox.Show(Resources.MessageBoxFailedToOpen, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RunAsAdmin()
        {
            try
            {
                Process.Start(new ProcessStartInfo(FullPathAndFileName)
                {
                    Verb = "runas",
                    UseShellExecute = true
                });
                EverythingSearch.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                MessageBox.Show(Resources.MessageBoxFailedToOpen, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenPath()
        {
            try
            {
                ShellUtils.OpenParentFolderAndSelect(FullPathAndFileName);
                EverythingSearch.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open path.");
                MessageBox.Show(Resources.MessageBoxFailedToOpenPath, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
                Logger.Error(e, "Failed to open dialog.");
                MessageBox.Show(Resources.MessageBoxFailedToOpenDialog, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyToClipboard()
        {
            try
            {
                var dataObj = new DataObject();
                dataObj.SetFileDropList(new StringCollection { FullPathAndFileName });
                Clipboard.SetDataObject(dataObj, copy: false);  // Fixes #362
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to copy file.");
                MessageBox.Show(Resources.MessageBoxFailedToCopyFile, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyPathToClipboard()
        {
            try
            {
                var dataObj = new DataObject();
                dataObj.SetText(FullPathAndFileName);
                Clipboard.SetDataObject(dataObj, copy: false); // Fixes #362
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to copy path.");
                MessageBox.Show(Resources.MessageBoxFailedToCopyPath, Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowProperties()
        {
            ShellUtils.ShowFileProperties(FullPathAndFileName);
        }

        public void ShowWindowsContextMenu()
        {
            var menu = new ShellContextMenu();
            var arrFI = new FileInfo[1];
            arrFI[0] = new FileInfo(FullPathAndFileName);
            menu.ShowContextMenu(arrFI, Control.MousePosition);
        }

        public void ShowInEverything()
        {
            EverythingSearch.Instance.OpenLastSearchInEverything(FullPathAndFileName);
        }

        public void PreviewInQuickLook()
        {
            Task.Run(() =>
            {
                try
                {
                    using (var client = new NamedPipeClientStream(".", "QuickLook.App.Pipe." + WindowsIdentity.GetCurrent().User?.Value, PipeDirection.Out))
                    {
                        client.Connect(1000);

                        using (var writer = new StreamWriter(client))
                        {
                            writer.WriteLine($"QuickLook.App.PipeMessages.Toggle|{FullPathAndFileName}");
                            writer.Flush();
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Logger.Info("Opening QuickLook preview timed out. Is QuickLook running?");
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to open preview.");
                }
            });
        }
    }
}
