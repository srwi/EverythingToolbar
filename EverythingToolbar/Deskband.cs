using CSDeskBand.ContextMenu;
using EverythingToolbar;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace CSDeskBand
{
	[ComVisible(true)]
    [Guid("9d39b79c-e03c-4757-b1b6-ecce843748f3")]
    [CSDeskBandRegistration(Name = "Everything Toolbar")]
    public class Deskband : CSDeskBandWpf
    {
        protected override UIElement UIElement => new ToolbarControl();

        public Deskband()
        {
            Options.ContextMenuItems = ContextMenuItems;
            Options.MinHorizontalSize = new Size(36, 0);
            Options.MinVerticalSize = new Size(0, 32);
            TaskbarInfo.TaskbarEdgeChanged += OnTaskbarEdgeChanged;

            ToolbarLogger.Initialize();
			ILogger logger = ToolbarLogger.GetLogger("EverythingToolbar");
            logger.Info("EverythingToolbar started. Version: {version}, OS: {os}", Assembly.GetExecutingAssembly().GetName().Version, Environment.OSVersion);

            AppDomain.CurrentDomain.UnhandledException += (sender, args) => ToolbarLogger.GetLogger("EverythingToolbar").Error((Exception)args.ExceptionObject, "Unhandled Exception");

            ToolbarControl.SetTaskbarEdge(TaskbarInfo.Edge);
        }

        private void OnTaskbarEdgeChanged(object sender, TaskbarEdgeChangedEventArgs e)
        {
            ToolbarControl.SetTaskbarEdge(e.Edge);
        }

        private List<DeskBandMenuItem> ContextMenuItems
        {
            get
            {
                var action = new DeskBandMenuAction("Action");
                return new List<DeskBandMenuItem>() { action };
            }
        }
    }
}
