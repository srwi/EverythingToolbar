using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using EverythingToolbar;

internal static class Program
{
    private static int _passed;
    [STAThread]
    private static int Main()
    {
        var box = new TextBox();
        var window = new Window { Title = "COM input regression", Width = 320, Height = 100, Content = box };
        IOleMessageFilter? original = null;
        var previous = new PreviousFilter();
        try
        {
            window.Show();
            window.Activate();
            box.Focus();
            Pump();
            var hwnd = new WindowInteropHelper(window).Handle;
            SetFocus(hwnd);
            Check(GetFocus() == hwnd, "test window has thread input focus");
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous, out original));
            using (var filter = new TaskbarComMessageFilter(hwnd))
            {
                Check(filter.HandleInComingCall(0, IntPtr.Zero, 0, IntPtr.Zero) == 0, "incoming calls delegate to previous filter");
                Check(filter.RetryRejectedCall(IntPtr.Zero, 0, 0) == 123, "retry policy delegates to previous filter");
                foreach (var ch in "Everything æøå ÆØÅ")
                    Check(PostMessage(hwnd, 0x102, (IntPtr)ch, (IntPtr)1), "post original WM_CHAR");
                filter.MessagePending(IntPtr.Zero, 0, 1);
                Check(box.Text == "Everything æøå ÆØÅ", "queued Unicode text is delivered in order during COM callback");
                Pump();
                Check(box.Text == "Everything æøå ÆØÅ", "normal dispatcher does not deliver input twice");

                var calls = 0;
                ThreadMessageEventHandler handler = (ref MSG msg, ref bool handled) =>
                {
                    if (msg.hwnd == hwnd && msg.message == 0x102) { calls++; handled = true; }
                };
                ComponentDispatcher.ThreadFilterMessage += handler;
                try
                {
                    PostMessage(hwnd, 0x102, (IntPtr)'X', (IntPtr)1);
                    filter.MessagePending(IntPtr.Zero, 0, 1);
                }
                finally { ComponentDispatcher.ThreadFilterMessage -= handler; }
                Check(calls == 1 && box.Text == "Everything æøå ÆØÅ", "WPF handled input is not dispatched again");

                using var other = new HwndSource(new HwndSourceParameters("Other input target") { Width = 10, Height = 10, WindowStyle = unchecked((int)0x80000000) });
                PostMessage(other.Handle, 0x102, (IntPtr)'Q', (IntPtr)1);
                filter.MessagePending(IntPtr.Zero, 0, 1);
                Check(PeekMessage(out var foreign, other.Handle, 0x102, 0x102, 1) && foreign.wParam == (IntPtr)'Q', "other HWND input remains in its queue");
                Check(previous.PendingCalls > 0, "pending-message policy delegates to previous filter");

                PostMessage(IntPtr.Zero, 0x12, (IntPtr)37, IntPtr.Zero);
                filter.MessagePending(IntPtr.Zero, 0, 1);
                var hasQuit = false;
                MSG quit = default;
                for (var i = 0; i < 100 && PeekMessage(out quit, IntPtr.Zero, 0, 0, 1); i++)
                {
                    if (quit.message == 0x12) { hasQuit = true; break; }
                    if (!ComponentDispatcher.RaiseThreadMessage(ref quit)) DispatchMessage(ref quit);
                }
                Check(hasQuit && quit.message == 0x12 && quit.wParam == (IntPtr)37,
                    $"COM pump preserves WM_QUIT and its exit code (found={hasQuit}, msg={quit.message:X}, code={quit.wParam})");

                calls = 0;
                handler = (ref MSG msg, ref bool handled) =>
                {
                    if (msg.hwnd == hwnd && msg.message == 0x102)
                    {
                        calls++;
                        handled = true;
                        SetFocus(other.Handle);
                    }
                };
                ComponentDispatcher.ThreadFilterMessage += handler;
                try
                {
                    PostMessage(hwnd, 0x102, (IntPtr)'A', (IntPtr)1);
                    PostMessage(hwnd, 0x102, (IntPtr)'B', (IntPtr)1);
                    filter.MessagePending(IntPtr.Zero, 0, 1);
                    Check(calls == 1, "pump stops when preprocessing moves focus");
                    Check(PeekMessage(out var remaining, hwnd, 0x102, 0x102, 1) && remaining.wParam == (IntPtr)'B', "later input is left queued");
                }
                finally { ComponentDispatcher.ThreadFilterMessage -= handler; SetFocus(hwnd); }

                var expected = new InvalidOperationException("Test handler failure");
                Exception? reported = null;
                DispatcherUnhandledExceptionEventHandler onError = (_, e) => { reported = e.Exception; e.Handled = true; };
                handler = (ref MSG msg, ref bool handled) => { if (msg.message == 0x102) throw expected; };
                Dispatcher.CurrentDispatcher.UnhandledException += onError;
                ComponentDispatcher.ThreadFilterMessage += handler;
                try
                {
                    PostMessage(hwnd, 0x102, (IntPtr)'X', (IntPtr)1);
                    Check(filter.MessagePending(IntPtr.Zero, 0, 1) == 0, "application exception cancels the COM wait");
                    ComponentDispatcher.ThreadFilterMessage -= handler;
                    Pump();
                    Check(ReferenceEquals(reported, expected), "application exception reaches WPF outside COM");
                }
                finally
                {
                    ComponentDispatcher.ThreadFilterMessage -= handler;
                    Dispatcher.CurrentDispatcher.UnhandledException -= onError;
                }

                TaskbarComMessageFilter.CheckRegistration(0);
                foreach (var result in new[] { 1, unchecked((int)0x80004005) })
                {
                    var rejected = false;
                    try { TaskbarComMessageFilter.CheckRegistration(result); }
                    catch (COMException error) { rejected = error.HResult == result; }
                    Check(rejected, $"registration rejects non-S_OK result {result:X}");
                }

                filter.Dispose();
                PostMessage(hwnd, 0x102, (IntPtr)'Z', (IntPtr)1);
                filter.MessagePending(IntPtr.Zero, 0, 1);
                Check(PeekMessage(out var afterDispose, hwnd, 0x102, 0x102, 1) && afterDispose.wParam == (IntPtr)'Z', "disposed filter cannot consume input");
            }
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(null, out var restored));
            Check(ReferenceEquals(restored, previous), "dispose restores previous COM filter");
            Console.WriteLine($"PASS: {_passed} assertions");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            CoRegisterMessageFilter(original, out _);
            window.Close();
        }
    }
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + name);
        _passed++;
        Console.WriteLine("PASS: " + name);
    }
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    [ComVisible(true)]
    private sealed class PreviousFilter : IOleMessageFilter
    {
        internal int PendingCalls;
        public int HandleInComingCall(int a, IntPtr b, int c, IntPtr d) => 0;
        public int RetryRejectedCall(IntPtr a, int b, int c) => 123;
        public int MessagePending(IntPtr a, int b, int c) { PendingCalls++; return 2; }
    }
    [DllImport("ole32.dll")] private static extern int CoRegisterMessageFilter(IOleMessageFilter? filter, out IOleMessageFilter? previous);
    [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetFocus();

    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")] private static extern IntPtr DispatchMessage(ref MSG message);
    [DllImport("user32.dll", EntryPoint = "PostMessageW")] private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")] private static extern bool PeekMessage(out MSG message, IntPtr hwnd, uint min, uint max, uint remove);
}
