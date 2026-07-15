using System;
using System.Runtime.InteropServices;
using NLog;

namespace EverythingToolbar.Services
{
    public sealed class LowLevelKeyboardHook : IDisposable
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<LowLevelKeyboardHook>();

        private NativeMethods.LowLevelKeyboardProc? _callback;
        private IntPtr _hookId = IntPtr.Zero;

        private const int WhKeyboardLl = 13;
        private const int WmKeydown = 0x0100;
        private const int WmSyskeydown = 0x0104;
        private const int LlkhfInjected = 0x10;

        public LowLevelKeyboardHook(Func<int, bool, bool, bool> onKeyEvent)
        {
            OnKeyEvent = onKeyEvent ?? throw new ArgumentNullException(nameof(onKeyEvent));
        }

        public Func<int, bool, bool, bool> OnKeyEvent { get; }

        public void Install()
        {
            if (_hookId != IntPtr.Zero)
                return;

            _callback = HookCallback;
            _hookId = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _callback, IntPtr.Zero, 0);

            if (_hookId == IntPtr.Zero)
            {
                _callback = null;
                Logger.Error("Failed to install the low-level keyboard hook.");
            }
        }

        public void Uninstall()
        {
            if (_hookId == IntPtr.Zero)
                return;

            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _callback = null;
        }

        public void Dispose()
        {
            Uninstall();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

            var vk = Marshal.ReadInt32(lParam);
            var message = (int)wParam;
            var isDown = message is WmKeydown or WmSyskeydown;
            var flags = Marshal.ReadInt32(lParam, 8);
            var isInjected = (flags & LlkhfInjected) != 0;

            var swallow = OnKeyEvent(vk, isDown, isInjected);

            return swallow ? (IntPtr)1 : NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
