using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Windows.Win32;

namespace EverythingToolbar.Converters
{
    public class SearchControlVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool AlwaysVisibleWithAutoHidingTaskbar { get; set; }
        public double VisibilityThreshold { get; set; }

        private static bool IsTaskbarAutoHiding;

        public SearchControlVisibilityConverter()
        {
            // We get the taskbar auto hide state only once for now as it is not expected to change often
            SetTaskbarAutoHideState();
        }

        private void SetTaskbarAutoHideState()
        {
            const uint ABS_AUTOHIDE = 0x0000001;
            var autoHideData = new Windows.Win32.UI.Shell.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf<Windows.Win32.UI.Shell.APPBARDATA>(),
            };
            var autoHideState = PInvoke.SHAppBarMessage(PInvoke.ABM_GETSTATE, ref autoHideData);
            if (autoHideState != 0)
            {
                IsTaskbarAutoHiding = ((uint)autoHideState & ABS_AUTOHIDE) == ABS_AUTOHIDE;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (IsTaskbarAutoHiding)
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
    }
}
