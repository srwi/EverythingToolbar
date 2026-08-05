using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class SearchControlVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool AlwaysVisibleWithAutoHidingTaskbar { get; set; }
        public double VisibilityThreshold { get; set; }
        public bool Invert { get; set; }

        private readonly bool _isTaskbarAutoHiding;

        public SearchControlVisibilityConverter()
        {
            // We get the taskbar auto hide state only once for now as it is not expected to change often
            _isTaskbarAutoHiding = NativeMethods.IsTaskbarAutoHiding();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_isTaskbarAutoHiding)
                return AlwaysVisibleWithAutoHidingTaskbar ? Visibility.Visible : Visibility.Collapsed;

            var isAboveThreshold = System.Convert.ToDouble(value) >= Math.Abs(VisibilityThreshold);
            if (Invert)
                isAboveThreshold = !isAboveThreshold;

            return isAboveThreshold ? Visibility.Visible : Visibility.Hidden;
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
