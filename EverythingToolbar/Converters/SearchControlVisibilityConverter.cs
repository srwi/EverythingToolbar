using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class SearchControlVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool AlwaysVisibleWithAutoHidingTaskbar { get; set; }
        public double VisibilityThreshold { get; set; }

        private static bool _isTaskbarAutoHiding;

        public SearchControlVisibilityConverter()
        {
            // We get the taskbar auto hide state only once for now as it is not expected to change often
            SetTaskbarAutoHideState();
        }

        private void SetTaskbarAutoHideState()
        {
            const uint ABS_AUTOHIDE = 0x0000001;
            var autoHideData = new APPBARDATA { hWnd = IntPtr.Zero, cbSize = Marshal.SizeOf<APPBARDATA>() };
            var autoHideState = SHAppBarMessage(APPBARMESSAGE.ABM_GETSTATE, ref autoHideData);
            if (autoHideState != IntPtr.Zero)
            {
                _isTaskbarAutoHiding = ((int)autoHideState.ToInt64() & ABS_AUTOHIDE) == ABS_AUTOHIDE;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_isTaskbarAutoHiding)
                return AlwaysVisibleWithAutoHidingTaskbar ? Visibility.Visible : Visibility.Collapsed;

            if (System.Convert.ToDouble(value) >= Math.Abs(VisibilityThreshold))
            {
                return VisibilityThreshold >= 0 ? Visibility.Visible : Visibility.Hidden;
            }

            return VisibilityThreshold >= 0 ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        // Win32 API structures and functions
        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum APPBARMESSAGE : uint
        {
            ABM_NEW = 0x00000000,
            ABM_REMOVE = 0x00000001,
            ABM_QUERYPOS = 0x00000002,
            ABM_SETPOS = 0x00000003,
            ABM_GETSTATE = 0x00000004,
            ABM_GETTASKBARPOS = 0x00000005,
            ABM_ACTIVATE = 0x00000006,
            ABM_GETAUTOHIDEBAR = 0x00000007,
            ABM_SETAUTOHIDEBAR = 0x00000008,
            ABM_WINDOWPOSCHANGED = 0x00000009,
            ABM_SETSTATE = 0x0000000A,
        }

        [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr SHAppBarMessage(APPBARMESSAGE dwMessage, ref APPBARDATA pData);
    }
}
