using Microsoft.Win32;
using System;
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
                ToolbarLogger.Initialize();

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

                EverythingSearch.Instance.PropertyChanged += OnEverythingSearchPropertyChanged;
            }

            private void OnEverythingSearchPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "SearchTerm" && EverythingSearch.Instance.SearchTerm != null)
                {
                    SetForegroundWindow(handle);
                }
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
                System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.PrimaryScreen;
                Rectangle taskbar = FindDockedTaskBar(screen);
                double currentDpi = GetCurrentDpi();

                if (screen.WorkingArea.Y == taskbar.Y)
                {
                    Top = taskbar.Height * 96.0 / currentDpi;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Top;
                }
                else
                {
                    Top = taskbar.Y * 96.0 / currentDpi;
                    SearchResultsPopup.taskbarEdge = CSDeskBand.Edge.Bottom;
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    object taskbarAlignment = key?.GetValue("TaskbarAl");
                    bool isCenterAligned = taskbarAlignment != null && (int)taskbarAlignment == 1;
                    if (isCenterAligned)
                    {
                        Left = (taskbar.Width * 96.0 / currentDpi - EverythingToolbar.Properties.Settings.Default.popupSize.Width) / 2;
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

            private Rectangle FindDockedTaskBar(System.Windows.Forms.Screen screen)
            {
                Rectangle rect = new Rectangle(0, 0, 0, 0);

                if (!screen.Bounds.Equals(screen.WorkingArea))
                {
                    var leftDockedWidth = Math.Abs((Math.Abs(screen.Bounds.Left) - Math.Abs(screen.WorkingArea.Left)));
                    var topDockedHeight = Math.Abs((Math.Abs(screen.Bounds.Top) - Math.Abs(screen.WorkingArea.Top)));
                    var rightDockedWidth = ((screen.Bounds.Width - leftDockedWidth) - screen.WorkingArea.Width);
                    var bottomDockedHeight = ((screen.Bounds.Height - topDockedHeight) - screen.WorkingArea.Height);

                    if (leftDockedWidth > 0)
                    {
                        rect.X = screen.Bounds.Left;
                        rect.Y = screen.Bounds.Top;
                        rect.Width = leftDockedWidth;
                        rect.Height = screen.Bounds.Height;
                    }
                    else if (rightDockedWidth > 0)
                    {
                        rect.X = screen.WorkingArea.Right;
                        rect.Y = screen.Bounds.Top;
                        rect.Width = rightDockedWidth;
                        rect.Height = screen.Bounds.Height;
                    }
                    else if (topDockedHeight > 0)
                    {
                        rect.X = screen.WorkingArea.Left;
                        rect.Y = screen.Bounds.Top;
                        rect.Width = screen.WorkingArea.Width;
                        rect.Height = topDockedHeight;
                    }
                    else if (bottomDockedHeight > 0)
                    {
                        rect.X = screen.WorkingArea.Left;
                        rect.Y = screen.WorkingArea.Bottom;
                        rect.Width = screen.WorkingArea.Width;
                        rect.Height = bottomDockedHeight;
                    }
                }

                return rect;
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
