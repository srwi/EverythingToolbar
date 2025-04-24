using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows;

namespace EverythingToolbar.Behaviors
{
    public class FlowDirectionAwareness : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            UpdateFlowDirection();
        }

        private void UpdateFlowDirection()
        {
            if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
            {
                AssociatedObject.FlowDirection = FlowDirection.RightToLeft;
            }
            else
            {
                AssociatedObject.FlowDirection = FlowDirection.LeftToRight;
            }
        }
    }
}