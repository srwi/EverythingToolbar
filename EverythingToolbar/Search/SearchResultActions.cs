using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Platform;
using NLog;

namespace EverythingToolbar.Search
{
    public sealed class SearchResultActions(
        IEverythingClient everything,
        IClipboard clipboard,
        IShellDialogs shellDialogs,
        INotifier notifier,
        IFileLauncher fileLauncher,
        SearchState searchState,
        EverythingSearchLauncher launcher)
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
            shellDialogs.ShowWindowsContextMenu(r.FullPathAndFileName, Control.MousePosition);
        }

        public void ShowInEverything(SearchResult r)
        {
            launcher.OpenSearchInEverything(searchState, filenameToHighlight: r.FullPathAndFileName);
        }

        public void PreviewInQuickLook(SearchResult r)
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
                    writer.WriteLine($"QuickLook.App.PipeMessages.Toggle|{r.FullPathAndFileName}");
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

        public void PreviewInSeer(SearchResult r)
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
                        cbData = (r.FullPathAndFileName.Length + 1) * 2,
                        lpData = Marshal.StringToHGlobalUni(r.FullPathAndFileName),
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
    }
}