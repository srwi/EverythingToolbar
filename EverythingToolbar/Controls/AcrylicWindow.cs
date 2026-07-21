using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar.Controls
{
    public class AcrylicWindow : Window
    {
        [DllImport("User32")]
        private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr DataPointer;
            public uint DataSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public AccentFlags AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private enum AccentState
        {
            Disabled,
            EnableGradient = 1,
            EnableTransparent = 2,
            EnableBlurBehind = 3,
            EnableAcrylicBlurBehind = 4,
            EnableHostBackdrop = 5,
            InvalidState = 6,
        }

        [Flags]
        private enum AccentFlags
        {
            None = 0,
            ExtendSize = 0x4,
            LeftBorder = 0x20,
            TopBorder = 0x40,
            RightBorder = 0x80,
            BottomBorder = 0x100,
            AllBorder = LeftBorder | TopBorder | RightBorder | BottomBorder,
        }

        private enum WindowCompositionAttribute
        {
            WcaAccentPolicy = 19,
        }

        private enum WindowCorner
        {
            Default = 0,
            DoNotRound = 1,
            Round = 2,
            RoundSmall = 3,
        }

        private readonly WindowsPolicy _windowsPolicy;
        private readonly ThemeService _themeService;

        public bool IsAcrylicEnabled
        {
            get => (bool)GetValue(IsAcrylicEnabledProperty);
            set => SetValue(IsAcrylicEnabledProperty, value);
        }

        public static readonly DependencyProperty IsAcrylicEnabledProperty = DependencyProperty.Register(
            nameof(IsAcrylicEnabled),
            typeof(bool),
            typeof(AcrylicWindow),
            new PropertyMetadata(true, OnAcrylicPropertyChanged)
        );

        public Color AcrylicGradientColor
        {
            get => (Color)GetValue(AcrylicGradientColorProperty);
            set => SetValue(AcrylicGradientColorProperty, value);
        }

        public static readonly DependencyProperty AcrylicGradientColorProperty = DependencyProperty.Register(
            nameof(AcrylicGradientColor),
            typeof(Color),
            typeof(AcrylicWindow),
            new PropertyMetadata(Colors.Transparent, OnAcrylicPropertyChanged)
        );

        public bool ShowAccentBorder
        {
            get => (bool)GetValue(ShowAccentBorderProperty);
            set => SetValue(ShowAccentBorderProperty, value);
        }

        public static readonly DependencyProperty ShowAccentBorderProperty = DependencyProperty.Register(
            nameof(ShowAccentBorder),
            typeof(bool),
            typeof(AcrylicWindow),
            new PropertyMetadata(false, OnAcrylicPropertyChanged)
        );

        protected AcrylicWindow(ThemeService themeService, WindowsPolicy windowsPolicy)
        {
            _themeService = themeService;
            _windowsPolicy = windowsPolicy;

            // Use layered window when Windows transparency is disabled to prevent white flash on open
            if (!SystemSettings.IsWindowsTransparencyEnabled())
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
            }

            WindowChrome.SetWindowChrome(
                this,
                new WindowChrome
                {
                    GlassFrameThickness = new Thickness(0),
                    CaptionHeight = 0,
                    ResizeBorderThickness = new Thickness(5),
                    CornerRadius = new CornerRadius(0),
                }
            );

            Background = Brushes.Transparent;

            SourceInitialized += OnSourceInitialized;
            Closed += OnClosed;

            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (!SystemSettings.IsWindowsTransparencyEnabled())
                UpdateBackgroundColor();
        }

        private void UpdateBackgroundColor()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource?.CompositionTarget != null)
            {
                hwndSource.CompositionTarget.BackgroundColor = GetThemeBackgroundColor();
            }
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            ApplyAcrylicEffect();
            ApplyWindowCorner();
        }

        private Color GetThemeBackgroundColor()
        {
            return TryFindResource("AcrylicWindowBackgroundFallback") as Color? ?? Colors.Transparent;
        }

        private static void OnAcrylicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AcrylicWindow { IsLoaded: true } window)
            {
                window.ApplyAcrylicEffect();
            }
        }

        private void ApplyAcrylicEffect()
        {
            if (!IsAcrylicEnabled)
                return;

            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource?.Handle == IntPtr.Zero)
                return;

            var handle = hwndSource!.Handle;

            var accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.EnableAcrylicBlurBehind,
                AccentFlags = ShowAccentBorder ? AccentFlags.AllBorder : AccentFlags.None,
                GradientColor = CreateColorInteger(AcrylicGradientColor),
                AnimationId = 0,
            };

            var accentPolicyPtr = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>());
            try
            {
                Marshal.StructureToPtr(accentPolicy, accentPolicyPtr, false);

                var windowCompositionAttributeData = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WcaAccentPolicy,
                    DataPointer = accentPolicyPtr,
                    DataSize = (uint)Marshal.SizeOf<AccentPolicy>(),
                };

                hwndSource.CompositionTarget!.BackgroundColor = SystemSettings.IsWindowsTransparencyEnabled()
                    ? Colors.Transparent
                    : GetThemeBackgroundColor();

                var margins = default(MARGINS);

                PInvoke.DwmExtendFrameIntoClientArea((HWND)handle, in margins);
                SetWindowCompositionAttribute(handle, ref windowCompositionAttributeData);

                PInvoke.SetWindowPos(
                    (HWND)handle,
                    (HWND)IntPtr.Zero,
                    0,
                    0,
                    0,
                    0,
                    SET_WINDOW_POS_FLAGS.SWP_DRAWFRAME
                        | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                        | SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                        | SET_WINDOW_POS_FLAGS.SWP_NOOWNERZORDER
                        | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                        | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                );
            }
            finally
            {
                Marshal.FreeHGlobal(accentPolicyPtr);
            }
        }

        private void ApplyWindowCorner()
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource?.Handle == IntPtr.Zero)
                return;

            var handle = hwndSource!.Handle;

            // Determine corner style based on Windows version
            var windowsVersion = _windowsPolicy.GetWindowsVersion();
            var cornerStyle = windowsVersion >= WindowsVersion.Windows11 ? WindowCorner.Round : WindowCorner.RoundSmall;

            var corner = (int)cornerStyle;

            unsafe
            {
                PInvoke.DwmSetWindowAttribute(
                    (HWND)handle,
                    DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                    &corner,
                    sizeof(int)
                );
            }
        }

        private static int CreateColorInteger(Color color)
        {
            return color.R << 0 | color.G << 8 | color.B << 16 | color.A << 24;
        }
    }
}
