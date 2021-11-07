using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace EverythingToolbar.Launcher
{
    class Program
    {
        public partial class LauncherWindow : Window
        {
            static IntPtr handle;

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool SetForegroundWindow(IntPtr hWnd);

            public LauncherWindow()
            {
                Width = 0;
                Height = 0;
                ShowInTaskbar = false;
                Visibility = Visibility.Hidden;
                ResizeMode = ResizeMode.NoResize;
                WindowStyle = WindowStyle.None;
                Content = new ToolbarControl();
                Loaded += OnLoaded;

                SetPosition();
                StartToggleListener();
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                handle = ((HwndSource)PresentationSource.FromVisual(this)).Handle;
            }

            private void SetPosition()
            {
                Rectangle taskbar = FindDockedTaskBars()[0];

                if (taskbar.Y + taskbar.Height == System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height)
                {
                    Top = taskbar.Y;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                }
                else
                {
                    Top = taskbar.Height;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Top;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    object registryValueObject = key?.GetValue("TaskbarAl");
                    if (registryValueObject != null && (int)registryValueObject == 1)
                    {
                        Left = taskbar.Width / 2 - Properties.Settings.Default.popupSize.Width / 2;
                    }
                    else
                    {
                        if (CultureInfo.CurrentCulture.TextInfo.IsRightToLeft)
                        {
                            Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - 1;
                        }
                        else
                        {
                            Left = 0;
                        }
                    }
                }
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
                        {
                            EverythingSearch.Instance.SearchTerm = null;
                        }
                        else
                        {
                            SetForegroundWindow(handle);
                            EverythingSearch.Instance.SearchTerm = "";
                        }
                    }
                });
            }

            private static List<Rectangle> FindDockedTaskBars()
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
                    using (System.Windows.Forms.NotifyIcon icon = new System.Windows.Forms.NotifyIcon())
                    {
                        Application app = new Application();
                        icon.Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
                        icon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] {
                            new System.Windows.Forms.MenuItem("Quit", (s, e) => { app.Shutdown(); })
                        });
                        icon.Visible = true;
                        app.Run(new LauncherWindow());
                    }
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
