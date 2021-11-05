using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace EverythingToolbar.Launcher
{
    class Program
    {
        public partial class LauncherWindow : Window
        {
            public LauncherWindow()
            {
                List<Rectangle> taskbars = FindDockedTaskBars();
                Rectangle taskbar = taskbars[0];
                Title = "EverythingToolbar";
                Width = 0;
                Height = 0;
                Left = taskbar.Width / 2;
                Top = taskbar.Y;
                
                Opacity = 0;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;
                Topmost = true;
                SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                ToolbarControl tc = new ToolbarControl();
                Content = tc;
                SearchResultsPopup searchResultsPopup = (SearchResultsPopup)tc.FindName("SearchResultsPopup");
                searchResultsPopup.Closed += OnPopupClosed;
                EverythingSearch.Instance.SearchTerm = "";
            }

            private void OnPopupClosed(object sender, EventArgs e)
            {
                Close();
            }

            public static List<Rectangle> FindDockedTaskBars()
            {
                List<Rectangle> dockedRects = new List<Rectangle>();
                foreach (var tmpScrn in Screen.AllScreens)
                {
                    if (!tmpScrn.Bounds.Equals(tmpScrn.WorkingArea))
                    {
                        Rectangle rect = new Rectangle();

                        var leftDockedWidth = Math.Abs((Math.Abs(tmpScrn.Bounds.Left) - Math.Abs(tmpScrn.WorkingArea.Left)));
                        var topDockedHeight = Math.Abs((Math.Abs(tmpScrn.Bounds.Top) - Math.Abs(tmpScrn.WorkingArea.Top)));
                        var rightDockedWidth = ((tmpScrn.Bounds.Width - leftDockedWidth) - tmpScrn.WorkingArea.Width);
                        var bottomDockedHeight = ((tmpScrn.Bounds.Height - topDockedHeight) - tmpScrn.WorkingArea.Height);

                        if ((leftDockedWidth > 0))
                        {
                            rect.X = tmpScrn.Bounds.Left;
                            rect.Y = tmpScrn.Bounds.Top;
                            rect.Width = leftDockedWidth;
                            rect.Height = tmpScrn.Bounds.Height;
                        }
                        else if ((rightDockedWidth > 0))
                        {
                            rect.X = tmpScrn.WorkingArea.Right;
                            rect.Y = tmpScrn.Bounds.Top;
                            rect.Width = rightDockedWidth;
                            rect.Height = tmpScrn.Bounds.Height;
                        }
                        else if ((topDockedHeight > 0))
                        {
                            rect.X = tmpScrn.WorkingArea.Left;
                            rect.Y = tmpScrn.Bounds.Top;
                            rect.Width = tmpScrn.WorkingArea.Width;
                            rect.Height = topDockedHeight;
                        }
                        else if ((bottomDockedHeight > 0))
                        {
                            rect.X = tmpScrn.WorkingArea.Left;
                            rect.Y = tmpScrn.WorkingArea.Bottom;
                            rect.Width = tmpScrn.WorkingArea.Width;
                            rect.Height = bottomDockedHeight;
                        }

                        dockedRects.Add(rect);
                    }
                }

                return dockedRects;
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            System.Windows.Application app = new System.Windows.Application();
            app.Run(new LauncherWindow());
        }
    }
}
