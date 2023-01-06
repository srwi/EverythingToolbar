using EverythingToolbar.Helpers;
using NLog;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace EverythingToolbar
{
    public class SearchResult
    {
        private static readonly ILogger _logger = ToolbarLogger.GetLogger<SearchResult>();

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
                Process.Start(new ProcessStartInfo(FullPathAndFileName)
                {
                    UseShellExecute = true,
                    WorkingDirectory = IsFile ? Path : FullPathAndFileName
                });
                EverythingSearch.Instance.IncrementRunCount(FullPathAndFileName);
                EverythingSearch.Instance.SearchTerm = null;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to open search result.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToOpen, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
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
                EverythingSearch.Instance.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to open search result.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToOpen, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenPath()
        {
            try
            {
                ShellUtils.OpenPathWithDefaultApp(FullPathAndFileName);
                EverythingSearch.Instance.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to open path.");
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
                _logger.Error(e, "Failed to open dialog.");
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
                _logger.Error(e, "Failed to copy file.");
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
                _logger.Error(e, "Failed to copy path.");
                MessageBox.Show(Properties.Resources.MessageBoxFailedToCopyPath, Properties.Resources.MessageBoxErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowProperties()
        {
            ShellUtils.ShowFileProperties(FullPathAndFileName);
        }

        public void ShowWindowsContexMenu()
        {
            ShowWindowsContexMenu(System.Windows.Forms.Control.MousePosition);
        }

        public void ShowWindowsContexMenu(System.Drawing.Point pos)
        {
            Peter.ShellContextMenu menu = new Peter.ShellContextMenu();
            FileInfo[] arrFI = new FileInfo[1];
            arrFI[0] = new FileInfo(FullPathAndFileName);
            menu.ShowContextMenu(arrFI, pos);
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
                            writer.WriteLine($"{"QuickLook.App.PipeMessages.Toggle"}|{FullPathAndFileName}");
                            writer.Flush();
                        }
                    }
                }
                catch (TimeoutException)
                {
                    _logger.Info("Opening QuickLook preview timed out. Is QuickLook running?");
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to open preview.");
                }
            });
        }
    }
}
