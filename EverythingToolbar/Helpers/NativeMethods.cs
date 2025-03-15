using System;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Helpers
{
    public class NativeMethods
    {
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
            var foregroundWindow = GetForegroundWindow();
            var foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            var currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(foregroundThreadId, currentThreadId, true);
                SetForegroundWindow(handle);
                AttachThreadInput(foregroundThreadId, currentThreadId, false);
            }
            else
            {
                SetForegroundWindow(handle);
            }
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
