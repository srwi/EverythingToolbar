using EverythingToolbar.Helpers;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EverythingToolbar
{
    public partial class SearchButton : Button
    {
        public SearchButton()
        {
            InitializeComponent();

            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
                UpdateTheme();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateTheme();
        }

        private void UpdateTheme()
        {
            bool systemUsesLightTheme;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object registryValueObject = key?.GetValue("SystemUsesLightTheme");
                systemUsesLightTheme = registryValueObject != null && (int)registryValueObject > 0;
            }

            if (systemUsesLightTheme)
            {
                Foreground = new SolidColorBrush(Colors.Black);
                (Template.FindName("OuterBorder", this) as Border).Opacity = 0.55;
            }
            else
            {
                Foreground = new SolidColorBrush(Colors.White);
                (Template.FindName("OuterBorder", this) as Border).Opacity = 0.2;
            }
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (SearchResultsWindow.Instance.IsOpen)
                EventDispatcher.Instance.InvokeHideWindow();
            else
                EventDispatcher.Instance.InvokeShowWindow();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Properties.Settings.Default.isIconOnly = (bool)e.NewValue;
        }
    }
}
