using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
    class ResourceLoaderToMenuItemsConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var menuItems = new ObservableCollection<MenuItem>();
            string category = parameter.ToString();

            foreach (var itemPath in (ObservableCollection<string>)value)
            {
                string theme = Path.GetFileNameWithoutExtension(itemPath);
                menuItems.Add(new MenuItem()
                {
                    Header = theme,
                    IsCheckable = true,
                    IsChecked = Properties.Settings.Default[category].ToString() == theme,
                });
            }

            return menuItems;
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
