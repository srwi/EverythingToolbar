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

        public FrameworkElement? PlacementTarget
        {
            get => _placementTarget;
            set
            {
                if (ReferenceEquals(_placementTarget, value))
                    return;

                // The target can be swapped after attaching, so the subscription has to move with it.
                if (_isAttached && _placementTarget != null)
                    _placementTarget.Loaded -= OnPlacementTargetLoaded;

                _placementTarget = value;

                if (_isAttached && _placementTarget != null)
                    _placementTarget.Loaded += OnPlacementTargetLoaded;
            }
        }

        private FrameworkElement? _placementTarget;
        private bool _isAttached;
        private double _dpiScalingFactor = 1.0;
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

            _isAttached = true;

            if (_placementTarget != null)
                _placementTarget.Loaded += OnPlacementTargetLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Showing -= OnShowing;
            AssociatedObject.Hiding -= OnHiding;

            if (_placementTarget != null)
                _placementTarget.Loaded -= OnPlacementTargetLoaded;

            _isAttached = false;
        }

        private void OnPlacementTargetLoaded(object sender, RoutedEventArgs e)
        {
            if (TryGetPlacementTargetScreen() is { } screen)
                _dpiScalingFactor = GetScalingFactor(screen);
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

            // Every calculation below works in physical pixels, so the scaling factor has to come from the
            // monitor the window is about to appear on. The placement target lives on the primary taskbar
            // and is the wrong reference whenever the window opens at a cursor on another monitor.
            var screen = useCursor
                ? Screen.FromPoint(Cursor.Position)
                : Screen.FromPoint(new Point(placementTargetRect.left, placementTargetRect.top));
            _dpiScalingFactor = GetScalingFactor(screen);

            var position = useCursor
                ? CalculatePositionFromTaskbar(screen)
                : CalculatePositionFromTarget(placementTargetRect, screen);

            var size = GetTargetWindowSizeDip();

            AssociatedObject.AnimateShow(
                position.left * _dpiScalingFactor,
                position.top * _dpiScalingFactor,
                size.Width,
                size.Height,
                _taskbarState.TaskbarEdge
            );
        }

        private RECT CalculatePositionFromTarget(RECT nativeRect, Screen screen)
        {
            var workingArea = screen.WorkingArea;
            var screenBounds = screen.Bounds;
            var windowSize = GetTargetWindowSize();
            var taskbarSize = _taskbarState.TaskbarSize;
            var margin = GetMargin();

            var windowPosition = new RECT();
            switch (_taskbarState.TaskbarEdge)
            {
                case Edge.Bottom:
                case Edge.Top:
                    var topDockPos = Math.Max(workingArea.Top, screenBounds.Top + (int)taskbarSize.Height);
                    var bottomDockPos = Math.Min(workingArea.Bottom, screenBounds.Bottom - (int)taskbarSize.Height);

                    windowPosition.right = Math.Min(
                        nativeRect.left + (int)windowSize.Width,
                        workingArea.Right - margin
                    );
                    windowPosition.left = Math.Max(
                        workingArea.Left + margin,
                        windowPosition.right - (int)windowSize.Width
                    );
                    windowPosition.right = windowPosition.left + (int)windowSize.Width;

                    windowPosition.bottom = Math.Min(nativeRect.top - margin, bottomDockPos - margin);
                    windowPosition.top = Math.Max(topDockPos + margin, windowPosition.bottom - (int)windowSize.Height);
                    windowPosition.bottom = windowPosition.top + (int)windowSize.Height;
                    break;
                case Edge.Left:
                case Edge.Right:
                    var leftDockPos = Math.Max(workingArea.Left, screenBounds.Left + (int)taskbarSize.Width);
                    var rightDockPos = Math.Min(workingArea.Right, screenBounds.Right - (int)taskbarSize.Width);

                    windowPosition.bottom = Math.Min(
                        nativeRect.top + (int)windowSize.Height,
                        workingArea.Bottom - margin
                    );
                    windowPosition.top = Math.Max(
                        workingArea.Top + margin,
                        windowPosition.bottom - (int)windowSize.Height
                    );
                    windowPosition.bottom = windowPosition.top + (int)windowSize.Height;

                    windowPosition.right = Math.Min(nativeRect.left - margin, rightDockPos - margin);
                    windowPosition.left = Math.Max(leftDockPos + margin, windowPosition.right - (int)windowSize.Width);
                    windowPosition.right = windowPosition.left + (int)windowSize.Width;
                    break;
            }
            return windowPosition;
        }

        private RECT CalculatePositionFromTaskbar(Screen screen)
        {
            var taskbar = FindDockedTaskBar(screen);
            var windowSize = GetTargetWindowSize();
            var margin = GetMargin();

            var windowPosition = new RECT();

            if (taskbar.Edge == Edge.Top)
            {
                windowPosition.top = taskbar.Position.Bottom + margin;
                windowPosition.bottom = Math.Min(
                    windowPosition.top + (int)windowSize.Height,
                    screen.WorkingArea.Bottom - margin
                );
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Bottom)
            {
                windowPosition.bottom = taskbar.Position.Y - margin;
                windowPosition.top = Math.Max(
                    screen.WorkingArea.Top + margin,
                    windowPosition.bottom - (int)windowSize.Height
                );
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Left)
            {
                windowPosition.left = taskbar.Position.Right + margin;
                windowPosition.right = Math.Min(
                    windowPosition.left + (int)windowSize.Width,
                    screen.WorkingArea.Right - margin
                );
                windowPosition.top = margin;
                windowPosition.bottom = Math.Min(
                    windowPosition.top + (int)windowSize.Height,
                    screen.WorkingArea.Bottom - margin
                );
            }
            else if (taskbar.Edge == Edge.Right)
            {
                windowPosition.right = taskbar.Position.Left - margin;
                windowPosition.left = Math.Max(
                    windowPosition.right - (int)windowSize.Width,
                    screen.WorkingArea.Left + margin
                );
                windowPosition.top = margin;
                windowPosition.bottom = Math.Min(
                    windowPosition.top + (int)windowSize.Height,
                    screen.WorkingArea.Bottom - margin
                );
            }

            _taskbarState.TaskbarEdge = taskbar.Edge;

            return windowPosition;
        }

        private Size GetTargetWindowSizeDip()
        {
            return new Size(
                Math.Max(_settings.PopupWidth, AssociatedObject.MinWidth),
                Math.Max(_settings.PopupHeight, AssociatedObject.MinHeight)
            );
        }

        private Size GetTargetWindowSize()
        {
            var windowSize = GetTargetWindowSizeDip();
            return new Size(windowSize.Width / _dpiScalingFactor, windowSize.Height / _dpiScalingFactor);
        }

        private RECT SetHorizontalPosition(
            RECT windowPosition,
            Rectangle screenWorkingArea,
            Size windowSize,
            int margin
        )
        {
            if (_windowsPolicy.IsTaskbarCenterAligned())
            {
                windowPosition.left = screenWorkingArea.Left + (int)((screenWorkingArea.Width - windowSize.Width) / 2);
                windowPosition.left = Math.Max(screenWorkingArea.Left + margin, windowPosition.left);
                windowPosition.right = screenWorkingArea.Left + (int)((screenWorkingArea.Width + windowSize.Width) / 2);
                windowPosition.right = Math.Min(screenWorkingArea.Right - margin, windowPosition.right);
            }
            else
            {
                if (AssociatedObject.FlowDirection == FlowDirection.RightToLeft)
                {
                    windowPosition.right = screenWorkingArea.Right - margin;
                    windowPosition.left = Math.Max(
                        windowPosition.right - (int)windowSize.Width,
                        screenWorkingArea.Left + margin
                    );
                }
                else
                {
                    windowPosition.left = screenWorkingArea.Left + margin;
                    windowPosition.right = Math.Min(
                        windowPosition.left + (int)windowSize.Width,
                        screenWorkingArea.Right - margin
                    );
                }
            }

            return windowPosition;
        }

        private TaskbarLocation FindDockedTaskBar(Screen screen)
        {
            var topDockedHeight = Math.Abs(Math.Abs(screen.Bounds.Top) - Math.Abs(screen.WorkingArea.Top));
            var bottomDockedHeight = screen.Bounds.Height - topDockedHeight - screen.WorkingArea.Height;
            var leftDockedWidth = Math.Abs(Math.Abs(screen.Bounds.Left) - Math.Abs(screen.WorkingArea.Left));
            var rightDockedWidth = screen.Bounds.Width - leftDockedWidth - screen.WorkingArea.Width;

            if (leftDockedWidth > 0 && bottomDockedHeight == 0)
            {
                return new TaskbarLocation
                {
                    Position = new Rectangle(
                        screen.Bounds.Left,
                        screen.Bounds.Top,
                        leftDockedWidth,
                        screen.Bounds.Height
                    ),
                    Edge = Edge.Left,
                };
            }
            if (rightDockedWidth > 0 && bottomDockedHeight == 0)
            {
                return new TaskbarLocation
                {
                    Position = new Rectangle(
                        screen.WorkingArea.Right,
                        screen.Bounds.Top,
                        rightDockedWidth,
                        screen.Bounds.Height
                    ),
                    Edge = Edge.Right,
                };
            }
            if (topDockedHeight > 0 && bottomDockedHeight == 0)
            {
                return new TaskbarLocation
                {
                    Position = new Rectangle(
                        screen.WorkingArea.Left,
                        screen.Bounds.Top,
                        screen.WorkingArea.Width,
                        topDockedHeight
                    ),
                    Edge = Edge.Top,
                };
            }

            return new TaskbarLocation
            {
                Position = new Rectangle(
                    screen.WorkingArea.Left,
                    screen.WorkingArea.Bottom,
                    screen.WorkingArea.Width,
                    bottomDockedHeight
                ),
                Edge = Edge.Bottom,
            };
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

        private Screen? TryGetPlacementTargetScreen()
        {
            return TryGetPlacementTargetRect(out var rect) ? Screen.FromPoint(new Point(rect.left, rect.top)) : null;
        }

        private double GetScalingFactor(Screen screen)
        {
            // Deliberately not GetDpiForWindow: the window may still sit on the monitor it was last shown
            // on, and its scale is what we are about to correct.
            var monitor = PInvoke.MonitorFromPoint(
                new Point(screen.Bounds.Left, screen.Bounds.Top),
                MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST
            );

            if (PInvoke.GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _).Succeeded)
                return 96.0 / dpiX;

            Logger.Error("Failed to get display scaling factor. This may result in incorrect window placement.");
            return 1.0;
        }

        private int GetMargin()
        {
            var marginDip = _windowsPolicy.GetEffectiveWindowsVersion() >= WindowsVersion.Windows11 ? 12 : 0;
            return (int)Math.Round(marginDip / _dpiScalingFactor);
        }

        private struct TaskbarLocation
        {
            public Rectangle Position;
            public Edge Edge;
        }
    }
}
