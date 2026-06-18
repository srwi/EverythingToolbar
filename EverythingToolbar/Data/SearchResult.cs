using System;
using System.Collections.Generic;
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
using System.Windows.Forms;
using System.Windows.Media;
using EverythingToolbar.Controls;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using EverythingToolbar.Search;
using NLog;
using Peter;
using Clipboard = System.Windows.Clipboard;
using DataObject = System.Windows.DataObject;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Data
{
    public class SearchResult : INotifyPropertyChanged
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchResult>();
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".bmp",
            ".tiff",
            ".ico",
            ".webp",
        };

        public bool IsFile { get; init; }

        public string FullPathAndFileName { get; init; }

        public string Path => System.IO.Path.GetDirectoryName(FullPathAndFileName) ?? "";

        public string HighlightedPath { get; set; }

        public string FileName => System.IO.Path.GetFileName(FullPathAndFileName);

        public string HighlightedFileName { get; set; }

        public long FileSize { get; init; }

        public FILETIME DateModified { get; init; }

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

        private ImageSource? _icon;
        private ImageSource? _previewImage;
        private const int IconSize = 16;
        private const int PreviewIconSize = 64;
        private const int PreviewThumbnailSize = 380;
        public ImageSource? Icon
        {
            get
            {
                if (_icon != null)
                    return _icon;

                if (ToolbarSettings.User.IsThumbnailsEnabled && IsImageFile && File.Exists(FullPathAndFileName))
                {
                    _icon = IconProvider.GetImage(FullPathAndFileName, IsFile, IconSize);

                    Task.Run(() =>
                    {
                        Icon = ThumbnailProvider.GetImage(FullPathAndFileName, IconSize);
                    });
                }
                else
                {
                    _icon = IconProvider.GetImage(
                        FullPathAndFileName,
                        IsFile,
                        32,
                        source =>
                        {
                            Icon = source;
                        }
                    );
                }

                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        public ImageSource? PreviewImage
        {
            get
            {
                if (_previewImage != null)
                    return _previewImage;

                var requiresThumbnail = IsImageFile && File.Exists(FullPathAndFileName);

                // We always load the regular icon first, independent of whether the file requires a thumbnail preview
                Task.Run(() =>
                {
                    Action<ImageSource>? onExactIconLoaced = null;
                    if (!requiresThumbnail)
                    {
                        onExactIconLoaced = source =>
                        {
                            PreviewImage = source;
                        };
                    }

                    ImageSource? image = IconProvider.GetImage(
                        FullPathAndFileName,
                        IsFile,
                        PreviewIconSize,
                        onExactIconLoaced
                    );
                    if (image != null && _previewImage == null)
                        PreviewImage = image;
                });

                // If needed, update the preview with a thumbnail later
                if (requiresThumbnail)
                {
                    Task.Run(() =>
                    {
                        ImageSource? image = ThumbnailProvider.GetImage(
                            FullPathAndFileName,
                            PreviewThumbnailSize,
                            allowUpscaling: false
                        );
                        if (image != null)
                            PreviewImage = image;
                    });
                }

                return _previewImage;
            }
            private set
            {
                _previewImage = value;
                OnPropertyChanged();
            }
        }

        private bool IsImageFile => ImageExtensions.Contains(System.IO.Path.GetExtension(FullPathAndFileName));

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
                Process.Start(new ProcessStartInfo(path) { WorkingDirectory = Path, UseShellExecute = true });
                SearchResultProvider.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToOpen, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
            }
        }

        public void RunAsAdmin()
        {
            try
            {
                Process.Start(new ProcessStartInfo(FullPathAndFileName) { Verb = "runas", UseShellExecute = true });
                SearchResultProvider.IncrementRunCount(FullPathAndFileName);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to open search result.");
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToOpen, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
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
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToOpenPath, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
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
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToOpenDialog, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
            }
        }

        public void CopyToClipboard()
        {
            try
            {
                var dataObj = new DataObject();
                dataObj.SetFileDropList(new StringCollection { FullPathAndFileName });
                Clipboard.SetDataObject(dataObj, copy: false); // Fixes #362
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to copy file.");
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToCopyFile, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
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
                FluentMessageBox
                    .CreateError(Resources.MessageBoxFailedToCopyPath, Resources.MessageBoxErrorTitle)
                    .ShowDialogAsync();
            }
        }

        public void ShowProperties()
        {
            ShellUtils.ShowFileProperties(FullPathAndFileName);
        }

        public void ShowWindowsContextMenu()
        {
            ShowWindowsContextMenu(new[] { this });
        }

        public static void ShowWindowsContextMenu(IEnumerable<SearchResult> items)
        {
            var itemsList = items.ToList();
            if (itemsList.Count == 0) return;

            var firstItemDir = itemsList[0].Path;
            var validItems = itemsList.Where(i => string.Equals(i.Path, firstItemDir, StringComparison.OrdinalIgnoreCase)).ToList();

            var menu = new ShellContextMenu();
            var arrFi = validItems.Select<SearchResult, FileSystemInfo>(i =>
                i.IsFile ? new FileInfo(i.FullPathAndFileName) : new DirectoryInfo(i.FullPathAndFileName)
            ).ToArray();

            menu.ShowContextMenu(arrFi, Control.MousePosition);
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
                    using var client = new NamedPipeClientStream(
                        ".",
                        "QuickLook.App.Pipe." + WindowsIdentity.GetCurrent().User?.Value,
                        PipeDirection.Out
                    );
                    client.Connect(1000);

                    using var writer = new StreamWriter(client);
                    writer.WriteLine($"QuickLook.App.PipeMessages.Toggle|{FullPathAndFileName}");
                    writer.Flush();
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

                    const int seerInvokeW32 = 5000;
                    const int wmCopydata = 0x004A;

                    var cd = new NativeMethods.Copydatastruct
                    {
                        cbData = (FullPathAndFileName.Length + 1) * 2,
                        lpData = Marshal.StringToHGlobalUni(FullPathAndFileName),
                        dwData = new IntPtr(seerInvokeW32),
                    };

                    NativeMethods.SendMessage(seer, wmCopydata, IntPtr.Zero, ref cd);

                    Marshal.FreeHGlobal(cd.lpData);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to open Seer preview.");
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
