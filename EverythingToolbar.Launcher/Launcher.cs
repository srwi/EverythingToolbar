using EverythingToolbar.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace EverythingToolbar.Launcher
{
    class Launcher
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

                StartToggleListener();
                if (!File.Exists(Utils.GetTaskbarShortcutPath()))
                    new TaskbarPinGuide().Show();
            }

            private void OnLoaded(object sender, RoutedEventArgs e)
            {
                handle = ((HwndSource)PresentationSource.FromVisual(this)).Handle;
            }

            private void StartToggleListener()
            {
                Task.Factory.StartNew(() =>
                {
                    EventWaitHandle wh = new EventWaitHandle(false, EventResetMode.AutoReset, "EverythingToolbarToggleEvent");
                    while (true)
                    {
                        wh.WaitOne();
                        if (EverythingSearch.Instance.DelayedOpened)
                        {
                            EverythingSearch.Instance.SearchTerm = null;
                        }
                        else
                        {
                            Dispatcher?.Invoke(() =>
                            {
                                SetPosition();
                            });
                            SetForegroundWindow(handle);
                            EverythingSearch.Instance.SearchTerm = "";
                        }
                    }
                });
            }

            private double GetCurrentDpi()
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                double dpi = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                return dpi;
            }

            private void SetPosition()
            {
                Rectangle taskbar = FindDockedTaskBars()[0];
                double currentDpi = GetCurrentDpi();

                if (taskbar.Y + taskbar.Height == System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height)
                {
                    Top = taskbar.Y * 96.0 / currentDpi;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                }
                else
                {
                    Top = taskbar.Height * 96.0 / currentDpi;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Top;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    object registryValueObject = key?.GetValue("TaskbarAl");
                    if (registryValueObject != null && (int)registryValueObject == 1)
                    {
                        Left = (taskbar.Width / 2 - EverythingToolbar.Properties.Settings.Default.popupSize.Width / 2) * 96.0 / currentDpi;
                    }
                    else
                    {
                        if (CultureInfo.CurrentCulture.TextInfo.IsRightToLeft)
                        {
                            Left = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width * 96.0 / currentDpi - 1;
                        }
                        else
                        {
                            Left = 0;
                        }
                    }
                }
            }

            private static List<Rectangle> FindDockedTaskBars()
            {
                List<Rectangle> dockedRects = new List<Rectangle>();
                foreach (var screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if (!screen.Bounds.Equals(screen.WorkingArea))
                    {
                        Rectangle rect = new Rectangle();

                        var leftDockedWidth = Math.Abs((Math.Abs(screen.Bounds.Left) - Math.Abs(screen.WorkingArea.Left)));
                        var topDockedHeight = Math.Abs((Math.Abs(screen.Bounds.Top) - Math.Abs(screen.WorkingArea.Top)));
                        var rightDockedWidth = ((screen.Bounds.Width - leftDockedWidth) - screen.WorkingArea.Width);
                        var bottomDockedHeight = ((screen.Bounds.Height - topDockedHeight) - screen.WorkingArea.Height);

                        if ((leftDockedWidth > 0))
                        {
                            rect.X = screen.Bounds.Left;
                            rect.Y = screen.Bounds.Top;
                            rect.Width = leftDockedWidth;
                            rect.Height = screen.Bounds.Height;
                        }
                        else if ((rightDockedWidth > 0))
                        {
                            rect.X = screen.WorkingArea.Right;
                            rect.Y = screen.Bounds.Top;
                            rect.Width = rightDockedWidth;
                            rect.Height = screen.Bounds.Height;
                        }
                        else if ((topDockedHeight > 0))
                        {
                            rect.X = screen.WorkingArea.Left;
                            rect.Y = screen.Bounds.Top;
                            rect.Width = screen.WorkingArea.Width;
                            rect.Height = topDockedHeight;
                        }
                        else if ((bottomDockedHeight > 0))
                        {
                            rect.X = screen.WorkingArea.Left;
                            rect.Y = screen.WorkingArea.Bottom;
                            rect.Width = screen.WorkingArea.Width;
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
                        icon.Icon = Icon.ExtractAssociatedIcon(Utils.GetThemedIconPath());
                        icon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] {
                            new System.Windows.Forms.MenuItem(Properties.Resources.ContextMenuRunSetupAssistant, (s, e) => { new TaskbarPinGuide().Show(); }),
                            new System.Windows.Forms.MenuItem(Properties.Resources.ContextMenuQuit, (s, e) => { app.Shutdown(); })
                        });
                        icon.Visible = true;
                        app.Run(new LauncherWindow());
                    }
                }
                else
                {
                    try
                    {
                        EventWaitHandle.OpenExisting("EverythingToolbarToggleEvent").Set();
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
