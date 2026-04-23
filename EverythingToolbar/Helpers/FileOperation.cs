using System;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Helpers
{
    /// <summary>
    /// Helper class for file operations using Windows Shell API
    /// Supports move to recycle bin and permanent deletion
    /// </summary>
    internal class FileOperation
    {
        [Flags]
        internal enum FileOperationFlags : ushort
        {
            FOF_MULTIDESTFILES = 0x0001,
            FOF_CONFIRMMOUSE = 0x0002,
            FOF_SILENT = 0x0004,
            FOF_RENAMEONCOLLISION = 0x0008,
            FOF_NOCONFIRMATION = 0x0010,
            FOF_WANTMAPPINGHANDLE = 0x0020,
            FOF_ALLOWUNDO = 0x0040,
            FOF_FILESONLY = 0x0080,
            FOF_SIMPLEPROGRESS = 0x0100,
            FOF_NOCONFIRMMKDIR = 0x0200,
            FOF_NOERRORUI = 0x0400,
            FOF_NOCOPYSECURITYATTRIBS = 0x0800,
            FOF_NORECURSION = 0x1000,
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            FOF_WANT_MAPPED_HANDLE = 0x0020,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        internal struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;

            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;

            public string pFrom;
            public string pTo;
            public FileOperationFlags fFlags;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;

            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        private const int FO_DELETE = 3;

        public void DeleteItem(string path, FileOperationFlags flags)
        {
            var fileOp = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = path + "\0\0",
                fFlags = flags | FileOperationFlags.FOF_SILENT,
            };

            var result = SHFileOperation(ref fileOp);
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to delete file. Windows error code: {result}"
                );
            }
        }
    }
}