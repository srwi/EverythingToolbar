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

            var controller = Ioc.Default.GetRequiredService<SearchWindowController>();
            controller.SetIconMode(false);
            ShortcutService.Initialize(controller.ToggleSearchUi);
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