using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;
using AutomationCondition = System.Windows.Automation.Condition;

namespace EverythingToolbar
{
    public partial class TaskbarWindow
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<TaskbarWindow>();
        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;

        private IntPtr _taskbarHandle;
        private int _positionGeneration;
        private bool _isWidgetHidden;

        private WidgetBounds? _currentBounds;
        private WidgetBounds? _targetBounds;
        private WidgetBounds _animationFrom;

        private WINEVENTPROC? _taskbarEventCallback;
        private HWINEVENTHOOK _taskbarLocationHook;
        private readonly DispatcherTimer _repositionDebounceTimer;

        private const double MaxWidgetWidthDip = 300;

        // Narrower than this the box is little more than its icon, so it hides instead.
        private const double MinWidgetWidthDip = 120;
        private const double MinWidgetHeightDip = 32;
        private const double WidgetVerticalMarginDip = 6;
        private const double HorizontalPaddingDip = 8;

        // Taskbar children this much of the bar are containers spanning it, not icons.
        private const double MaxIconClusterChildWidthRatio = 0.6;

        private const string TaskbarFrameAutomationId = "TaskbarFrame";
        private const string SystemTrayIconAutomationId = "SystemTrayIcon";
        private const string WidgetsButtonAutomationId = "WidgetsButton";

        private const int RepositionDebounceMilliseconds = 150;
        private static readonly TimeSpan PlacementAnimationDuration = TimeSpan.FromMilliseconds(250);

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WM_SHOWWINDOW = 0x0018;
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private const int WM_NCCALCSIZE = 0x0083;

        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        // The box is placed with SetWindowPos rather than through WPF layout, so the transition animates
        // one progress value and interpolates it into the bounds.
        private static readonly DependencyProperty PlacementProgressProperty = DependencyProperty.Register(
            "PlacementProgress",
            typeof(double),
            typeof(TaskbarWindow),
            new PropertyMetadata(0.0, OnPlacementProgressChanged)
        );

        private IntPtr WindowHandle => new WindowInteropHelper(this).Handle;

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
            StopPlacementAnimation();
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
                var hwnd = WindowHandle;
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
                var hwnd = WindowHandle;
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
                    var layout = ResolveTaskbarLayout(taskbarHandle);
                    Dispatcher.BeginInvoke(() =>
                    {
                        // Drop stale results if a newer UpdatePosition ran or the box went away.
                        if (generation != _positionGeneration || !IsLoaded)
                            return;
                        ApplyPosition(taskbarHandle, layout);
                    });
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating position");
            }
        }

        private void ApplyPosition(IntPtr taskbarHandle, TaskbarLayout layout)
        {
            // While the taskbar is auto-hidden or mid-reveal nothing on it is measurable. Keep the last
            // good position instead of collapsing onto the far-left corner.
            if (!layout.IsMeasurable)
                return;

            if (!PInvoke.GetWindowRect((HWND)taskbarHandle, out var taskbarRect))
                return;

            double dpiScale = NativeMethods.GetDpiForWindow(taskbarHandle) / 96.0;

            var (left, width) = CalculateHorizontalPlacement(taskbarHandle, taskbarRect, layout, dpiScale);
            if (width <= 0)
            {
                HideWidget();
                return;
            }

            int taskbarHeight = taskbarRect.bottom - taskbarRect.top;
            int verticalMargin = (int)(WidgetVerticalMarginDip * dpiScale);
            int height = Math.Max(taskbarHeight - 2 * verticalMargin, (int)(MinWidgetHeightDip * dpiScale));

            MoveWidget(new WidgetBounds(left, (taskbarHeight - height) / 2, width, height));
        }

        private void MoveWidget(WidgetBounds target)
        {
            if (_targetBounds == target)
                return;

            // A box that is not on the taskbar yet has nothing to glide from.
            bool animate = !_isWidgetHidden && _currentBounds.HasValue && !_windowsPolicy.IsEffectiveAnimationsDisabled;

            _targetBounds = target;
            _isWidgetHidden = false;

            if (!animate)
            {
                // Pinning the start at the target keeps the progress reset below from moving the box.
                _animationFrom = target;
                StopPlacementAnimation();
                ApplyBounds(target);
                return;
            }

            // Starting wherever the box is right now lets a target picked up mid-flight continue smoothly.
            _animationFrom = _currentBounds ?? target;

            BeginAnimation(
                PlacementProgressProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = PlacementAnimationDuration,
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );
        }

        private void HideWidget()
        {
            if (_isWidgetHidden)
                return;

            _isWidgetHidden = true;
            Logger.Debug("Not enough free space on the taskbar; hiding the search box.");

            // The animation would keep re-showing the box through SWP_SHOWWINDOW, and the bounds it was
            // heading for must not suppress the next move.
            _targetBounds = null;
            StopPlacementAnimation();

            PInvoke.SetWindowPos(
                (HWND)WindowHandle,
                (HWND)IntPtr.Zero,
                0,
                0,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                    | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                    | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                    | SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW
            );
        }

        // A finished animation holds its end value without ticking, so it is only cancelled where that
        // held value would get in the way.
        private void StopPlacementAnimation() => BeginAnimation(PlacementProgressProperty, null);

        private static void OnPlacementProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = (TaskbarWindow)d;
            if (window._targetBounds is { } target)
                window.ApplyBounds(WidgetBounds.Lerp(window._animationFrom, target, (double)e.NewValue));
        }

        private void ApplyBounds(WidgetBounds bounds)
        {
            _currentBounds = bounds;

            PInvoke.SetWindowPos(
                (HWND)WindowHandle,
                (HWND)IntPtr.Zero,
                bounds.Left,
                bounds.Top,
                bounds.Width,
                bounds.Height,
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                    | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
            );
        }

        private readonly record struct WidgetBounds(int Left, int Top, int Width, int Height)
        {
            public static WidgetBounds Lerp(WidgetBounds from, WidgetBounds to, double progress) =>
                new(
                    Interpolate(from.Left, to.Left, progress),
                    Interpolate(from.Top, to.Top, progress),
                    Interpolate(from.Width, to.Width, progress),
                    Interpolate(from.Height, to.Height, progress)
                );

            private static int Interpolate(int from, int to, double progress) =>
                (int)Math.Round(from + (to - from) * progress);
        }

        /// <summary>
        /// Fits the box into the free stretch of taskbar between the icon cluster and the nearest thing
        /// on the aligned side of it. Returns a width of zero when that gap is too narrow to be useful.
        /// </summary>
        private (int Left, int Width) CalculateHorizontalPlacement(
            IntPtr taskbarHandle,
            RECT taskbarRect,
            TaskbarLayout layout,
            double dpiScale
        )
        {
            // "Left" alignment is only offered on a centered taskbar, where the box sits in the empty
            // area to the left of the centered cluster; every other case fills the space to its right.
            bool leftOnCentered = _settings.TaskbarWindowAlignment == "Left" && _windowsPolicy.IsTaskbarCenterAligned();

            // The gap runs from the cluster edge to the nearest obstacle beyond it, falling back to the
            // taskbar edge. An unmeasurable cluster widens it to the whole taskbar, which still lands the
            // box in the aligned corner because it is capped at its maximum width below.
            double gapStart;
            double gapEnd;

            if (leftOnCentered)
            {
                gapEnd = layout.IconCluster?.Left ?? taskbarRect.right;
                gapStart = layout.Obstacles.Select(o => o.Right).Where(x => x <= gapEnd).Append(taskbarRect.left).Max();
            }
            else
            {
                gapStart = layout.IconCluster?.Right ?? taskbarRect.left;
                gapEnd = layout.Obstacles.Select(o => o.Left).Where(x => x >= gapStart).Append(taskbarRect.right).Min();
            }

            int padding = (int)(HorizontalPaddingDip * dpiScale);
            int startClient = ToClientX(taskbarHandle, (int)Math.Round(gapStart));
            int endClient = ToClientX(taskbarHandle, (int)Math.Round(gapEnd));

            int gapLeft = Math.Min(startClient, endClient) + padding;
            int gapRight = Math.Max(startClient, endClient) - padding;

            int available = gapRight - gapLeft;
            if (available < (int)(MinWidgetWidthDip * dpiScale))
                return (0, 0);

            int width = Math.Min(available, (int)(MaxWidgetWidthDip * dpiScale));
            return (leftOnCentered ? gapLeft : gapRight - width, width);
        }

        private static int ToClientX(IntPtr taskbarHandle, int screenX)
        {
            var pt = new System.Drawing.Point(screenX, 0);
            PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
            return pt.X;
        }

        /// <summary>
        /// Measures the taskbar in screen pixels: the bounds of the icon cluster (Start button plus the
        /// task buttons) and the elements the box must not overlap on either side of it.
        /// </summary>
        private static TaskbarLayout ResolveTaskbarLayout(IntPtr taskbarHandle)
        {
            var obstacles = new List<Rect>();
            Rect? iconCluster = null;

            try
            {
                var taskbar = AutomationElement.FromHandle(taskbarHandle);

                // Depending on the Windows build the tray icons sit outside the taskbar frame, so they
                // are collected from the root rather than from among the frame's children.
                foreach (
                    AutomationElement icon in taskbar.FindAll(TreeScope.Descendants, ById(SystemTrayIconAutomationId))
                )
                {
                    var iconRect = icon.Current.BoundingRectangle;
                    if (iconRect.Width > 0)
                        obstacles.Add(iconRect);
                }

                var frame = taskbar.FindFirst(TreeScope.Descendants, ById(TaskbarFrameAutomationId));
                if (frame != null)
                {
                    double maxIconWidth = taskbar.Current.BoundingRectangle.Width * MaxIconClusterChildWidthRatio;

                    foreach (
                        AutomationElement child in frame.FindAll(TreeScope.Children, AutomationCondition.TrueCondition)
                    )
                    {
                        var rect = child.Current.BoundingRectangle;
                        if (rect.Width <= 0)
                            continue;

                        if (child.Current.AutomationId is SystemTrayIconAutomationId or WidgetsButtonAutomationId)
                            obstacles.Add(rect);
                        else if (rect.Width <= maxIconWidth)
                            iconCluster = iconCluster.HasValue ? Rect.Union(iconCluster.Value, rect) : rect;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not measure the taskbar layout");
            }

            return new TaskbarLayout(iconCluster, obstacles);
        }

        private static PropertyCondition ById(string automationId) =>
            new(AutomationElement.AutomationIdProperty, automationId);

        private readonly record struct TaskbarLayout(Rect? IconCluster, IReadOnlyList<Rect> Obstacles)
        {
            public bool IsMeasurable => IconCluster.HasValue || Obstacles.Count > 0;
        }
    }
}
