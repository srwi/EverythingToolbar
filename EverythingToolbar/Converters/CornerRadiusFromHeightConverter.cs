using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class CornerRadiusFromHeightConverter : MarkupExtension, IValueConverter
    {
        public static readonly CornerRadiusFromHeightConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double height && !double.IsInfinity(height) && height > 0)
            {
                double ratio = 0.5;
                if (parameter is double r)
                    ratio = r;
                else if (
                    parameter is string s
                    && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRatio)
                )
                    ratio = parsedRatio;

                return new CornerRadius(height * ratio);
            }

            return default(CornerRadius);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("This converter cannot be used in two-way binding.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
