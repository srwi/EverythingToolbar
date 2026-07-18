using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using EverythingToolbar.Core.Platform;
using NLog;

namespace EverythingToolbar.Services
{
    public sealed class FilePreviewerAdapter : IFilePreviewer
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<FilePreviewerAdapter>();

        public void PreviewInQuickLook(string filePath)
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
                    writer.WriteLine($"QuickLook.App.PipeMessages.Toggle|{filePath}");
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

        public void PreviewInSeer(string filePath)
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
                        cbData = (filePath.Length + 1) * 2,
                        lpData = Marshal.StringToHGlobalUni(filePath),
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
