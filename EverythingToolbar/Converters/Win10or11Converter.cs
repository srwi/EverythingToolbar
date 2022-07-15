using EverythingToolbar.Helpers;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
    public class Win10or11Converter : MarkupExtension, IValueConverter
    {            
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] values = System.Convert.ToString(parameter).Split(';');

            if (values.Length != 2)
                throw new ArgumentException("Converter parameter must include exactly two values separated by semicolon.");

            return Utils.IsWindows11 ? values[1] : values[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("This converter cannot be used in two-way binding.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
