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
            if (Properties.Settings.Default.isHideEmptySearchResults &&
                (EverythingSearch.Instance.SearchTerm == null || EverythingSearch.Instance.SearchTerm == ""))
                return "";

            string output = value.ToString() + " ";
            output += (uint)value == 1 ? Properties.Resources.SearchResult : Properties.Resources.SearchResults;
            return output;
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
