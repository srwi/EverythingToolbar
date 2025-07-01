using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace EverythingToolbar.Settings
{
    public partial class About
    {
        public About()
        {
            InitializeComponent();

            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            VersionTextBlock.Text = Properties.Resources.AboutVersion + " " +
                                    (version.Revision == 0
                                        ? $"{version.Major}.{version.Minor}.{version.Build}"
                                        : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        }

        private void OnSearchSettingsClicked(object sender, RoutedEventArgs e)
        {
            NavigateToPage(typeof(Search));
        }

        private void OnCustomActionsClicked(object sender, RoutedEventArgs e)
        {
            NavigateToPage(typeof(CustomActions));
        }

        private void OnUserInterfaceSettingsClicked(object sender, RoutedEventArgs e)
        {
            NavigateToPage(typeof(UserInterface));
        }

        private void NavigateToPage(Type pageType)
        {
            if (Window.GetWindow(this) is not { } window)
                return;

            FindNavigationView(window)?.Navigate(pageType);
        }

        private static NavigationView? FindNavigationView(DependencyObject parent)
        {
            if (parent is NavigationView navigationView)
                return navigationView;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                NavigationView? result = FindNavigationView(child);

                if (result != null)
                    return result;
            }

            return null;
        }
    }
}