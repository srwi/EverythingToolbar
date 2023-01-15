using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace EverythingToolbar.Behaviors
{
    public class ThemeAwareness : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.IsLoaded)
            {
                AssociatedObjectOnLoaded(null, null);
            }
            else
            {
                AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs _)
        {
            ResourceManager.Instance.ResourceChanged += (s, e) => { AssociatedObject.Resources = e.NewResource; };
            ResourceManager.Instance.AutoApplyTheme();
        }
    }
}
