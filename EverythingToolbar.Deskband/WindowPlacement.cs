using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using Point = System.Drawing.Point;

namespace EverythingToolbar.Behaviors
{
    internal class SearchWindowPlacement : Behavior<SearchWindow>
    {
        // Using a dependency property for binding is not required since the placement target will not change
        public FrameworkElement PlacementTarget;

        private double DpiScalingFactor;

        protected override void OnAttached()
        {
            // Start with window outside of screen area to prevent flickering when loading for the first time
            AssociatedObject.Left = 100000;
            AssociatedObject.Top = 100000;

            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;

            PlacementTarget.Loaded += OnPlacementTargetLoaded;
        }

        private void OnPlacementTargetLoaded(object sender, RoutedEventArgs e)
        {
            DpiScalingFactor = GetScalingFactor();
        }

        private void OnHiding(object sender, EventArgs e)
        {
            AssociatedObject.AnimateHide(TaskbarStateManager.Instance.TaskbarEdge);
        }

        private void OnShowing(object sender, EventArgs e)
        {
            var position = CalculatePosition();
            AssociatedObject.AnimateShow(
                position.Left * DpiScalingFactor,
                position.Top * DpiScalingFactor,
                (position.Right - position.Left) * DpiScalingFactor,
                (position.Bottom - position.Top) * DpiScalingFactor,
                TaskbarStateManager.Instance.TaskbarEdge
            );
        }

        private RECT CalculatePosition()
        {
            var hwnd = ((HwndSource)PresentationSource.FromVisual(PlacementTarget)).Handle;
            GetWindowRect(hwnd, out var placementTarget);

            var placementTargetPos = new Point(placementTarget.Left, placementTarget.Top);
            var screen = Screen.FromPoint(placementTargetPos);
            var workingArea = screen.WorkingArea;
            var screenBounds = screen.Bounds;
            var windowSize = GetTargetWindowSize();
            var taskbarSize = TaskbarStateManager.Instance.TaskbarSize;
            var margin = GetMargin();

            var windowPosition = new RECT();
            switch (TaskbarStateManager.Instance.TaskbarEdge)
            {
                case Edge.Bottom:
                case Edge.Top:
                    // In case of auto-hiding taskbar the working area is not affected by the taskbar.
                    // Therefore the taskbar size needs to be handled separately.
                    var topDockPos = Math.Max(workingArea.Top, screenBounds.Top + (int)taskbarSize.Height);
                    var bottomDockPos = Math.Min(workingArea.Bottom, screenBounds.Bottom - (int)taskbarSize.Height);

                    windowPosition.Right = Math.Min(placementTarget.Left + (int)windowSize.Width, workingArea.Right - margin);
                    windowPosition.Left = Math.Max(workingArea.Left + margin, windowPosition.Right - (int)windowSize.Width);
                    windowPosition.Top = Math.Max(topDockPos + margin, placementTarget.Top - margin - (int)windowSize.Height);
                    windowPosition.Bottom = Math.Min(bottomDockPos - margin, placementTarget.Bottom + margin + (int)windowSize.Height);
                    break;
                case Edge.Left:
                case Edge.Right:
                    var leftDockPos = Math.Max(workingArea.Left, screenBounds.Left + (int)taskbarSize.Width);
                    var rightDockPos = Math.Min(workingArea.Right, screenBounds.Right - (int)taskbarSize.Width);

                    windowPosition.Bottom = Math.Min(placementTarget.Top + (int)windowSize.Height, workingArea.Bottom - margin);
                    windowPosition.Top = Math.Max(workingArea.Top + margin, windowPosition.Bottom - (int)windowSize.Height);
                    windowPosition.Left = Math.Max(leftDockPos + margin, placementTarget.Left - margin - (int)windowSize.Width);
                    windowPosition.Right = Math.Min(rightDockPos - margin, placementTarget.Right + margin + (int)windowSize.Width);
                    break;
            }
            return windowPosition;
        }

        private Size GetTargetWindowSize()
        {
            var windowSize = new Size(ToolbarSettings.User.PopupWidth, ToolbarSettings.User.PopupHeight);
            windowSize.Width = Math.Max(windowSize.Width, AssociatedObject.MinWidth) / DpiScalingFactor;
            windowSize.Height = Math.Max(windowSize.Height, AssociatedObject.MinHeight) / DpiScalingFactor;
            return windowSize;
        }

        private double GetScalingFactor()
        {
            var hwnd = ((HwndSource)PresentationSource.FromVisual(PlacementTarget)).Handle;
            return 96.0 / GetDpiForWindow(hwnd);
        }

        private int GetMargin()
        {
            if (Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows11)
                return (int)(12 / GetScalingFactor());
            
            return 0;
        }

        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
