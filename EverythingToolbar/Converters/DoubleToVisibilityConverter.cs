using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
	public class DoubleToVisibilityConverter : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int threshold = System.Convert.ToInt32(parameter);

			if ((double)value > Math.Abs(threshold))
			{
				return threshold >= 0 ? Visibility.Visible : Visibility.Hidden;
			}

			return threshold >= 0 ? Visibility.Hidden : Visibility.Visible;
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
