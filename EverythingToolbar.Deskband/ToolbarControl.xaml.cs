using EverythingToolbar.Helpers;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControl
    {
        public ToolbarControl()
        {
            InitializeComponent();

            TaskbarStateManager.Instance.IsIcon = false;
            ShortcutManager.Initialize(UnifiedToolbarControl.FocusSearchBox);
            StartMenuIntegration.Instance.Initialize();
        }
    }
}