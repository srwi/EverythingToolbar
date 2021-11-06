using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EverythingToolbar.Launcher
{
    class Program
    {
        public partial class LauncherWindow : Window
        {
            public LauncherWindow()
            {
                Rectangle taskbar = FindDockedTaskBars()[0];
                Left = taskbar.Width / 2;
                Top = taskbar.Y;
                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;
                SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                Content = new ToolbarControl();
                StartToggleListener();
            }

            private void StartToggleListener()
            {
                Task.Factory.StartNew(() =>
                {
                    EventWaitHandle wh = new EventWaitHandle(false, EventResetMode.AutoReset, "EverythingToolbarToggleEvent");
                    while (true)
                    {
                        wh.WaitOne();
                        if (EverythingSearch.Instance.SearchTerm != null)
                            EverythingSearch.Instance.SearchTerm = null;
                        else
                            EverythingSearch.Instance.SearchTerm = "";
                    }
                });
            }

            public static List<Rectangle> FindDockedTaskBars()
            {
                List<Rectangle> dockedRects = new List<Rectangle>();
                foreach (var tmpScrn in System.Windows.Forms.Screen.AllScreens)
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
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "EverythingToolbar.Launcher", out bool createdNew))
            {
                if (createdNew)
                {
                    Application app = new Application();
                    app.Run(new LauncherWindow());
                }
                else
                {
                    try
                    {
                        EventWaitHandle wh = EventWaitHandle.OpenExisting("EverythingToolbarToggleEvent");
                        wh.Set();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }
    }
}
