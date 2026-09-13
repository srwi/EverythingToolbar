using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class FontSizeFromHeightConverter : MarkupExtension, IValueConverter
    {
        public static readonly FontSizeFromHeightConverter Instance = new();

        private const double BaseHeight = 32.0;
        private const double DefaultBaseFontSize = 15.0;
        private const double ScaleRate = 0.25; // 1 DIP font reduction per 4 DIP height reduction

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double baseFontSize = DefaultBaseFontSize;
            if (parameter is double d)
                baseFontSize = d;
            else if (
                parameter is string s
                && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            )
                baseFontSize = parsed;

            double minFontSize = Math.Max(baseFontSize - 3.0, 9.0);
            double maxFontSize = baseFontSize;

            if (value is double height && !double.IsInfinity(height) && !double.IsNaN(height) && height > 0)
            {
                if (height >= BaseHeight)
                    return maxFontSize;

                double calculated = baseFontSize - (BaseHeight - height) * ScaleRate;
                return Math.Clamp(Math.Round(calculated, 1), minFontSize, maxFontSize);
            }

            return baseFontSize;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("This converter cannot be used in two-way binding.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
