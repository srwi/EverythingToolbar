using EverythingToolbar.Helpers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EverythingToolbar.Behaviors
{
    public class DpiScaling : Behavior<FrameworkElement>
    {
        [DllImport("user32")]
        static extern uint GetDpiForWindow(IntPtr hWnd);

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

        public static readonly DependencyProperty CurrentDpiProperty
            = DependencyProperty.Register(nameof(CurrentDpi), typeof(double), typeof(DpiScaling), new PropertyMetadata(96.0));

        public static readonly DependencyProperty InitialDpiProperty
            = DependencyProperty.Register(nameof(InitialDpi), typeof(double), typeof(DpiScaling), new PropertyMetadata(96.0));

        public double CurrentDpi
        {
            get => (double)GetValue(CurrentDpiProperty);
            set => SetValue(CurrentDpiProperty, value);
        }

        public double InitialDpi
        {
            get => (double)GetValue(InitialDpiProperty);
            set => SetValue(InitialDpiProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.IsLoaded)
            {
                AssociatedObjectOnLoaded(null, null);
            }
            else
            {
                AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            if (PresentationSource.FromVisual(AssociatedObject) is HwndSource hwndSource)
            {
                hwndSource.RemoveHook(HwndSourceHook);
            }
        }

        private static double GetParentWindowDpi(Visual visual)
        {
            if (Utils.GetWindowsVersion() < Utils.WindowsVersion.Windows10Anniversary)
            {
                return 96.0;
            }

            if (!(PresentationSource.FromVisual(visual) is HwndSource hwnd))
            {
                return 96.0;
            }

            // Requires Windows 10 Anniversary Update
            return GetDpiForWindow(hwnd.Handle);
        }

        private static short HiWord(IntPtr ptr)
        {
            return unchecked((short)((long)ptr >> 16));
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs e)
        {
            if (PresentationSource.FromVisual(AssociatedObject) is HwndSource hwndSource)
            {
                hwndSource.AddHook(HwndSourceHook);
            }

            InitialDpi = VisualTreeHelper.GetDpi(AssociatedObject).PixelsPerInchY;
            CurrentDpi = InitialDpi;

            UpdateDpi(GetParentWindowDpi(AssociatedObject));
        }

        [DebuggerStepThrough]
        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_DPICHANGED_AFTERPARENT = 0x02E3;
            const int WM_DPICHANGED = 0x02E0;

            switch (msg)
            {
                case WM_DPICHANGED:
                    var newDpi = HiWord(wparam);
                    if (AssociatedObject is Window window)
                    {
                        UpdateDpi(newDpi, false);
                        var suggestedRect = Marshal.PtrToStructure<RECT>(lparam);
                        window.Left = suggestedRect.Left;
                        window.Top = suggestedRect.Top;
                        window.Width = suggestedRect.Right - suggestedRect.Left;
                        window.Height = suggestedRect.Bottom - suggestedRect.Top;
                    }
                    else
                    {
                        UpdateDpi(newDpi);
                    }

                    handled = true;
                    break;
                case WM_DPICHANGED_AFTERPARENT:
                    // Used for the deskband since we don't receive WM_DPICHANGED messages there
                    UpdateDpi(GetParentWindowDpi(AssociatedObject));
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private void UpdateDpi(double newDpi, bool updateSize = true)
        {
            if (updateSize)
            {
                var dpiScale = newDpi / CurrentDpi;
                AssociatedObject.Width *= dpiScale;
                AssociatedObject.Height *= dpiScale;
            }

            CurrentDpi = newDpi;

            if (VisualTreeHelper.GetChildrenCount(AssociatedObject) == 0)
            {
                return;
            }

            var child = VisualTreeHelper.GetChild(AssociatedObject, 0);
            var renderScale = newDpi / InitialDpi;

            var scaleTransform = Math.Abs(renderScale - 1) < 0.0001
                ? Transform.Identity
                : new ScaleTransform(renderScale, renderScale);
            child.SetValue(FrameworkElement.LayoutTransformProperty, scaleTransform);
        }
    }
}