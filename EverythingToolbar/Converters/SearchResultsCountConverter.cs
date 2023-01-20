using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
    public class SearchResultsCountConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            string suffix = (int)value == 1 ? Properties.Resources.SearchResult : Properties.Resources.SearchResults;
            return $"{value} {suffix}";
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
