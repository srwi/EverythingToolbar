using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar.Platform.Helpers
{
    public static class NativeMethods
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static IntPtr FindTaskbarHandle()
        {
            return FindWindow("Shell_TrayWnd", null);
        }

        public static IntPtr FindWindow(string lpClassName, string? lpWindowName)
        {
            return PInvoke.FindWindow(lpClassName, lpWindowName);
        }

        public static IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowTitle)
        {
            return PInvoke.FindWindowEx((HWND)parentHandle, (HWND)childAfter, className, windowTitle);
        }

        public static void FocusTaskbarWindow()
        {
            var taskbarHandle = FindTaskbarHandle();
            if (taskbarHandle != IntPtr.Zero)
            {
                ForciblySetForegroundWindow(taskbarHandle);
            }
        }

        public static void ForciblySetForegroundWindow(IntPtr handle)
        {
            if (PInvoke.SetForegroundWindow((HWND)handle))
            {
                PInvoke.SetActiveWindow((HWND)handle);
                return;
            }

            Logger.Debug("SetForegroundWindow failed, trying to force window to front...");

            var foregroundWindow = PInvoke.GetForegroundWindow();
            var foregroundThreadId = PInvoke.GetWindowThreadProcessId(foregroundWindow, out _);
            var targetThreadId = PInvoke.GetWindowThreadProcessId((HWND)handle, out _);

            if (foregroundThreadId != targetThreadId)
                PInvoke.AttachThreadInput(foregroundThreadId, targetThreadId, true);

            try
            {
                PInvoke.SetForegroundWindow((HWND)handle);
                PInvoke.SetActiveWindow((HWND)handle);
            }
            finally
            {
                if (foregroundThreadId != targetThreadId)
                    PInvoke.AttachThreadInput(foregroundThreadId, targetThreadId, false);
            }
        }

        public static bool FlashWindow(IntPtr hWnd, bool bInvert)
        {
            return PInvoke.FlashWindow((HWND)hWnd, bInvert);
        }

        public static IntPtr GetForegroundWindow()
        {
            return PInvoke.GetForegroundWindow();
        }

        public static uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
        {
            return PInvoke.GetWindowThreadProcessId((HWND)hWnd, out lpdwProcessId);
        }

        public static uint GetDpiForWindow(IntPtr hWnd)
        {
            return PInvoke.GetDpiForWindow((HWND)hWnd);
        }

        public static unsafe IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref CopyDataStruct lParam)
        {
            fixed (CopyDataStruct* p = &lParam)
            {
                return PInvoke.SendMessage((HWND)hWnd, msg, (WPARAM)(nuint)wParam, (LPARAM)(nint)p);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static readonly Dictionary<IntPtr, HOOKPROC> InstalledHooks = new();

        public static IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc? lpfn, IntPtr hMod, uint dwThreadId)
        {
            if (lpfn is null)
                return IntPtr.Zero;

            HOOKPROC proc = (code, wParam, lParam) =>
                (LRESULT)(nint)lpfn(code, (IntPtr)(nint)(nuint)wParam, (IntPtr)(nint)lParam);

            HHOOK hook = PInvoke.SetWindowsHookEx((WINDOWS_HOOK_ID)idHook, proc, (HINSTANCE)hMod, dwThreadId);
            IntPtr raw = hook;
            if (raw != IntPtr.Zero)
            {
                lock (InstalledHooks)
                    InstalledHooks[raw] = proc;
            }
            return raw;
        }

        public static bool UnhookWindowsHookEx(IntPtr hhk)
        {
            lock (InstalledHooks)
                InstalledHooks.Remove(hhk);

            return PInvoke.UnhookWindowsHookEx((HHOOK)hhk);
        }

        public static IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return PInvoke.CallNextHookEx((HHOOK)hhk, nCode, (WPARAM)(nuint)wParam, (LPARAM)(nint)lParam);
        }

        public static void SendKeybdEvent(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo)
        {
            PInvoke.keybd_event(bVk, bScan, (KEYBD_EVENT_FLAGS)dwFlags, (nuint)dwExtraInfo);
        }

        public static int DwmFlush()
        {
            return PInvoke.DwmFlush();
        }
    }
}
