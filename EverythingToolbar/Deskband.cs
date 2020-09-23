using CSDeskBand.ContextMenu;
using EverythingToolbar;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace CSDeskBand
{
	[ComVisible(true)]
    [Guid("9d39b79c-e03c-4757-b1b6-ecce843748f3")]
    [CSDeskBandRegistration(Name = "Everything Toolbar")]
    public class Deskband : CSDeskBandWpf
    {
        public Deskband()
        {
            Options.ContextMenuItems = ContextMenuItems;
            Options.MinHorizontalSize = new Size(36, 0);
            Options.MinVerticalSize = new Size(0, 32);
        }

        protected override UIElement UIElement => new ToolbarControl(TaskbarInfo);

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
