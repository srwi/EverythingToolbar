using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.WindowsAndMessaging;
using DpiChangedEventArgs = System.Windows.DpiChangedEventArgs;
using FlowDirection = System.Windows.FlowDirection;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

namespace EverythingToolbar.Behaviors
{
    public class SearchWindowPlacement : Behavior<SearchWindow>
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchWindowPlacement>();

        public FrameworkElement? PlacementTarget { get; set; }

        private double _pixelsPerDip = 1.0;
        private readonly TaskbarInfoProvider _taskbarState;
        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;

        public SearchWindowPlacement(TaskbarInfoProvider taskbarState, ISettings settings, WindowsPolicy windowsPolicy)
        {
            _taskbarState = taskbarState;
            _settings = settings;
            _windowsPolicy = windowsPolicy;
        }

        protected override void OnAttached()
        {
            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;
            AssociatedObject.DpiChanged += OnDpiChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Showing -= OnShowing;
            AssociatedObject.Hiding -= OnHiding;
            AssociatedObject.DpiChanged -= OnDpiChanged;
        }

        private void OnHiding(object? sender, EventArgs e)
        {
            AssociatedObject.AnimateHide(_taskbarState.TaskbarEdge);
        }

        private void OnShowing(object? sender, ShowingEventArgs e)
        {
            var useCursor = e.AtCursor || PlacementTarget == null;

            RECT placementTargetRect = default;
            if (!useCursor && !TryGetPlacementTargetRect(out placementTargetRect))
            {
                Logger.Error("Failed to get PlacementTarget bounds. Falling back to cursor placement.");
                useCursor = true;
            }

            // Everything below works in physical pixels, so the scale has to come from the monitor the
            // window is about to appear on, not from the placement target pinned to the primary taskbar.
            var screen = useCursor
                ? Screen.FromPoint(Cursor.Position)
                : Screen.FromPoint(new Point(placementTargetRect.left, placementTargetRect.top));
            _pixelsPerDip = GetPixelsPerDip(screen);

            EnsureWindowDpiTransition(screen);

            var position = useCursor
                ? CalculatePositionFromTaskbar(screen)
                : CalculatePositionFromTarget(placementTargetRect, screen);

            var size = GetTargetWindowSizeDip();

            AssociatedObject.AnimateShow(
                position.X / _pixelsPerDip,
                position.Y / _pixelsPerDip,
                size.Width,
                size.Height,
                _taskbarState.TaskbarEdge
            );

            // WPF only pushes Width/Height to the handle when they change, converted with the scale the window
            // last rendered at - both wrong after a scaling change. Enforce the physical size directly.
            ApplyWindowSize();
        }

        /// <summary>
        /// Windows only transitions a window to a monitor's DPI while its rect overlaps that monitor and the
        /// window is not hidden, so a window parked off-screen keeps the DPI of the monitor it was last shown
        /// on. Left alone, the transition would happen mid show animation, where WPF's WM_DPICHANGED handling
        /// visibly rescales the window. Instead, hop the still-parked window onto the bottom few pixels of the
        /// target monitor - behind a bottom-docked taskbar and overlapping no other monitor - which delivers
        /// the transition before the window can be seen.
        /// </summary>
        private void EnsureWindowDpiTransition(Screen screen)
        {
            var hwnd = new WindowInteropHelper(AssociatedObject).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            if (PInvoke.GetDpiForWindow((HWND)hwnd) == (uint)Math.Round(_pixelsPerDip * 96))
                return;

            var bounds = screen.Bounds;
            PInvoke.SetWindowPos(
                (HWND)hwnd,
                HWND.Null,
                bounds.Left,
                bounds.Bottom - 8,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                    | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            );
        }

        private void OnDpiChanged(object? sender, DpiChangedEventArgs e)
        {
            // WPF's own WM_DPICHANGED handling runs after this event and may resize the window or rewrite
            // Width/Height from a stale handle size, so repair its results once it is done. This also covers
            // scaling changes while the popup is open and transitions Windows delivers late or not at all.
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Background, ReconcileAfterDpiChange);
        }

        private void ReconcileAfterDpiChange()
        {
            var hwnd = new WindowInteropHelper(AssociatedObject).Handle;
            if (!AssociatedObject.IsVisible || hwnd == IntPtr.Zero)
                return;

            // The transition has completed, so the window's own DPI is trustworthy again.
            _pixelsPerDip = PInvoke.GetDpiForWindow((HWND)hwnd) / 96.0;
            ApplyWindowSize();
        }

        /// <summary>
        /// Writes the intended size to both places WPF fails to keep in step across DPI transitions:
        /// the Width/Height properties (which WPF may have rewritten from a stale handle size) and the
        /// handle itself (which WPF never resizes once Width/Height stop changing).
        /// </summary>
        private void ApplyWindowSize()
        {
            var size = GetTargetWindowSizeDip();
            AssociatedObject.Width = size.Width;
            AssociatedObject.Height = size.Height;

            var hwnd = new WindowInteropHelper(AssociatedObject).Handle;
            if (hwnd == IntPtr.Zero || !PInvoke.GetWindowRect((HWND)hwnd, out var rect))
                return;

            var width = (int)Math.Round(size.Width * _pixelsPerDip);
            var height = (int)Math.Round(size.Height * _pixelsPerDip);

            if (Math.Abs(rect.right - rect.left - width) <= 1 && Math.Abs(rect.bottom - rect.top - height) <= 1)
                return;

            PInvoke.SetWindowPos(
                (HWND)hwnd,
                HWND.Null,
                0,
                0,
                width,
                height,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                    | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
            );
        }

        private Point CalculatePositionFromTarget(RECT nativeRect, Screen screen)
        {
            var workingArea = screen.WorkingArea;
            var screenBounds = screen.Bounds;
            var (width, height) = GetTargetWindowSize();
            var taskbarSize = _taskbarState.TaskbarSize;
            var margin = GetMargin();

            switch (_taskbarState.TaskbarEdge)
            {
                case Edge.Bottom:
                case Edge.Top:
                {
                    var topDockPos = Math.Max(workingArea.Top, screenBounds.Top + (int)taskbarSize.Height);
                    var bottomDockPos = Math.Min(workingArea.Bottom, screenBounds.Bottom - (int)taskbarSize.Height);

                    var right = Math.Min(nativeRect.left + width, workingArea.Right - margin);
                    var bottom = Math.Min(nativeRect.top - margin, bottomDockPos - margin);

                    return new Point(
                        Math.Max(workingArea.Left + margin, right - width),
                        Math.Max(topDockPos + margin, bottom - height)
                    );
                }
                case Edge.Left:
                case Edge.Right:
                {
                    var leftDockPos = Math.Max(workingArea.Left, screenBounds.Left + (int)taskbarSize.Width);
                    var rightDockPos = Math.Min(workingArea.Right, screenBounds.Right - (int)taskbarSize.Width);

                    var bottom = Math.Min(nativeRect.top + height, workingArea.Bottom - margin);
                    var right = Math.Min(nativeRect.left - margin, rightDockPos - margin);

                    return new Point(
                        Math.Max(leftDockPos + margin, right - width),
                        Math.Max(workingArea.Top + margin, bottom - height)
                    );
                }
                default:
                    return new Point();
            }
        }

        private Point CalculatePositionFromTaskbar(Screen screen)
        {
            var taskbar = FindDockedTaskBar(screen);
            var (width, height) = GetTargetWindowSize();
            var margin = GetMargin();
            var workingArea = screen.WorkingArea;

            _taskbarState.TaskbarEdge = taskbar.Edge;

            switch (taskbar.Edge)
            {
                case Edge.Top:
                    return new Point(
                        GetHorizontalPosition(workingArea, width, margin),
                        taskbar.Position.Bottom + margin
                    );
                case Edge.Bottom:
                    return new Point(
                        GetHorizontalPosition(workingArea, width, margin),
                        Math.Max(workingArea.Top + margin, taskbar.Position.Y - margin - height)
                    );
                case Edge.Left:
                    return new Point(taskbar.Position.Right + margin, workingArea.Top + margin);
                default:
                    return new Point(
                        Math.Max(taskbar.Position.Left - margin - width, workingArea.Left + margin),
                        workingArea.Top + margin
                    );
            }
        }

        private Size GetTargetWindowSizeDip()
        {
            return new Size(
                Math.Max(_settings.PopupWidth, AssociatedObject.MinWidth),
                Math.Max(_settings.PopupHeight, AssociatedObject.MinHeight)
            );
        }

        private (int Width, int Height) GetTargetWindowSize()
        {
            var size = GetTargetWindowSizeDip();
            return ((int)(size.Width * _pixelsPerDip), (int)(size.Height * _pixelsPerDip));
        }

        private int GetHorizontalPosition(Rectangle workingArea, int width, int margin)
        {
            if (_windowsPolicy.IsTaskbarCenterAligned())
                return Math.Max(workingArea.Left + margin, workingArea.Left + (workingArea.Width - width) / 2);

            if (AssociatedObject.FlowDirection == FlowDirection.RightToLeft)
                return Math.Max(workingArea.Right - margin - width, workingArea.Left + margin);

            return workingArea.Left + margin;
        }

        private TaskbarLocation FindDockedTaskBar(Screen screen)
        {
            // An auto-hiding taskbar reserves no work area, so the geometry below cannot see it at all.
            if (
                NativeMethods.IsTaskbarAutoHiding()
                && NativeMethods.TryGetTaskbarPosition(out var edge, out var thickness)
            )
                return CreateTaskbarLocation(screen, ToEdge(edge), RescaleFromPrimary(thickness));

            var topDockedHeight = screen.WorkingArea.Top - screen.Bounds.Top;
            var bottomDockedHeight = screen.Bounds.Bottom - screen.WorkingArea.Bottom;
            var leftDockedWidth = screen.WorkingArea.Left - screen.Bounds.Left;
            var rightDockedWidth = screen.Bounds.Right - screen.WorkingArea.Right;

            if (leftDockedWidth > 0 && bottomDockedHeight == 0)
                return CreateTaskbarLocation(screen, Edge.Left, leftDockedWidth);
            if (rightDockedWidth > 0 && bottomDockedHeight == 0)
                return CreateTaskbarLocation(screen, Edge.Right, rightDockedWidth);
            if (topDockedHeight > 0 && bottomDockedHeight == 0)
                return CreateTaskbarLocation(screen, Edge.Top, topDockedHeight);

            return CreateTaskbarLocation(screen, Edge.Bottom, bottomDockedHeight);
        }

        private static Edge ToEdge(uint appBarEdge) =>
            appBarEdge switch
            {
                0 => Edge.Left,
                1 => Edge.Top,
                2 => Edge.Right,
                _ => Edge.Bottom,
            };

        private static TaskbarLocation CreateTaskbarLocation(Screen screen, Edge edge, int thickness)
        {
            var bounds = screen.Bounds;
            return new TaskbarLocation
            {
                Position = edge switch
                {
                    Edge.Left => new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height),
                    Edge.Right => new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height),
                    Edge.Top => new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness),
                    _ => new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness),
                },
                Edge = edge,
            };
        }

        private int RescaleFromPrimary(int thickness)
        {
            if (Screen.PrimaryScreen is not { } primary)
                return thickness;

            return (int)Math.Round(thickness / GetPixelsPerDip(primary) * _pixelsPerDip);
        }

        private bool TryGetPlacementTargetRect(out RECT rect)
        {
            rect = default;

            if (
                PlacementTarget == null
                || PresentationSource.FromVisual(PlacementTarget) as HwndSource is not { } hwndSource
            )
            {
                return false;
            }

            return PInvoke.GetWindowRect((HWND)hwndSource.Handle, out rect);
        }

        private double GetPixelsPerDip(Screen screen)
        {
            // Deliberately not GetDpiForWindow: the window may still sit on the monitor it was last shown on.
            var monitor = PInvoke.MonitorFromPoint(
                new Point(screen.Bounds.Left, screen.Bounds.Top),
                MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST
            );

            if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded)
                return dpiX / 96.0;

            Logger.Error("Failed to get display scaling factor. This may result in incorrect window placement.");
            return 1.0;
        }

        private int GetMargin()
        {
            var marginDip = _windowsPolicy.GetEffectiveWindowsVersion() >= WindowsVersion.Windows11 ? 12 : 0;
            return (int)Math.Round(marginDip * _pixelsPerDip);
        }

        private struct TaskbarLocation
        {
            public Rectangle Position;
            public Edge Edge;
        }
    }
}
