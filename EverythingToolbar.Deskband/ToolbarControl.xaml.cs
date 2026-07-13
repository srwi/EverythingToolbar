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
            Ioc.Default.GetRequiredService<GlobalShortcutListener>().Initialize(controller.ToggleSearchUi);
            Ioc.Default.GetRequiredService<StartMenuSearchInterceptor>().Initialize();

            var searchWindow = Ioc.Default.GetRequiredService<SearchWindow>();
            Interaction.GetBehaviors(searchWindow).Add(
                new SearchWindowPlacement(
                    Ioc.Default.GetRequiredService<TaskbarInfoProvider>(),
                    Ioc.Default.GetRequiredService<ISettings>(),
                    Ioc.Default.GetRequiredService<WindowsPolicy>())
                { PlacementTarget = UnifiedToolbarControl });
        }
    }
}