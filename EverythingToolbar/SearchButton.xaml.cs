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
