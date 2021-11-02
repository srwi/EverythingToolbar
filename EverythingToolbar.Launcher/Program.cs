using System;
using System.Windows;

namespace EverythingToolbar.Launcher
{
    class Program
    {
        public partial class LauncherWindow : Window
        {
            public LauncherWindow()
            {
                Title = "EverythingToolbar";
                Width = 0;
                Height = 0;
                Opacity = 0;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;
                Topmost = true;
                SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                ToolbarControl tc = new ToolbarControl();
                Content = tc;
                EverythingSearch.Instance.SearchTerm = "";
                SearchResultsPopup searchResultsPopup = (SearchResultsPopup)tc.FindName("SearchResultsPopup");
                searchResultsPopup.Closed += OnPopupClosed;
            }

            private void OnPopupClosed(object sender, EventArgs e)
            {
                Close();
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application app = new Application();
            app.Run(new LauncherWindow());
        }
    }
}
