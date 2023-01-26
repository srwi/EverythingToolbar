using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;
using Microsoft.Xaml.Behaviors;
using Size = System.Windows.Size;

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
            RECT position = CalculatePosition();
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
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(PlacementTarget)).Handle;
            GetWindowRect(hwnd, out RECT placementTarget);

            int margin = GetMargin();
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            Size windowSize = GetTargetWindowSize();
            Edge taskbarEdge = TaskbarStateManager.Instance.TaskbarEdge;

            RECT windowPosition = new RECT();
            switch (taskbarEdge)
            {
                case Edge.Bottom:
                case Edge.Top:
                    windowPosition.Right = Math.Min(placementTarget.Left + (int)windowSize.Width, workingArea.Right - margin);
                    windowPosition.Left = Math.Max(margin, windowPosition.Right - (int)windowSize.Width);
                    windowPosition.Top = Math.Max(workingArea.Top + margin, placementTarget.Top - margin - (int)windowSize.Height);
                    windowPosition.Bottom = Math.Min(workingArea.Bottom - margin, placementTarget.Bottom + margin + (int)windowSize.Height);
                    break;
                case Edge.Left:
                case Edge.Right:
                    windowPosition.Bottom = Math.Min(placementTarget.Top + (int)windowSize.Height, workingArea.Bottom - margin);
                    windowPosition.Top = Math.Max(margin, windowPosition.Bottom - (int)windowSize.Height);
                    windowPosition.Left = Math.Max(workingArea.Left + margin, placementTarget.Left - margin - (int)windowSize.Width);
                    windowPosition.Right = Math.Min(workingArea.Right - margin, placementTarget.Right + margin + (int)windowSize.Width);
                    break;
            }
            return windowPosition;
        }

        private Size GetTargetWindowSize()
        {
            Size windowSize = Settings.Default.popupSize;
            windowSize.Width = Math.Max(windowSize.Width, AssociatedObject.MinWidth) / DpiScalingFactor;
            windowSize.Height = Math.Max(windowSize.Height, AssociatedObject.MinHeight) / DpiScalingFactor;
            return windowSize;
        }

        private double GetScalingFactor()
        {
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(PlacementTarget)).Handle;
            return 96.0 / GetDpiForWindow(hwnd);
        }

        private int GetMargin()
        {
            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                return (int)(12 / GetScalingFactor());
            
            return 0;
        }

        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }
        }
    }
}
