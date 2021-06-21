using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EverythingToolbar.Deskband
{
    [ComVisible(true)]
    [Guid("FB17B6DA-E3D7-4D17-9E43-3416988372A9")]
    [CSDeskBand.CSDeskBandRegistration(Name = "Everything Toolbar WinForms", ShowDeskBand = false)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control control;

        public Deskband()
        {
            Options.MinHorizontalSize = new Size(18, 30);
            control = new ToolbarControlHost(this);
        }

        protected override Control Control => control;
    }
}
