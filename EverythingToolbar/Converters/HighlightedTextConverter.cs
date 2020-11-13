using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace EverythingToolbar
{
	public class HighlightedTextConverter : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string input)
			{
				TextBlock textBlock = new TextBlock();
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
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

			return null;
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
