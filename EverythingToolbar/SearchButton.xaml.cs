using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EverythingToolbar
{
    public partial class SearchButton : Button
    {
        private bool popupWasOpen = false;

        public SearchButton()
        {
            InitializeComponent();

            if (AppsUsLightTheme())
                Foreground = new SolidColorBrush(Colors.Black);
            else
                Foreground = new SolidColorBrush(Colors.White);
        }

        private static bool AppsUsLightTheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object registryValueObject = key?.GetValue("AppsUseLightTheme");

                if (registryValueObject == null)
                {
                    return true;
                }

                return (int)registryValueObject > 0;
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            popupWasOpen = EverythingSearch.Instance.SearchTerm != null;
        }

        private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            EverythingSearch.Instance.SearchTerm = popupWasOpen ? null : "";
            popupWasOpen = false;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Properties.Settings.Default.isIconOnly = (bool)e.NewValue;
        }
    }
}
