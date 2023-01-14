using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace EverythingToolbar.Behaviors
{
    internal class SearchWindowPlacement : Behavior<SearchWindow>
    {
        protected override void OnAttached()
        {
            AssociatedObject.Showing += OnShowing;
            AssociatedObject.Hiding += OnHiding;
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            double scalingFactor = GetScalingFactor();
            RECT position = CalculatePosition(scalingFactor);
            AssociatedObject.Left = position.Left * scalingFactor;
            AssociatedObject.Top = position.Top * scalingFactor;
            AssociatedObject.Width = (position.Right - position.Left) * scalingFactor;
            AssociatedObject.Height = (position.Bottom - position.Top) * scalingFactor;
        }

        private void OnHiding(object sender, EventArgs e)
        {
            AssociatedObject.AnimateHide(TaskbarStateManager.Instance.TaskbarEdge);
        }

        private void OnShowing(object sender, EventArgs e)
        {
            double scalingFactor = GetScalingFactor();
            RECT position = CalculatePosition(scalingFactor);
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
            Screen screen = Screen.PrimaryScreen;
            TaskbarLocation taskbar = FindDockedTaskBar(screen);
            System.Windows.Size windowSize = Properties.Settings.Default.popupSize;
            windowSize.Width /= scalingFactor;
            windowSize.Height /= scalingFactor;
            int margin = (int)(GetMargin() / scalingFactor);

            RECT windowPosition = new RECT();

            if (taskbar.Edge == Edge.Top)
            {
                windowPosition.Top = taskbar.Position.Height + margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Bottom - margin);
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Bottom)
            {
                windowPosition.Bottom = taskbar.Position.Y - margin;
                windowPosition.Top = Math.Max(margin, windowPosition.Bottom - (int)windowSize.Height);
                windowPosition = SetHorizontalPosition(windowPosition, screen.WorkingArea, windowSize, margin);
            }
            else if (taskbar.Edge == Edge.Left)
            {
                windowPosition.Left = taskbar.Position.Width + margin;
                windowPosition.Right = Math.Min(windowPosition.Left + (int)windowSize.Width, screen.WorkingArea.Width - margin);
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Height - margin);
            }
            else if (taskbar.Edge == Edge.Right)
            {
                windowPosition.Right = taskbar.Position.X - margin;
                windowPosition.Left = Math.Max(windowPosition.Right - (int)windowSize.Width, margin);
                windowPosition.Top = margin;
                windowPosition.Bottom = Math.Min(windowPosition.Top + (int)windowSize.Height, screen.WorkingArea.Height - margin);
            }

            TaskbarStateManager.Instance.TaskbarEdge = taskbar.Edge;

            return windowPosition;
        }

        private RECT SetHorizontalPosition(RECT windowPosition, Rectangle screenWorkingArea, System.Windows.Size windowSize, int margin)
        {
            if (Launcher.Utils.IsTaskbarCenterAligned())
            {
                windowPosition.Left = (int)((screenWorkingArea.Width - windowSize.Width) / 2);
                windowPosition.Left = Math.Max(margin, windowPosition.Left);
                windowPosition.Right = (int)((screenWorkingArea.Width + windowSize.Width) / 2);
                windowPosition.Right = Math.Min(screenWorkingArea.Width - margin, windowPosition.Right);
            }
            else
            {
                if (CultureInfo.CurrentCulture.TextInfo.IsRightToLeft)
                {
                    windowPosition.Right = screenWorkingArea.Width - margin;
                    windowPosition.Left = Math.Max(windowPosition.Right - (int)windowSize.Width, margin);
                }
                else
                {
                    windowPosition.Left = margin;
                    windowPosition.Right = Math.Min(windowPosition.Left + (int)windowSize.Width, screenWorkingArea.Width - margin);
                }
            }

            return windowPosition;
        }

        private TaskbarLocation FindDockedTaskBar(Screen screen)
        {
            TaskbarLocation location = new TaskbarLocation
            {
                Position = new Rectangle(0, 0, 0, 0),
                Edge = Edge.Bottom,
            };

            var leftDockedWidth = Math.Abs((Math.Abs(screen.Bounds.Left) - Math.Abs(screen.WorkingArea.Left)));
            var topDockedHeight = Math.Abs((Math.Abs(screen.Bounds.Top) - Math.Abs(screen.WorkingArea.Top)));
            var rightDockedWidth = ((screen.Bounds.Width - leftDockedWidth) - screen.WorkingArea.Width);
            var bottomDockedHeight = ((screen.Bounds.Height - topDockedHeight) - screen.WorkingArea.Height);

            if (leftDockedWidth > 0)
            {
                location.Position.X = screen.Bounds.Left;
                location.Position.Y = screen.Bounds.Top;
                location.Position.Width = leftDockedWidth;
                location.Position.Height = screen.Bounds.Height;
                location.Edge = Edge.Left;
            }
            else if (rightDockedWidth > 0)
            {
                location.Position.X = screen.WorkingArea.Right;
                location.Position.Y = screen.Bounds.Top;
                location.Position.Width = rightDockedWidth;
                location.Position.Height = screen.Bounds.Height;
                location.Edge = Edge.Right;
            }
            else if (topDockedHeight > 0)
            {
                location.Position.X = screen.WorkingArea.Left;
                location.Position.Y = screen.Bounds.Top;
                location.Position.Width = screen.WorkingArea.Width;
                location.Position.Height = topDockedHeight;
                location.Edge = Edge.Top;
            }
            else
            {
                location.Position.X = screen.WorkingArea.Left;
                location.Position.Y = screen.WorkingArea.Bottom;
                location.Position.Width = screen.WorkingArea.Width;
                location.Position.Height = bottomDockedHeight;
                location.Edge = Edge.Bottom;
            }

            return location;
        }

        private double GetScalingFactor()
        {
            // Converting from wpf-size requires division by scaling factor;
            // Converting to wpf-size requires multiplication with scaling factor
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(AssociatedObject)).Handle;
            return 96.0 / GetDpiForWindow(hwnd);
        }

        private int GetMargin()
        {
            if (Environment.OSVersion.Version >= Utils.WindowsVersion.Windows11)
                return 12;
            else
                return 0;
        }

        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

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
