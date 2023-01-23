using System;
using System.Runtime.InteropServices;

namespace EverythingToolbar.Helpers
{
    public class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
