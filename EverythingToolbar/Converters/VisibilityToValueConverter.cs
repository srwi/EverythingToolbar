using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
    public class VisibilityToValueConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] values = System.Convert.ToString(parameter).Split(';');

            if (values.Length != 2)
                throw new ArgumentException("Converter parameter must include exactly two values separated by semicolon.");

            // value is of type Binding rather than Visibility
            bool isVisible = value.ToString() == "Visible";

            return isVisible ? values[0] : values[1];
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
