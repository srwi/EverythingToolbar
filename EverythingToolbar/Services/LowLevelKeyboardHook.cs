using System;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar.Services
{
    public sealed class LowLevelKeyboardHook : IDisposable
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<LowLevelKeyboardHook>();

        private NativeMethods.LowLevelKeyboardProc? _callback;
        private IntPtr _hookId = IntPtr.Zero;
        private Thread? _hookThread;
        private uint _hookThreadId;

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
            if (_hookThread != null)
                return;

            var ready = new ManualResetEventSlim(false);
            _hookThread = new Thread(() => HookThreadProc(ready))
            {
                Name = "LowLevelKeyboardHook",
                IsBackground = true,
                Priority = ThreadPriority.Highest,
            };
            _hookThread.Start();
            ready.Wait();

            if (_hookId == IntPtr.Zero)
            {
                _hookThread.Join();
                _hookThread = null;
                Logger.Error("Failed to install the low-level keyboard hook.");
            }
        }

        public void Uninstall()
        {
            if (_hookThread == null)
                return;

            PInvoke.PostThreadMessage(_hookThreadId, PInvoke.WM_QUIT, default, default);
            _hookThread.Join();
            _hookThread = null;
            _hookThreadId = 0;
        }

        public void Dispose()
        {
            Uninstall();
        }

        private void HookThreadProc(ManualResetEventSlim ready)
        {
            _hookThreadId = PInvoke.GetCurrentThreadId();

            // Ensure the thread has a message queue before Install() returns so that
            // Uninstall() can always reach it via PostThreadMessage.
            PInvoke.PeekMessage(out _, HWND.Null, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE);

            OptOutOfPowerThrottling();

            _callback = HookCallback;
            _hookId = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _callback, IntPtr.Zero, 0);

            ready.Set();

            if (_hookId == IntPtr.Zero)
            {
                _callback = null;
                return;
            }

            while (PInvoke.GetMessage(out var msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(in msg);
                PInvoke.DispatchMessage(in msg);
            }

            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _callback = null;
        }

        private static unsafe void OptOutOfPowerThrottling()
        {
            // Windows 11 may put background processes into efficiency mode (EcoQoS). A
            // throttled hook thread risks missing the LowLevelHooksTimeout deadline after
            // the process has been idle, which makes Windows deliver the keystroke
            // un-swallowed as if the hook had not handled it.
            var state = new THREAD_POWER_THROTTLING_STATE
            {
                Version = PInvoke.THREAD_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PInvoke.THREAD_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = 0,
            };

            PInvoke.SetThreadInformation(
                PInvoke.GetCurrentThread(),
                THREAD_INFORMATION_CLASS.ThreadPowerThrottling,
                &state,
                (uint)sizeof(THREAD_POWER_THROTTLING_STATE)
            );
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
