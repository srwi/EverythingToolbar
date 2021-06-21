using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace EverythingToolbar.Deskband
{
    public partial class ToolbarControlHost : UserControl
    {
        public ToolbarControlHost(CSDeskBand.CSDeskBandWin w)
        {
            InitializeComponent();
            //this.BackColor = Color.LimeGreen;
            //this.TransparencyKey = Color.LimeGreen;
            ElementHost host = new ElementHost
            {
                Dock = DockStyle.Fill,
                BackColorTransparent = true,
                Child = new ToolbarControl()
            };
            Controls.Add(host);
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }
    }
}
