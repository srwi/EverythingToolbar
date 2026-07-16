using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using FlowDirection = System.Windows.FlowDirection;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

namespace EverythingToolbar.Behaviors
{
    public class SearchWindowPlacement : Behavior<SearchWindow>
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchWindowPlacement>();

        public FrameworkElement? PlacementTarget { get; set; }

        public bool UseCursorPlacement { get; set; }

        private double _dpiScalingFactor = 1.0;
        private readonly TaskbarStateManager _taskbarState;
        private readonly ISettings _settings;
        private readonly WindowsPolicy _windowsPolicy;

        public SearchWindowPlacement(TaskbarStateManager taskbarState, ISettings settings, WindowsPolicy windowsPolicy)
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

            if (PlacementTarget != null)
                PlacementTarget.Loaded += OnPlacementTargetLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Showing -= OnShowing;
            AssociatedObject.Hiding -= OnHiding;

            if (PlacementTarget != null)
                PlacementTarget.Loaded -= OnPlacementTargetLoaded;
        }

        private void OnPlacementTargetLoaded(object sender, RoutedEventArgs e)
        {
            _dpiScalingFactor = GetScalingFactor();
        }

        private void OnHiding(object? sender, EventArgs e)
        {
            AssociatedObject.AnimateHide(_taskbarState.TaskbarEdge);
        }

        private void OnShowing(object? sender, EventArgs e)
        {
            _dpiScalingFactor = GetScalingFactor();

            var useCursor = UseCursorPlacement || PlacementTarget == null;
            UseCursorPlacement = false;

            var position = useCursor ? CalculatePositionFromTaskbar() : CalculatePositionFromTarget();

            AssociatedObject.AnimateShow(
                position.left * _dpiScalingFactor,
                position.top * _dpiScalingFactor,
                (position.right - position.left) * _dpiScalingFactor,
                (position.bottom - position.top) * _dpiScalingFactor,
                _taskbarState.TaskbarEdge
            );
        }

        private RECT CalculatePositionFromTarget()
        {
            if (
                PlacementTarget == null
                || PresentationSource.FromVisual(PlacementTarget) as HwndSource is not { } hwndSource
            )
            {
                Logger.Error("Failed to get HwndSource from PlacementTarget. Cannot calculate window position.");
                return new RECT();
            }

            PInvoke.GetWindowRect((HWND)hwndSource.Handle, out var nativeRect);
            var placementTargetPos = new Point(nativeRect.left, nativeRect.top);
            var screen = Screen.FromPoint(placementTargetPos);
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

        private RECT CalculatePositionFromTaskbar()
        {
            var screen = Screen.FromPoint(Cursor.Position);
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

        private Size GetTargetWindowSize()
        {
            var windowSize = new Size(_settings.PopupWidth, _settings.PopupHeight);
            windowSize.Width = Math.Max(windowSize.Width, AssociatedObject.MinWidth) / _dpiScalingFactor;
            windowSize.Height = Math.Max(windowSize.Height, AssociatedObject.MinHeight) / _dpiScalingFactor;
            return windowSize;
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

        private double GetScalingFactor()
        {
            var visual = PlacementTarget ?? (System.Windows.Media.Visual)AssociatedObject;
            if (PresentationSource.FromVisual(visual) is not HwndSource hwndSource)
            {
                Logger.Error("Failed to get display scaling factor. This may result in incorrect window placement.");
                return 1.0;
            }

            return 96.0 / NativeMethods.GetDpiForWindow(hwndSource.Handle);
        }

        private int GetMargin()
        {
            var marginDip = _windowsPolicy.GetWindowsVersion() >= Utils.WindowsVersion.Windows11 ? 12 : 0;
            return (int)Math.Round(marginDip / GetScalingFactor());
        }

        private struct TaskbarLocation
        {
            public Rectangle Position;
            public Edge Edge;
        }

    }
}