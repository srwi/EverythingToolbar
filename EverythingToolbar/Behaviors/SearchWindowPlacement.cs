using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using NLog;
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
        private readonly TaskbarStateManager _taskbarState = Ioc.Default.GetRequiredService<TaskbarStateManager>();
        private readonly ISettings _settings = Ioc.Default.GetRequiredService<ISettings>();
        private readonly WindowsPolicy _windowsPolicy = Ioc.Default.GetRequiredService<WindowsPolicy>();

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
                position.Left * _dpiScalingFactor,
                position.Top * _dpiScalingFactor,
                (position.Right - position.Left) * _dpiScalingFactor,
                (position.Bottom - position.Top) * _dpiScalingFactor,
                _taskbarState.TaskbarEdge
            );
        }

        private Rect CalculatePositionFromTarget()
        {
            if (
                PlacementTarget == null
                || PresentationSource.FromVisual(PlacementTarget) as HwndSource is not { } hwndSource
            )
            {
                Logger.Error("Failed to get HwndSource from PlacementTarget. Cannot calculate window position.");
                return new Rect();
            }

            GetWindowRect(hwndSource.Handle, out var placementTarget);

            var placementTargetPos = new Point(placementTarget.Left, placementTarget.Top);
            var screen = Screen.FromPoint(placementTargetPos);
            var workingArea = screen.WorkingArea;
            var screenBounds = screen.Bounds;
            var windowSize = GetTargetWindowSize();
            var taskbarSize = _taskbarState.TaskbarSize;
            var margin = GetMargin();

            var windowPosition = new Rect();
            switch (_taskbarState.TaskbarEdge)
            {
                case Edge.Bottom:
                case Edge.Top:
                    var topDockPos = Math.Max(workingArea.Top, screenBounds.Top + (int)taskbarSize.Height);
                    var bottomDockPos = Math.Min(workingArea.Bottom, screenBounds.Bottom - (int)taskbarSize.Height);

                    windowPosition.Right = Math.Min(
                        placementTarget.Left + (int)windowSize.Width,
                        workingArea.Right - margin
                    );
                    windowPosition.Left = Math.Max(
                        workingArea.Left + margin,
                        windowPosition.Right - (int)windowSize.Width
                    );
                    windowPosition.Right = windowPosition.Left + (int)windowSize.Width;

                    windowPosition.Bottom = Math.Min(placementTarget.Top - margin, bottomDockPos - margin);
                    windowPosition.Top = Math.Max(topDockPos + margin, windowPosition.Bottom - (int)windowSize.Height);
                    windowPosition.Bottom = windowPosition.Top + (int)windowSize.Height;
                    break;
                case Edge.Left:
                case Edge.Right:
                    var leftDockPos = Math.Max(workingArea.Left, screenBounds.Left + (int)taskbarSize.Width);
                    var rightDockPos = Math.Min(workingArea.Right, screenBounds.Right - (int)taskbarSize.Width);

                    windowPosition.Bottom = Math.Min(
                        placementTarget.Top + (int)windowSize.Height,
                        workingArea.Bottom - margin
                    );
                    windowPosition.Top = Math.Max(
                        workingArea.Top + margin,
                        windowPosition.Bottom - (int)windowSize.Height
                    );
                    windowPosition.Bottom = windowPosition.Top + (int)windowSize.Height;

                    windowPosition.Right = Math.Min(placementTarget.Left - margin, rightDockPos - margin);
                    windowPosition.Left = Math.Max(leftDockPos + margin, windowPosition.Right - (int)windowSize.Width);
                    windowPosition.Right = windowPosition.Left + (int)windowSize.Width;
                    break;
            }
            return windowPosition;
        }

        private Rect CalculatePositionFromTaskbar()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            var taskbar = FindDockedTaskBar(screen);
            var windowSize = GetTargetWindowSize();
            var margin = GetMargin();

            var windowPosition = new Rect();

            if (taskbar.Edge == Edge.Top)
            {
                windowPosition.Top = taskbar.Position.Bottom + margin;
                windowPosition.Bottom = Math.Min(
                    windowPosition.Top + (int)windowSize.Height,
                    screen.WorkingArea.Bottom - margin
                );
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Bottom)
            {
                windowPosition.Bottom = taskbar.Position.Y - margin;
                windowPosition.Top = Math.Max(
                    screen.WorkingArea.Top + margin,
                    windowPosition.Bottom - (int)windowSize.Height
                );
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Left)
            {
                windowPosition.Left = taskbar.Position.Right + margin;
                windowPosition.Right = Math.Min(
                    windowPosition.Left + (int)windowSize.Width,
                    screen.WorkingArea.Right - margin
                );
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(
                    windowPosition.Top + (int)windowSize.Height,
                    screen.WorkingArea.Bottom - margin
                );
            }
            else if (taskbar.Edge == Edge.Right)
            {
                windowPosition.Right = taskbar.Position.Left - margin;
                windowPosition.Left = Math.Max(
                    windowPosition.Right - (int)windowSize.Width,
                    screen.WorkingArea.Left + margin
                );
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(
                    windowPosition.Top + (int)windowSize.Height,
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

        private Rect SetHorizontalPosition(
            Rect windowPosition,
            Rectangle screenWorkingArea,
            Size windowSize,
            int margin
        )
        {
            if (_windowsPolicy.IsTaskbarCenterAligned())
            {
                windowPosition.Left = screenWorkingArea.Left + (int)((screenWorkingArea.Width - windowSize.Width) / 2);
                windowPosition.Left = Math.Max(screenWorkingArea.Left + margin, windowPosition.Left);
                windowPosition.Right = screenWorkingArea.Left + (int)((screenWorkingArea.Width + windowSize.Width) / 2);
                windowPosition.Right = Math.Min(screenWorkingArea.Right - margin, windowPosition.Right);
            }
            else
            {
                if (AssociatedObject.FlowDirection == FlowDirection.RightToLeft)
                {
                    windowPosition.Right = screenWorkingArea.Right - margin;
                    windowPosition.Left = Math.Max(
                        windowPosition.Right - (int)windowSize.Width,
                        screenWorkingArea.Left + margin
                    );
                }
                else
                {
                    windowPosition.Left = screenWorkingArea.Left + margin;
                    windowPosition.Right = Math.Min(
                        windowPosition.Left + (int)windowSize.Width,
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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        private struct TaskbarLocation
        {
            public Rectangle Position;
            public Edge Edge;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}