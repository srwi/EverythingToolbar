using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace EverythingToolbar.Behaviors
{
    internal class SearchWindowPlacement : Behavior<SearchWindow>
    {
        // Using a dependency property for binding is not required since the placement target will not change
        public UIElement PlacementTarget;

        protected override void OnAttached()
        {
            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RECT position = CalculatePosition();
            AssociatedObject.Left = position.Left;
            AssociatedObject.Top = position.Top;
            AssociatedObject.Width = position.Right - position.Left;
            AssociatedObject.Height = position.Bottom - position.Top;
        }

        private void OnHiding(object sender, EventArgs e)
        {
            RECT position = CalculatePosition();
            AssociatedObject.AnimateHide(
                position.Left,
                position.Top,
                position.Right - position.Left,
                position.Bottom - position.Top,
                TaskbarStateManager.Instance.TaskbarEdge
            );
        }

        private void OnShowing(object sender, EventArgs e)
        {
            RECT position = CalculatePosition();
            AssociatedObject.AnimateShow(
                position.Left,
                position.Top,
                position.Right - position.Left,
                position.Bottom - position.Top,
                TaskbarStateManager.Instance.TaskbarEdge
            );
        }

        private RECT CalculatePosition()
        {
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(PlacementTarget)).Handle;
            GetWindowRect(hwnd, out RECT placementTarget);

            int margin = GetMargin();
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            System.Windows.Size windowSize = Properties.Settings.Default.popupSize;
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

        private int GetMargin()
        {
            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                return 12;
            else
                return 0;
        }

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
