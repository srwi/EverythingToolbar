using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;

namespace EverythingToolbar
{
    // COM waits can discard input from the queue shared with Explorer.
    [ComVisible(true)]
    internal sealed class TaskbarComMessageFilter : IOleMessageFilter, IDisposable
    {
        private readonly IntPtr _window;
        private readonly Dispatcher _dispatcher;
        private IOleMessageFilter? _previous;
        private bool _registered;
        private bool _stopping;

        internal TaskbarComMessageFilter(IntPtr window)
        {
            _window = window;
            _dispatcher = Dispatcher.CurrentDispatcher;
            CheckRegistration(CoRegisterMessageFilter(this, out _previous));
            _registered = true;
        }

        public int HandleInComingCall(int callType, IntPtr caller, int ticks, IntPtr interfaceInfo) =>
            _previous?.HandleInComingCall(callType, caller, ticks, interfaceInfo) ?? 0;

        public int RetryRejectedCall(IntPtr callee, int ticks, int rejectType) =>
            _previous?.RetryRejectedCall(callee, ticks, rejectType) ?? -1;

        private bool CanPump => _registered && !_stopping && !_dispatcher.HasShutdownStarted && GetFocus() == _window;

        public int MessagePending(IntPtr callee, int ticks, int pendingType)
        {
            try
            {
                while (CanPump && PeekMessage(out var message, _window, 0x100, 0x109, 1))
                {
                    // PeekMessage returns WM_QUIT even with a keyboard-only range.
                    if (message.message == 0x12)
                    {
                        PostQuitMessage(message.wParam.ToInt32());
                        break;
                    }
                    if (!IsWindow(_window) || _dispatcher.HasShutdownStarted) break;
                    // Finish the retrieved message if focus moved; reposting would reorder input.
                    if (!ComponentDispatcher.RaiseThreadMessage(ref message))
                    {
                        if (!IsWindow(_window) || _dispatcher.HasShutdownStarted) break;
                        TranslateMessage(ref message);
                        DispatchMessage(ref message);
                    }
                }
                return _previous?.MessagePending(callee, ticks, pendingType) ?? 2;
            }
            catch (Exception error)
            {
                // Report application errors through WPF, not the PreserveSig COM boundary.
                if (!_dispatcher.HasShutdownStarted)
                    _dispatcher.BeginInvoke(new Action(ExceptionDispatchInfo.Capture(error).Throw));
                else
                    Trace.TraceError(error.ToString());
                return 0;
            }
        }


        internal static void CheckRegistration(int result)
        {
            if (result != 0)
                throw new COMException("Could not change the thread's OLE message filter.", result);
        }

        public void Dispose()
        {
            _dispatcher.VerifyAccess();
            _stopping = true;
            if (!_registered) return;
            CheckRegistration(CoRegisterMessageFilter(_previous, out _));
            _registered = false;
        }

        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter? filter, out IOleMessageFilter? previous);
        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int exitCode);

        [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out MSG message, IntPtr window, uint min, uint max, uint remove);
        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG message);
        [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
        private static extern IntPtr DispatchMessage(ref MSG message);
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int callType, IntPtr caller, int ticks, IntPtr interfaceInfo);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int rejectType);
        [PreserveSig] int MessagePending(IntPtr callee, int ticks, int pendingType);
    }
}
