using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        private HWND _handle;
        private IntPtr _taskbarHandle;
        private int _positionGeneration;
        private TaskbarWindowAnimator? _animator;

        private WINEVENTPROC? _taskbarEventCallback;
        private HWINEVENTHOOK _taskbarLocationHook;
        private readonly DispatcherTimer _repositionTimer;
        private readonly TaskbarLayoutProbe _layoutProbe = new();
        private DateTime _settleDeadline;
        private bool _refreshLayoutElements = true;

        private const double MaxSearchBoxWidthDip = 300;

        // Narrower than this the full search box cannot comfortably fit, so it switches to icon mode.
        private const double MinSearchBoxWidthDip = 120;
        private const double MinWidgetHeightDip = 32;
        private const double MaxWidgetHeightDip = 48;
        private const double SearchBoxVerticalMarginDip = 6;
        private const double IconButtonMarginDip = 4.5;
        private const double HorizontalPaddingDip = 8;

        private const int RepositionIntervalMilliseconds = 150;

        // The taskbar reports a layout change before it animates the buttons into their new places.
        // On window close the only event arrives well before the reflow even starts, so a single
        // measurement would read the old geometry. Keep re-measuring for this long instead.
        private static readonly TimeSpan TaskbarSettleWindow = TimeSpan.FromMilliseconds(1200);

        public FrameworkElement PlacementTarget => ToolbarControl;

        public bool IsAttachedToTaskbar { get; private set; }

        public TaskbarWindow(WindowsPolicy windowsPolicy, ISettings settings)
        {
            _windowsPolicy = windowsPolicy;
            _settings = settings;

            InitializeComponent();

            _repositionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(RepositionIntervalMilliseconds),
            };
            _repositionTimer.Tick += OnRepositionTick;

            Loaded += OnLoaded;
            _settings.PropertyChanged += OnSettingsChanged;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _handle = (HWND)new WindowInteropHelper(this).Handle;
            _animator = new TaskbarWindowAnimator(_handle, () => _windowsPolicy.IsEffectiveAnimationsDisabled);

            SetupAsTaskbarChild();
        }

        protected override void OnClosed(EventArgs e)
        {
            _repositionTimer.Stop();
            _animator?.StopAnimation();
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
                if (_handle.IsNull)
                    return;

                _taskbarHandle = NativeMethods.FindTaskbarHandle();
                if (_taskbarHandle == IntPtr.Zero)
                {
                    Logger.Warn("Could not find taskbar handle");
                    return;
                }

                var style = (WINDOW_STYLE)PInvoke.GetWindowLong(_handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
                style = (style & ~WINDOW_STYLE.WS_POPUP) | WINDOW_STYLE.WS_CHILD;
                PInvoke.SetWindowLong(_handle, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int)style);

                var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLong(_handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                exStyle |= WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;
                PInvoke.SetWindowLong(_handle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)exStyle);

                PInvoke.SetParent(_handle, (HWND)_taskbarHandle);
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
                PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
                PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
                default(HMODULE),
                _taskbarEventCallback,
                processId,
                threadId,
                PInvoke.WINEVENT_OUTOFCONTEXT
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
            // The taskbar emits location changes in bursts; coalesce them and reposition once they
            // stop. Marshal onto the UI thread to touch the timer.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(RestartRepositionTimer);
                return;
            }

            RestartRepositionTimer();
        }

        private void RestartRepositionTimer()
        {
            _settleDeadline = DateTime.UtcNow + TaskbarSettleWindow;
            _repositionTimer.Stop();
            _repositionTimer.Start();
        }

        private void OnRepositionTick(object? sender, EventArgs e)
        {
            _repositionTimer.Stop();

            var refreshElements = _refreshLayoutElements;
            _refreshLayoutElements = true;
            UpdatePosition(refreshElements);
        }

        private void UpdatePosition(bool refreshLayoutElements = true)
        {
            if (!IsLoaded || _handle.IsNull || !_settings.TaskbarWindowEnabled)
                return;

            if (_taskbarHandle == IntPtr.Zero)
                _taskbarHandle = NativeMethods.FindTaskbarHandle();

            if (_taskbarHandle == IntPtr.Zero)
            {
                Logger.Warn("Could not find taskbar handle");
                return;
            }

            if (PInvoke.GetParent(_handle) != _taskbarHandle)
                PInvoke.SetParent(_handle, (HWND)_taskbarHandle);

            var taskbarHandle = _taskbarHandle;
            var generation = ++_positionGeneration;

            // Measuring the taskbar walks its automation tree, which is far too slow for the UI thread.
            Task.Run(() =>
            {
                var layout = _layoutProbe.Measure(taskbarHandle, refreshLayoutElements);
                Dispatcher.BeginInvoke(() => ApplyMeasuredLayout(taskbarHandle, layout, generation));
            });
        }

        private void ApplyMeasuredLayout(IntPtr taskbarHandle, TaskbarLayout layout, int generation)
        {
            // Drop stale results if a newer UpdatePosition ran or the box went away.
            if (generation != _positionGeneration || !IsLoaded)
                return;

            // Re-check until the taskbar has finished moving; a new burst of events pushes the deadline.
            if (DateTime.UtcNow < _settleDeadline)
            {
                _refreshLayoutElements = false;
                _repositionTimer.Start();
            }

            // While the taskbar is auto-hidden or mid-reveal nothing on it is measurable. Keep the last
            // good position instead of collapsing onto the far-left corner.
            if (!layout.IsMeasurable)
                return;

            try
            {
                if (!PInvoke.GetWindowRect((HWND)taskbarHandle, out var taskbarRect))
                    return;

                if (CalculateBounds(taskbarHandle, taskbarRect, layout) is { } bounds)
                {
                    _animator?.MoveTo(bounds);
                }
                else if (_animator?.Hide() == true)
                    Logger.Debug("Not enough free space on the taskbar; hiding the search box.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error updating position");
            }
        }

        /// <summary>
        /// Fits the box into the free stretch of taskbar between the icon cluster and the nearest thing
        /// on the aligned side of it. Returns null when that gap is too narrow to be useful.
        /// </summary>
        private WidgetBounds? CalculateBounds(IntPtr taskbarHandle, RECT taskbarRect, TaskbarLayout layout)
        {
            double dpiScale = NativeMethods.GetDpiForWindow(taskbarHandle) / 96.0;

            int taskbarWidth = taskbarRect.right - taskbarRect.left;
            int taskbarHeight = taskbarRect.bottom - taskbarRect.top;
            bool isVertical = taskbarHeight > taskbarWidth;

            if (isVertical)
                return CalculateBoundsVertical(taskbarHandle, taskbarRect, layout, dpiScale, taskbarWidth);

            return CalculateBoundsHorizontal(taskbarHandle, taskbarRect, layout, dpiScale, taskbarHeight);
        }

        private WidgetBounds? CalculateBoundsHorizontal(
            IntPtr taskbarHandle,
            RECT taskbarRect,
            TaskbarLayout layout,
            double dpiScale,
            int taskbarHeight
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
            var (gapLeft, gapRight) = ProjectSpan(taskbarHandle, gapStart, gapEnd, padding, isVertical: false);
            int available = gapRight - gapLeft;

            int minHeight = (int)(MinWidgetHeightDip * dpiScale);
            int maxHeight = (int)(MaxWidgetHeightDip * dpiScale);

            int width;
            int height;

            if (available < (int)(MinSearchBoxWidthDip * dpiScale))
            {
                int iconMargin = (int)Math.Round(IconButtonMarginDip * dpiScale);
                int iconSize = Math.Clamp(taskbarHeight - 2 * iconMargin, minHeight, maxHeight);
                if (available < iconSize)
                    return null;

                width = iconSize;
                height = iconSize;
            }
            else
            {
                int searchBoxMargin = (int)(SearchBoxVerticalMarginDip * dpiScale);
                width = Math.Min(available, (int)(MaxSearchBoxWidthDip * dpiScale));
                height = Math.Max(taskbarHeight - 2 * searchBoxMargin, minHeight);
            }

            return new WidgetBounds(
                leftOnCentered ? gapLeft : gapRight - width,
                (taskbarHeight - height) / 2,
                width,
                height
            );
        }

        private WidgetBounds? CalculateBoundsVertical(
            IntPtr taskbarHandle,
            RECT taskbarRect,
            TaskbarLayout layout,
            double dpiScale,
            int taskbarWidth
        )
        {
            int padding = (int)(HorizontalPaddingDip * dpiScale);
            int minHeight = (int)(MinWidgetHeightDip * dpiScale);
            int maxHeight = (int)(MaxWidgetHeightDip * dpiScale);

            bool alignTop = _settings.TaskbarWindowAlignment == "Left";

            double gapStart;
            double gapEnd;

            if (
                alignTop
                && layout.IconCluster is { } cluster
                && cluster.Top - taskbarRect.top >= minHeight + 2 * padding
            )
            {
                gapEnd = cluster.Top;
                gapStart = layout.Obstacles.Select(o => o.Bottom).Where(y => y <= gapEnd).Append(taskbarRect.top).Max();
            }
            else
            {
                gapStart = layout.IconCluster?.Bottom ?? taskbarRect.top;
                gapEnd = layout.Obstacles.Select(o => o.Top).Where(y => y >= gapStart).Append(taskbarRect.bottom).Min();
            }

            var (gapTop, gapBottom) = ProjectSpan(taskbarHandle, gapStart, gapEnd, padding, isVertical: true);
            int available = gapBottom - gapTop;

            if (available < minHeight && alignTop && layout.IconCluster is not null)
            {
                gapStart = layout.IconCluster.Value.Bottom;
                gapEnd = layout.Obstacles.Select(o => o.Top).Where(y => y >= gapStart).Append(taskbarRect.bottom).Min();
                (gapTop, gapBottom) = ProjectSpan(taskbarHandle, gapStart, gapEnd, padding, isVertical: true);
                available = gapBottom - gapTop;
            }

            int iconMargin = (int)Math.Round(IconButtonMarginDip * dpiScale);
            int buttonSize = Math.Clamp(taskbarWidth - 2 * iconMargin, minHeight, maxHeight);
            if (available < buttonSize)
                return null;

            int width = buttonSize;
            int height = Math.Min(available, buttonSize);
            int x = (taskbarWidth - width) / 2;
            return new WidgetBounds(x, gapTop, width, height);
        }

        private static (int Start, int End) ProjectSpan(
            IntPtr taskbarHandle,
            double screenStart,
            double screenEnd,
            int padding,
            bool isVertical
        )
        {
            int startClient = isVertical
                ? ToClient(taskbarHandle, 0, (int)Math.Round(screenStart)).Y
                : ToClient(taskbarHandle, (int)Math.Round(screenStart), 0).X;
            int endClient = isVertical
                ? ToClient(taskbarHandle, 0, (int)Math.Round(screenEnd)).Y
                : ToClient(taskbarHandle, (int)Math.Round(screenEnd), 0).X;

            int spanStart = Math.Min(startClient, endClient) + padding;
            int spanEnd = Math.Max(startClient, endClient) - padding;
            return (spanStart, spanEnd);
        }

        private static System.Drawing.Point ToClient(IntPtr taskbarHandle, int screenX, int screenY)
        {
            var pt = new System.Drawing.Point(screenX, screenY);
            PInvoke.ScreenToClient((HWND)taskbarHandle, ref pt);
            return pt;
        }
    }
}
