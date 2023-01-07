using System;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Helpers
{
    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
