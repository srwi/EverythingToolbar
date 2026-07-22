using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Threading;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar
{
    public partial class TaskbarWindow
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<TaskbarWindow>();
        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;

        private IntPtr _taskbarHandle;
        private int _positionGeneration;

        private WINEVENTPROC? _taskbarEventCallback;
        private HWINEVENTHOOK _taskbarLocationHook;
        private readonly DispatcherTimer _repositionDebounceTimer;

        private const double WidgetWidthDip = 300;
        private const double MinWidgetHeightDip = 32;
        private const double WidgetVerticalMarginDip = 6;
        private const double HorizontalPaddingDip = 8;

        private const int RepositionDebounceMilliseconds = 150;

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WM_SHOWWINDOW = 0x0018;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_NCCALCSIZE = 0x0083;

        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        public FrameworkElement PlacementTarget => ToolbarControl;

        public bool IsAttachedToTaskbar { get; private set; }

        public TaskbarWindow(WindowsPolicy windowsPolicy, ISettings settings)
        {
            _windowsPolicy = windowsPolicy;
            _settings = settings;

            InitializeComponent();

            _repositionDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RepositionDebounceMilliseconds),
            };
            _repositionDebounceTimer.Tick += OnRepositionDebounceTick;

            Loaded += OnLoaded;
            _settings.PropertyChanged += OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
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
                case WM_SHOWWINDOW:
                case WM_WINDOWPOSCHANGING:
                case WM_NCCALCSIZE:
                    handled = true;
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _repositionDebounceTimer.Stop();
            UnhookTaskbarEvents();
            _settings.PropertyChanged -= OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            base.OnClosed(e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePosition();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.TaskbarWindowAlignment))
            {
                UpdatePosition();
            }
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

                HookTaskbarEvents();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to setup as taskbar child");
            }
        }

        private void HookTaskbarEvents()
        {
            if (_taskbarHandle == IntPtr.Zero || _taskbarEventCallback != null)
                return;

            uint threadId = NativeMethods.GetWindowThreadProcessId(_taskbarHandle, out uint processId);
            if (threadId == 0)
                return;

            // Scope the hook to the taskbar's own thread so we only hear about its layout changes
            // (the taskbar moving/resizing on auto-hide, alignment switching, remaining buttons
            // shifting) rather than every window on the desktop.
            _taskbarEventCallback = OnTaskbarEvent;
            _taskbarLocationHook = PInvoke.SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE,
                EVENT_OBJECT_LOCATIONCHANGE,
                default(HMODULE),
                _taskbarEventCallback,
                processId,
                threadId,
                WINEVENT_OUTOFCONTEXT
            );
        }

        private void UnhookTaskbarEvents()
        {
            if (_taskbarEventCallback == null)
                return;

            PInvoke.UnhookWinEvent(_taskbarLocationHook);
            _taskbarLocationHook = default;
            _taskbarEventCallback = null;
        }

        private void OnTaskbarEvent(
            HWINEVENTHOOK hWinEventHook,
            uint eventType,
            HWND hwnd,
            int idObject,
            int idChild,
            uint idEventThread,
            uint dwmsEventTime
        )
        {
            // The taskbar emits location changes in bursts (e.g. during reflow animations); coalesce
            // them and reposition once things settle. Marshal onto the UI thread to touch the timer.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(RestartRepositionDebounce);
                return;
            }

            RestartRepositionDebounce();
        }

        private void RestartRepositionDebounce()
        {
            _repositionDebounceTimer.Stop();
            _repositionDebounceTimer.Start();
        }

        private void OnRepositionDebounceTick(object? sender, EventArgs e)
        {
            _repositionDebounceTimer.Stop();
            UpdatePosition();
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
            // While the taskbar is auto-hidden or mid-reveal its anchors aren't measurable. Keep the
            // last good position instead of falling back to the far-left corner.
            if (!anchors.WidgetsRect.HasValue && !anchors.SystemTrayRect.HasValue)
                return;

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
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                    | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
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
            int padding = (int)(HorizontalPaddingDip * dpiScale);
            int taskbarWidth = taskbarRect.right - taskbarRect.left;

            // "Left" alignment is only offered on a centered taskbar, where it sits in the empty area
            // to the left of the centered cluster; every other case hugs the right-hand anchor.
            bool leftOnCentered = _settings.TaskbarWindowAlignment == "Left" && _windowsPolicy.IsTaskbarCenterAligned();

            if (anchors.WidgetsRect.HasValue)
            {
                var widgets = anchors.WidgetsRect.Value;
                return leftOnCentered
                    ? ToClientX(taskbarHandle, (int)widgets.Right) + padding
                    : ToClientX(taskbarHandle, (int)widgets.Left) - widgetWidth - padding;
            }

            if (anchors.SystemTrayRect.HasValue)
            {
                return leftOnCentered
                    ? 0
                    : ToClientX(taskbarHandle, (int)anchors.SystemTrayRect.Value.Left) - widgetWidth - padding;
            }

            // Neither anchor available: fall back to the far corner matching the alignment.
            return leftOnCentered ? 0 : taskbarWidth - widgetWidth - padding;
        }

        private static int ToClientX(IntPtr taskbarHandle, int screenX)
        {
            var pt = new System.Drawing.Point(screenX, 0);
            PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
            return pt.X;
        }

        private static AnchorRects ResolveAnchorRects(IntPtr taskbarHandle)
        {
            var widgetsRect = FindTaskbarElementRect(taskbarHandle, "WidgetsButton", "Widgets button");
            var systemTrayRect = widgetsRect.HasValue
                ? null
                : FindTaskbarElementRect(taskbarHandle, "SystemTrayIcon", "System Tray");
            return new AnchorRects(widgetsRect, systemTrayRect);
        }

        private readonly struct AnchorRects(Rect? widgetsRect, Rect? systemTrayRect)
        {
            public Rect? WidgetsRect { get; } = widgetsRect;
            public Rect? SystemTrayRect { get; } = systemTrayRect;
        }

        private static Rect? FindTaskbarElementRect(IntPtr taskbarHandle, string automationId, string description)
        {
            try
            {
                var taskbarElement = AutomationElement.FromHandle(taskbarHandle);
                var element = taskbarElement?.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, automationId)
                );

                if (element != null)
                {
                    var rect = element.Current.BoundingRectangle;
                    if (!rect.IsEmpty)
                        return rect;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"Could not find {description}");
            }

            return null;
        }
    }
}
