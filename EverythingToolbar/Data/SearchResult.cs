using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using EverythingToolbar.Search;
using NLog;
using Peter;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Clipboard = System.Windows.Clipboard;
using DataObject = System.Windows.DataObject;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using MessageBox = System.Windows.MessageBox;

namespace EverythingToolbar.Data
{
    public class SearchResult : INotifyPropertyChanged
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
                long dateModified = ((long)DateModified.dwHighDateTime << 32) | (uint)DateModified.dwLowDateTime;
                return DateTime.FromFileTime(dateModified).ToString("g");
            }
        }

        private ImageSource _icon;
        public ImageSource Icon
        {
            get
            {
                if (_icon != null)
                    return _icon;

                string[] imageExtensions =
                {
                    ".png",
                    ".jpg",
                    ".jpeg",
                    ".gif",
                    ".bmp",
                    ".tiff",
                    ".ico"
                };
                string ext = System.IO.Path.GetExtension(FullPathAndFileName).ToLowerInvariant();
                if (ToolbarSettings.User.IsThumbnailsEnabled && imageExtensions.Contains(ext) && File.Exists(FullPathAndFileName))
                {
                    _icon = IconProvider.GetImage(FullPathAndFileName);
                    Task.Run(() =>
                    {
                        Icon = ThumbnailProvider.GetImage(FullPathAndFileName);
                    });
                }
                else
                {
                    _icon = IconProvider.GetImage(FullPathAndFileName, source =>
                    {
                        Icon = source;
                    });
                }

                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public void Open()
        {
            try
            {
                var path = FullPathAndFileName;
                if (Directory.Exists(FullPathAndFileName))
                {
                    // We need to make sure directories end with a slash. Otherwise executables with the same stem
                    // might be executed instead due to how Process.Start prioritizes executables when resolving filenames.
                    path += "\\";
                }
                Process.Start(new ProcessStartInfo(path)
                {
                    WorkingDirectory = Path
                });
                SearchResultProvider.IncrementRunCount(FullPathAndFileName);
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
                    Verb = "runas"
                });
                SearchResultProvider.IncrementRunCount(FullPathAndFileName);
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
                SearchResultProvider.IncrementRunCount(FullPathAndFileName);
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
            SearchResultProvider.OpenSearchInEverything(SearchState.Instance, filenameToHighlight: FullPathAndFileName);
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
                    Logger.Error(e, "Failed to open QuickLook preview.");
                }
            });
        }

        public void PreviewInSeer()
        {
            Task.Run(() =>
            {
                try
                {
                    var seer = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "SeerWindowClass", null);

                    const int SEER_INVOKE_W32 = 5000;
                    const int WM_COPYDATA = 0x004A;

                    var cd = new NativeMethods.Copydatastruct
                    {
                        cbData = (FullPathAndFileName.Length + 1) * 2,
                        lpData = Marshal.StringToHGlobalUni(FullPathAndFileName),
                        dwData = new IntPtr(SEER_INVOKE_W32)
                    };

                    NativeMethods.SendMessage(seer, WM_COPYDATA, IntPtr.Zero, ref cd);

                    Marshal.FreeHGlobal(cd.lpData);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to open Seer preview.");
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}