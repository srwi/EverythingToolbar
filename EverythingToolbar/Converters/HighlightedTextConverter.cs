using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class HighlightedTextConverter : MarkupExtension, IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string input)
                return null;

            TextBlock textBlock = new()
            {
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            if (parameter is string paramStr && double.TryParse(paramStr, out double fontSizePt))
            {
                // Convert points to DIPs (1pt = 4/3 DIP)
                textBlock.FontSize = fontSizePt * 4.0 / 3.0;
            }

            string[] segments = input.Split('*');
            for (int j = 0; j < segments.Length; j++)
            {
                if (j % 2 > 0)
                {
                    textBlock.Inlines.Add(new Run(segments[j]) { FontWeight = FontWeights.Bold });
                }
                else
                {
                    textBlock.Inlines.Add(new Run(segments[j]));
                }
            }

            return textBlock;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("This converter cannot be used in two-way binding.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}