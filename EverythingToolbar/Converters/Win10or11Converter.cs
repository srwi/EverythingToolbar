using Microsoft.Win32;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace EverythingToolbar
{
    public class Win10or11Converter : MarkupExtension, IValueConverter
    {            
        private static int buildNumber = -1;
        private static bool IsWindows11
        {
            get
            {
                if (buildNumber == -1)
                {
                    object registryValue = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "");
                    buildNumber = System.Convert.ToInt32(registryValue);
                }

                return buildNumber >= 22000;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] values = System.Convert.ToString(parameter).Split(';');

            if (values.Length != 2)
                throw new ArgumentException("Converter parameter must include exactly two values separated by semicolon.");

            return IsWindows11 ? values[1] : values[0];
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
