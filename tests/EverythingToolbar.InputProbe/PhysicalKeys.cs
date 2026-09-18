using System;
using System.Runtime.InteropServices;

internal static class PhysicalKeys
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public Data data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Data
    {
        [FieldOffset(0)]
        public Keyboard keyboard;

        [FieldOffset(0)]
        public Mouse mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Keyboard
    {
        public ushort vk,
            scan;
        public uint flags,
            time;
        public UIntPtr extra;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Mouse
    {
        public int x,
            y;
        public uint data,
            flags,
            time;
        public UIntPtr extra;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint n, Input[] inputs, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    internal static bool CanSendToForeground(IntPtr foreground, IntPtr window, IntPtr parent) =>
        foreground != IntPtr.Zero && (foreground == window || foreground == parent);

    public static bool Send(char c, IntPtr window, IntPtr parent)
    {
        var foreground = GetForegroundWindow();
        if (!CanSendToForeground(foreground, window, parent))
        {
            Console.WriteLine($"ABORT: foreign foreground {foreground} (own={window}, parent={parent})");
            return false;
        }
        var vk = (ushort)char.ToUpperInvariant(c);
        var inputs = new[]
        {
            new Input
            {
                type = 1,
                data = new Data { keyboard = new Keyboard { vk = vk } },
            },
            new Input
            {
                type = 1,
                data = new Data
                {
                    keyboard = new Keyboard { vk = vk, flags = 2 },
                },
            },
        };
        return SendInput(2, inputs, Marshal.SizeOf<Input>()) == 2;
    }
}
