using EverythingToolbar.Behaviors;
using EverythingToolbar.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EverythingToolbar.Controls
{
    public partial class SearchButton : Button
    {
        public SearchButton()
        {
            InitializeComponent();

            SearchWindow.Instance.Activated += OnSearchWindowActivated;
            SearchWindow.Instance.Deactivated += OnSearchWindowDeactivated;

            ThemeAwareness.ResourceChanged += UpdateTheme;
        }

        private void OnSearchWindowDeactivated(object sender, EventArgs e)
        {
            var border = Template.FindName("OuterBorder", this) as Border;
            border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        }

        private void OnSearchWindowActivated(object sender, EventArgs e)
        {
            var border = Template.FindName("OuterBorder", this) as Border;
            border.Background = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
        }

        private void UpdateTheme(Theme newTheme)
        {
            var border = Template.FindName("OuterBorder", this) as Border;
            if (newTheme == Theme.Light)
            {
                Foreground = new SolidColorBrush(Colors.Black);
                border.Opacity = 0.55;
            }
            else
            {
                Foreground = new SolidColorBrush(Colors.White);
                border.Opacity = 0.2;
            }
        }

        private void UpdateTheme(object sender, ResourcesChangedEventArgs e)
        {
            if (IsLoaded)
                UpdateTheme(e.NewTheme);
            else
                Loaded += (s, e_) => { UpdateTheme(e.NewTheme); };
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            SearchWindow.Instance.Toggle();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TaskbarStateManager.Instance.IsIcon = (bool)e.NewValue;
        }
    }
}