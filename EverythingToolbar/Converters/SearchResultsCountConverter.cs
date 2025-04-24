using EverythingToolbar.Properties;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class SearchResultsCountConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            var formattedValue = ((int)value).ToString("N0", culture);

            var suffix = (int)value == 1 ? Resources.SearchResult : Resources.SearchResults;
            return $"{formattedValue} {suffix}";
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