using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using Size = System.Windows.Size;

namespace EverythingToolbar.Launcher
{
    internal class SearchWindowPlacement : Behavior<SearchWindow>
    {
        protected override void OnAttached()
        {
            // Start with window outside of screen area to prevent flickering when loading for the first time
            AssociatedObject.Left = 100000;
            AssociatedObject.Top = 100000;

            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;
        }

        private void OnHiding(object sender, EventArgs e)
        {
            AssociatedObject.AnimateHide(TaskbarStateManager.Instance.TaskbarEdge);
        }

        private void OnShowing(object sender, EventArgs e)
        {
            var scalingFactor = GetScalingFactor();
            var position = CalculatePosition(scalingFactor);
            AssociatedObject.AnimateShow(
                position.Left * scalingFactor,
                position.Top * scalingFactor,
                (position.Right - position.Left) * scalingFactor,
                (position.Bottom - position.Top) * scalingFactor,
                TaskbarStateManager.Instance.TaskbarEdge
            );
        }

        private RECT CalculatePosition(double scalingFactor)
        {
            var screen = Screen.FromPoint(Cursor.Position);
            var taskbar = FindDockedTaskBar(screen);
            var windowSize = GetTargetWindowSize(scalingFactor);
            var margin = (int)(GetMargin() / scalingFactor);

            var windowPosition = new RECT();

            if (taskbar.Edge == Edge.Top)
            {
                windowPosition.Top = taskbar.Position.Bottom + margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Bottom - margin);
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Bottom)
            {
                windowPosition.Bottom = taskbar.Position.Y - margin;
                windowPosition.Top = Math.Max(screen.WorkingArea.Top + margin, windowPosition.Bottom - (int)windowSize.Height);
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Left)
            {
                windowPosition.Left = taskbar.Position.Right + margin;
                windowPosition.Right = Math.Min(windowPosition.Left + (int)windowSize.Width, screen.WorkingArea.Right - margin);
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Bottom - margin);
            }
            else if (taskbar.Edge == Edge.Right)
            {
                windowPosition.Right = taskbar.Position.Left - margin;
                windowPosition.Left = Math.Max(windowPosition.Right - (int)windowSize.Width, screen.WorkingArea.Left + margin);
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Bottom - margin);
            }

            TaskbarStateManager.Instance.TaskbarEdge = taskbar.Edge;

            return windowPosition;
        }

        private Size GetTargetWindowSize(double scalingFactor)
        {
            var windowSize = new Size(ToolbarSettings.User.PopupWidth, ToolbarSettings.User.PopupHeight);
            windowSize.Width = Math.Max(windowSize.Width, AssociatedObject.MinWidth) / scalingFactor;
            windowSize.Height = Math.Max(windowSize.Height, AssociatedObject.MinHeight) / scalingFactor;
            return windowSize;
        }

        private RECT SetHorizontalPosition(RECT windowPosition, Rectangle screenWorkingArea, Size windowSize, int margin)
        {
            if (Utils.IsTaskbarCenterAligned())
            {
                windowPosition.Left = screenWorkingArea.Left + (int)((screenWorkingArea.Width - windowSize.Width) / 2);
                windowPosition.Left = Math.Max(screenWorkingArea.Left + margin, windowPosition.Left);
                windowPosition.Right = screenWorkingArea.Left + (int)((screenWorkingArea.Width + windowSize.Width) / 2);
                windowPosition.Right = Math.Min(screenWorkingArea.Right - margin, windowPosition.Right);
            }
            else
            {
                if (CultureInfo.CurrentCulture.TextInfo.IsRightToLeft)
                {
                    windowPosition.Right = screenWorkingArea.Right - margin;
                    windowPosition.Left = Math.Max(windowPosition.Right - (int)windowSize.Width, screenWorkingArea.Left + margin);
                }
                else
                {
                    windowPosition.Left = screenWorkingArea.Left + margin;
                    windowPosition.Right = Math.Min(windowPosition.Left + (int)windowSize.Width, screenWorkingArea.Right - margin);
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
                    Position = new Rectangle(screen.Bounds.Left, screen.Bounds.Top, leftDockedWidth, screen.Bounds.Height),
                    Edge = Edge.Left
                };
            }
            if (rightDockedWidth > 0 && bottomDockedHeight == 0)
            {
                return new TaskbarLocation
                {
                    Position = new Rectangle(screen.WorkingArea.Right, screen.Bounds.Top, rightDockedWidth, screen.Bounds.Height),
                    Edge = Edge.Right
                };

            }
            if (topDockedHeight > 0 && bottomDockedHeight == 0)
            {
                return new TaskbarLocation
                {
                    Position = new Rectangle(screen.WorkingArea.Left, screen.Bounds.Top, screen.WorkingArea.Width, topDockedHeight),
                    Edge = Edge.Top
                };
            }

            return new TaskbarLocation
            {
                Position = new Rectangle(screen.WorkingArea.Left, screen.WorkingArea.Bottom, screen.WorkingArea.Width, bottomDockedHeight),
                Edge = Edge.Bottom
            };
        }

        private double GetScalingFactor()
        {
            // Converting from wpf-size requires division by scaling factor;
            // Converting to wpf-size requires multiplication with scaling factor
            var hwnd = ((HwndSource)PresentationSource.FromVisual(AssociatedObject)).Handle;
            return 96.0 / GetDpiForWindow(hwnd);
        }

        private int GetMargin()
        {
            return Helpers.Utils.GetWindowsVersion() >= Helpers.Utils.WindowsVersion.Windows11 ? 12 : 0;
        }

        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        private struct TaskbarLocation
        {
            public Rectangle Position;
            public Edge Edge;
        }

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
