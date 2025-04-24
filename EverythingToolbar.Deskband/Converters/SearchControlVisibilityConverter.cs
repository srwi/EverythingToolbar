using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Deskband.Converters
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
            var autoHideData = new APPBARDATA
            {
                hWnd = IntPtr.Zero,
                cbSize = Marshal.SizeOf<APPBARDATA>()
            };
            var autoHideState = Shell32.SHAppBarMessage(APPBARMESSAGE.ABM_GETSTATE, ref autoHideData);
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
            throw new NotSupportedException("This converter cannot be used in two-way binding.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}