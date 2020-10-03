using CSDeskBand;
using System.Windows;

namespace EverythingToolbar.Debug
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			grid.Children.Add(new ToolbarControl(Edge.Bottom));
		}
	}
}
