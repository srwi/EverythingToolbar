using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using NLog;

namespace EverythingToolbar
{
    public partial class TaskbarWindow : Window
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<TaskbarWindow>();

        private IntPtr _taskbarHandle;

        /// <summary>
        /// Gets the ToolbarControl for placement target purposes.
        /// </summary>
        public FrameworkElement PlacementTarget => ToolbarControl;

        public TaskbarWindow()
        {
            InitializeComponent();
            DataContext = ToolbarSettings.User;

            Loaded += OnLoaded;
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            // SystemEvents raises on a worker thread; marshal back to the UI thread.
            // Reset the cached taskbar handle so a stale one is re-resolved after the change.
            Dispatcher.BeginInvoke(() =>
            {
                _taskbarHandle = IntPtr.Zero;
                UpdatePosition();
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = (HwndSource)PresentationSource.FromDependencyObject(this);
            source?.AddHook(WndProc);

            SetupAsTaskbarChild();
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x003D: // WM_GETOBJECT
                case 0x0018: // WM_SHOWWINDOW
                case 0x0046: // WM_WINDOWPOSCHANGING
                case 0x0083: // WM_NCCALCSIZE
                case 0x0281: // WM_IME_SETCONTEXT
                case 0x0282: // WM_IME_NOTIFY
                    handled = true;
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePosition();
        }

        private void SetupAsTaskbarChild()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                _taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (_taskbarHandle == IntPtr.Zero)
                {
                    Logger.Warn("Could not find taskbar handle");
                    return;
                }

                int style = GetWindowLong(hwnd, GWL_STYLE);
                style = (style & ~WS_POPUP) | WS_CHILD;
                SetWindowLong(hwnd, GWL_STYLE, style);

                SetParent(hwnd, _taskbarHandle);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to setup as taskbar child");
            }
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Window lifecycle (enable/disable) is owned by the Launcher, which creates and
            // closes the TaskbarWindow. Here we only react to changes affecting placement.
            if (e.PropertyName == nameof(ToolbarSettings.User.TaskbarWindowAlignment))
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (!IsLoaded || !ToolbarSettings.User.TaskbarWindowEnabled)
                return;

            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                if (_taskbarHandle == IntPtr.Zero)
                    _taskbarHandle = FindWindow("Shell_TrayWnd", null);

                if (_taskbarHandle == IntPtr.Zero)
                {
                    Logger.Warn("Could not find taskbar handle");
                    return;
                }

                if (GetParent(hwnd) != _taskbarHandle)
                    SetParent(hwnd, _taskbarHandle);

                CalculateAndSetPosition(hwnd);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating position");
            }
        }

        private void CalculateAndSetPosition(IntPtr hwnd)
        {
            double dpiScale = GetDpiForWindow(_taskbarHandle) / 96.0;

            if (!GetWindowRect(_taskbarHandle, out RECT taskbarRect))
                return;

            int taskbarWidth = taskbarRect.Right - taskbarRect.Left;
            int taskbarHeight = taskbarRect.Bottom - taskbarRect.Top;
            int screenCenter = taskbarRect.Left + taskbarWidth / 2;

            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            int widgetWidth = (int)(Math.Max(DesiredSize.Width, 250) * dpiScale);
            int widgetHeight = (int)(Math.Max(DesiredSize.Height, 32) * dpiScale);

            int top = (taskbarHeight - widgetHeight) / 2;
            int left = CalculateHorizontalPosition(taskbarRect, widgetWidth, screenCenter, dpiScale);

            SetWindowPos(hwnd, IntPtr.Zero,
                left, top,
                widgetWidth, widgetHeight,
                SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private int CalculateHorizontalPosition(RECT taskbarRect, int widgetWidth, int screenCenter, double dpiScale)
        {
            int padding = (int)(8 * dpiScale);
            int taskbarWidth = taskbarRect.Right - taskbarRect.Left;
            string alignment = ToolbarSettings.User.TaskbarWindowAlignment;
            bool isTaskbarCentered = Utils.IsTaskbarCenterAligned();

            var widgetsRect = GetWidgetsButtonRect();
            if (widgetsRect.HasValue)
            {
                POINT pt = new() { X = (int)widgetsRect.Value.Left, Y = 0 };
                ScreenToClient(_taskbarHandle, ref pt);
                int widgetsLeftRelative = pt.X;

                pt = new POINT { X = (int)widgetsRect.Value.Right, Y = 0 };
                ScreenToClient(_taskbarHandle, ref pt);
                int widgetsRightRelative = pt.X;

                // Left setting + center taskbar: widget is on left, place on right of widget
                // Otherwise (right setting or left taskbar): place on left of widget
                if (alignment == "Left" && isTaskbarCentered)
                {
                    return widgetsRightRelative + padding;
                }
                else
                {
                    return widgetsLeftRelative - widgetWidth - padding;
                }
            }

            var systemTrayRect = GetSystemTrayRect();
            if (systemTrayRect.HasValue)
            {
                POINT pt = new() { X = (int)systemTrayRect.Value.Left, Y = 0 };
                ScreenToClient(_taskbarHandle, ref pt);
                int trayLeftRelative = pt.X;

                // Left setting + center taskbar: place at very left edge
                // Otherwise (right setting or left taskbar): place on left of system tray
                if (alignment == "Left" && isTaskbarCentered)
                {
                    return 0;
                }
                else
                {
                    return trayLeftRelative - widgetWidth - padding;
                }
            }

            // Fallback
            if (alignment == "Left" && isTaskbarCentered)
                return taskbarWidth - widgetWidth - padding;
            else
                return 0;
        }

        private Rect? GetWidgetsButtonRect()
        {
            try
            {
                var taskbarElement = AutomationElement.FromHandle(_taskbarHandle);
                if (taskbarElement == null)
                    return null;

                var widgetsButton = taskbarElement.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "WidgetsButton"));

                if (widgetsButton != null)
                {
                    var rect = widgetsButton.Current.BoundingRectangle;
                    if (!rect.IsEmpty)
                        return rect;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not find Widgets button");
            }

            return null;
        }

        private Rect? GetSystemTrayRect()
        {
            try
            {
                var taskbarElement = AutomationElement.FromHandle(_taskbarHandle);
                if (taskbarElement == null)
                    return null;

                var systemTray = taskbarElement.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "SystemTrayIcon"));

                if (systemTray != null)
                {
                    var rect = systemTray.Current.BoundingRectangle;
                    if (!rect.IsEmpty)
                        return rect;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not find System Tray");
            }

            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            ToolbarSettings.User.PropertyChanged -= OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }

        #region Win32 Interop

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X, Y;
        }

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        #endregion
    }
}
