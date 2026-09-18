using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

internal sealed class NativeHost : IDisposable
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int ex,
        string cls,
        string title,
        int style,
        int x,
        int y,
        int w,
        int h,
        IntPtr parent,
        IntPtr menu,
        IntPtr inst,
        IntPtr param
    );

    [DllImport("user32.dll")]
    private static extern int GetMessage(out Msg msg, IntPtr h, uint min, uint max);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg msg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg msg);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr h);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint id, uint m, IntPtr w, IntPtr l);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr child, IntPtr parent);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr h, int i, int v);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr h, int i);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr w;
        public IntPtr l;
        public uint time;
        public int x,
            y;
        public uint priv;
    }

    private readonly Thread _thread;
    private uint _id;
    public IntPtr Handle { get; }

    public NativeHost()
    {
        var ready = new TaskCompletionSource<IntPtr>();
        _thread = new Thread(() =>
        {
            _id = GetCurrentThreadId();
            var h = CreateWindowEx(
                0,
                "STATIC",
                "Isolated native input host",
                unchecked((int)0x10CF0000),
                100,
                100,
                500,
                180,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero
            );
            ready.SetResult(h);
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            DestroyWindow(h);
        })
        {
            IsBackground = true,
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        Handle = ready.Task.GetAwaiter().GetResult();
    }

    public void Attach(IntPtr child)
    {
        SetWindowLong(child, -16, (GetWindowLong(child, -16) & ~unchecked((int)0x80000000)) | 0x40000000);
        SetParent(child, Handle);
    }

    public void Dispose()
    {
        PostThreadMessage(_id, 0x12, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(2000);
    }
}
