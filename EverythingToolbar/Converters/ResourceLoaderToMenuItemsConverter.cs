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
                string fileIdentifier = Path.GetFileNameWithoutExtension(itemPath);
                string name = fileIdentifier;
                switch (fileIdentifier)
                {
                    case "COMPACT":
                        name = Properties.Resources.ItemTemplateCompact;
                        break;
                    case "COMPACT_DETAILED":
                        name = Properties.Resources.ItemTemplateCompactDetailed;
                        break;
                    case "NORMAL":
                        name = Properties.Resources.ItemTemplateNormal;
                        break;
                    case "NORMAL_DETAILED":
                        name = Properties.Resources.ItemTemplateNormalDetailed;
                        break;
                    case "DARK":
                        name = Properties.Resources.ThemeDark;
                        break;
                    case "MEDIUM":
                        name = Properties.Resources.ThemeMedium;
                        break;
                    case "LIGHT":
                        name = Properties.Resources.ThemeLight;
                        break;
                }
                menuItems.Add(new MenuItem()
                {
                    Header = name,
                    Tag = fileIdentifier,
                    IsCheckable = true,
                    IsChecked = Properties.Settings.Default[category].ToString() == fileIdentifier,
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
