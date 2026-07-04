using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace EverythingToolbar.Helpers
{
    public static class ShellUtils
    {
        public static void ShowFileProperties(string path)
        {
            unsafe
            {
                fixed (char* verb = "properties")
                fixed (char* file = path)
                {
                    var info = new SHELLEXECUTEINFOW
                    {
                        cbSize = (uint)sizeof(SHELLEXECUTEINFOW),
                        fMask = 12u, // SEE_MASK_INVOKEIDLIST
                        lpVerb = verb,
                        lpFile = file,
                        nShow = 5, // SW_SHOW
                    };
                    PInvoke.ShellExecuteEx(ref info);
                }
            }
        }

        public static void OpenWithDialog(string path)
        {
            var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            args += ",OpenAs_RunDLL " + path;
            Process.Start("rundll32.exe", args);
        }

        public static unsafe void OpenParentFolderAndSelect(string path)
        {
            var parentFolder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentFolder))
                return;

            PInvoke.SHParseDisplayName(parentFolder, null, out ITEMIDLIST* nativeFolder, 0, out _);
            if (nativeFolder == null)
                return;

            var itemToSelect = Path.GetFileName(path);
            PInvoke.SHParseDisplayName(Path.Combine(parentFolder, itemToSelect), null, out ITEMIDLIST* nativeFile, 0, out _);

            var fileToSelect = nativeFile != null ? nativeFile : nativeFolder;
            PInvoke.SHOpenFolderAndSelectItems(nativeFolder, 1, &fileToSelect, 0);

            Marshal.FreeCoTaskMem((IntPtr)nativeFolder);
            if (nativeFile != null)
                Marshal.FreeCoTaskMem((IntPtr)nativeFile);
        }
    }
}
