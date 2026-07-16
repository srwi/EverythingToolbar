using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Behaviors;
using Microsoft.Xaml.Behaviors;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            Ioc.Default.GetRequiredService<TaskbarStateService>().IsIcon = false;
            ShortcutService.Initialize(UnifiedToolbarControl.FocusSearchBox);
            Ioc.Default.GetRequiredService<StartMenuService>().Initialize();

            var searchWindow = Ioc.Default.GetRequiredService<SearchWindow>();
            Interaction.GetBehaviors(searchWindow).Add(
                new SearchWindowPlacement(
                    Ioc.Default.GetRequiredService<TaskbarStateService>(),
                    Ioc.Default.GetRequiredService<ISettings>(),
                    Ioc.Default.GetRequiredService<WindowsPolicyService>())
                { PlacementTarget = UnifiedToolbarControl });
        }
    }
}