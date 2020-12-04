using CSDeskBand;
using System;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
	public class EdgeToPlacementModeConverter : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Edge edge = (Edge)value;
			if (edge == Edge.Left)
			{
				return PlacementMode.Right;
			}
			else if (edge == Edge.Top)
			{
				return PlacementMode.Bottom;
			}
			else if (edge == Edge.Right)
			{
				return PlacementMode.Left;
			}
			else
			{
				return PlacementMode.Top;
			}
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
