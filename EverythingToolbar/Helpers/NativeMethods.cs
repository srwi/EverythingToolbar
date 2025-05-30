﻿using System;
using System.Runtime.InteropServices;
using NLog;

namespace EverythingToolbar.Helpers
{
    public class NativeMethods
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<NativeMethods>();

        public static void FocusTaskbarWindow()
        {
            var taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle != IntPtr.Zero)
            {
                ForciblySetForegroundWindow(taskbarHandle);
            }
        }

        public static void ForciblySetForegroundWindow(IntPtr handle)
        {
            var success = SetForegroundWindow(handle);
            if (success)
                return;

            Logger.Debug("SetForegroundWindow failed, trying to force window to front...");

            var foregroundWindow = GetForegroundWindow();
            var foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            var currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId == currentThreadId)
                return;

            AttachThreadInput(foregroundThreadId, currentThreadId, true);
            SetForegroundWindow(handle);
            AttachThreadInput(foregroundThreadId, currentThreadId, false);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref Copydatastruct lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [StructLayout(LayoutKind.Sequential)]
        public struct Copydatastruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }
    }
}
