using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Helpers
{
    class ShellUtils
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<ShellUtils>();

        private ShellUtils() { }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        public static void ShowFileProperties(string path)
        {
            var info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = path;
            info.nShow = 5;
            info.fMask = 12;
            ShellExecuteEx(ref info);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        public static void CreateProcessFromCommandLine(string commandLine, string workingDirectory = null)
        {
            var si = new STARTUPINFO();

            CreateProcess(
                null,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                workingDirectory,
                ref si,
                out var _);
        }

        public static void OpenWithDialog(string path)
        {
            var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            args += ",OpenAs_RunDLL " + path;
            Process.Start("rundll32.exe", args);
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        public static void OpenParentFolderAndSelect(string path)
        {
            var parentFolder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentFolder))
                return;

            SHParseDisplayName(parentFolder, IntPtr.Zero, out var nativeFolder, 0, out _);
            if (nativeFolder == IntPtr.Zero)
                return;

            var itemToSelect = Path.GetFileName(path);
            SHParseDisplayName(Path.Combine(parentFolder, itemToSelect), IntPtr.Zero, out var nativeFile, 0, out _);

            var fileArray = new[] { nativeFile == IntPtr.Zero ? nativeFolder : nativeFile };
            SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

            Marshal.FreeCoTaskMem(nativeFolder);
            if (nativeFile != IntPtr.Zero) Marshal.FreeCoTaskMem(nativeFile);
        }
    }
}