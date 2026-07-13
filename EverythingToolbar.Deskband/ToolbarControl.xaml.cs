using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            Ioc.Default.GetRequiredService<SearchHost>().Attach(UnifiedToolbarControl, iconMode: false);
        }
    }
}