using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Xaml.Behaviors;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
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
            AssociatedObject.Left = 100000;
            AssociatedObject.Top = 100000;

            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Showing -= OnShowing;
            AssociatedObject.Hiding -= OnHiding;
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
                    return new Point(GetHorizontalPosition(workingArea, width, margin), taskbar.Position.Bottom + margin);
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
            if (NativeMethods.IsTaskbarAutoHiding() && NativeMethods.TryGetTaskbarPosition(out var edge, out var thickness))
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
