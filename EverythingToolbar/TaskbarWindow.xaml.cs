using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar
{
    public partial class TaskbarWindow
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<TaskbarWindow>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();

        private IntPtr _taskbarHandle;

        private int _positionGeneration;

        private const double WidgetWidthDip = 300;
        private const double MinWidgetHeightDip = 32;
        private const double WidgetVerticalMarginDip = 6;

        public FrameworkElement PlacementTarget => ToolbarControl;

        public bool IsAttachedToTaskbar { get; private set; }

        public TaskbarWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            _settings.PropertyChanged += OnSettingsChanged;
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

            var source = PresentationSource.FromDependencyObject(this) as HwndSource;
            source?.AddHook(WndProc);

            SetupAsTaskbarChild();
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0018: // WM_SHOWWINDOW
                case 0x0046: // WM_WINDOWPOSCHANGING
                case 0x0083: // WM_NCCALCSIZE
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

                _taskbarHandle = NativeMethods.FindTaskbarHandle();
                if (_taskbarHandle == IntPtr.Zero)
                {
                    Logger.Warn("Could not find taskbar handle");
                    return;
                }

                int style = PInvoke.GetWindowLong((HWND)hwnd, (WINDOW_LONG_PTR_INDEX)GWL_STYLE);
                style = (style & ~WS_POPUP) | WS_CHILD;
                PInvoke.SetWindowLong((HWND)hwnd, (WINDOW_LONG_PTR_INDEX)GWL_STYLE, style);

                PInvoke.SetParent((HWND)hwnd, (HWND)_taskbarHandle);
                IsAttachedToTaskbar = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to setup as taskbar child");
            }
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.TaskbarWindowAlignment))
            {
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            if (!IsLoaded || !_settings.TaskbarWindowEnabled)
                return;

            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                if (_taskbarHandle == IntPtr.Zero)
                    _taskbarHandle = NativeMethods.FindTaskbarHandle();

                if (_taskbarHandle == IntPtr.Zero)
                {
                    Logger.Warn("Could not find taskbar handle");
                    return;
                }

                if (PInvoke.GetParent((HWND)hwnd) != _taskbarHandle)
                    PInvoke.SetParent((HWND)hwnd, (HWND)_taskbarHandle);

                var taskbarHandle = _taskbarHandle;
                var generation = ++_positionGeneration;
                Task.Run(() =>
                {
                    var anchors = ResolveAnchorRects(taskbarHandle);
                    Dispatcher.BeginInvoke(() =>
                    {
                        // Drop stale results if a newer UpdatePosition ran or the box went away.
                        if (generation != _positionGeneration || !IsLoaded)
                            return;
                        ApplyPosition(hwnd, taskbarHandle, anchors);
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating position");
            }
        }

        private void ApplyPosition(IntPtr hwnd, IntPtr taskbarHandle, AnchorRects anchors)
        {
            double dpiScale = NativeMethods.GetDpiForWindow(taskbarHandle) / 96.0;

            if (!PInvoke.GetWindowRect((HWND)taskbarHandle, out var taskbarRect))
                return;

            int taskbarHeight = taskbarRect.bottom - taskbarRect.top;

            int verticalMargin = (int)(WidgetVerticalMarginDip * dpiScale);
            int widgetHeight = Math.Max(taskbarHeight - 2 * verticalMargin, (int)(MinWidgetHeightDip * dpiScale));
            int widgetWidth = (int)(WidgetWidthDip * dpiScale);

            int top = (taskbarHeight - widgetHeight) / 2;
            int left = CalculateHorizontalPosition(taskbarHandle, taskbarRect, anchors, widgetWidth, dpiScale);

            PInvoke.SetWindowPos(
                (HWND)hwnd,
                (HWND)IntPtr.Zero,
                left,
                top,
                widgetWidth,
                widgetHeight,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
            );
        }

        private int CalculateHorizontalPosition(
            IntPtr taskbarHandle,
            RECT taskbarRect,
            AnchorRects anchors,
            int widgetWidth,
            double dpiScale
        )
        {
            int padding = (int)(8 * dpiScale);
            int taskbarWidth = taskbarRect.right - taskbarRect.left;
            string alignment = _settings.TaskbarWindowAlignment;
            bool isTaskbarCentered = _windowsPolicy.IsTaskbarCenterAligned();

            if (anchors.WidgetsRect.HasValue)
            {
                var pt = new System.Drawing.Point((int)anchors.WidgetsRect.Value.Left, 0);
                PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
                int widgetsLeftRelative = pt.X;

                pt = new System.Drawing.Point((int)anchors.WidgetsRect.Value.Right, 0);
                PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
                int widgetsRightRelative = pt.X;
                if (alignment == "Left" && isTaskbarCentered)
                {
                    return widgetsRightRelative + padding;
                }
                else
                {
                    return widgetsLeftRelative - widgetWidth - padding;
                }
            }

            if (anchors.SystemTrayRect.HasValue)
            {
                var pt = new System.Drawing.Point((int)anchors.SystemTrayRect.Value.Left, 0);
                PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
                int trayLeftRelative = pt.X;

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

        private static AnchorRects ResolveAnchorRects(IntPtr taskbarHandle)
        {
            var widgetsRect = GetWidgetsButtonRect(taskbarHandle);
            var systemTrayRect = widgetsRect.HasValue ? null : GetSystemTrayRect(taskbarHandle);
            return new AnchorRects(widgetsRect, systemTrayRect);
        }

        private readonly struct AnchorRects(Rect? widgetsRect, Rect? systemTrayRect)
        {
            public Rect? WidgetsRect { get; } = widgetsRect;
            public Rect? SystemTrayRect { get; } = systemTrayRect;
        }

        private static Rect? GetWidgetsButtonRect(IntPtr taskbarHandle)
        {
            try
            {
                var taskbarElement = AutomationElement.FromHandle(taskbarHandle);
                if (taskbarElement == null)
                    return null;

                var widgetsButton = taskbarElement.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "WidgetsButton")
                );

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

        private static Rect? GetSystemTrayRect(IntPtr taskbarHandle)
        {
            try
            {
                var taskbarElement = AutomationElement.FromHandle(taskbarHandle);
                if (taskbarElement == null)
                    return null;

                var systemTray = taskbarElement.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "SystemTrayIcon")
                );

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
            _settings.PropertyChanged -= OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
    }
}
