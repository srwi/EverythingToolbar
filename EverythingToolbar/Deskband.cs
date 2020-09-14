using CSDeskBand.ContextMenu;
using EverythingToolbar;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace CSDeskBand
{
	[ComVisible(true)]
    [Guid("AA01ACB3-6CCC-497C-9CE6-9211F2EDFC10")]
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
