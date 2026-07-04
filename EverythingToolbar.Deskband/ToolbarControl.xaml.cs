using EverythingToolbar.Helpers;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            // Host owns global initialization; the unified ToolbarControl is a passive control.
            // AddPlacementBehavior="True" on the unified control handles SearchWindowPlacement.
            TaskbarStateManager.Instance.IsIcon = false;
            ShortcutManager.Initialize(UnifiedToolbarControl.FocusSearchBox);
            StartMenuIntegration.Instance.Initialize();
        }
    }
}
