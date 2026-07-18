using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar.Converters
{
    public class ItemTemplateConverter : MarkupExtension, IValueConverter
    {
        private const string FallbackKey = "Normal";

        private static readonly Lazy<ResourceDictionary> Templates = new(() =>
            new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/EverythingToolbar;component/ItemTemplates/ItemTemplates.xaml"),
            }
        );

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var templates = Templates.Value;
            var key = value as string;
            return key != null && templates.Contains(key) ? templates[key] : templates[FallbackKey];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException("This converter cannot be used in two-way binding.");

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
