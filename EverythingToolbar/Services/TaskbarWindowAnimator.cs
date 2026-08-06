using System;
using System.Windows;
using System.Windows.Media.Animation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EverythingToolbar.Services
{
    internal readonly record struct WidgetBounds(int Left, int Top, int Width, int Height)
    {
        public static WidgetBounds Lerp(WidgetBounds from, WidgetBounds to, double progress) =>
            new(
                Interpolate(from.Left, to.Left, progress),
                Interpolate(from.Top, to.Top, progress),
                Interpolate(from.Width, to.Width, progress),
                Interpolate(from.Height, to.Height, progress)
            );

        private static int Interpolate(int from, int to, double progress) =>
            (int)Math.Round(from + (to - from) * progress);
    }

    /// <summary>
    /// Moves and shows/hides a window that is placed with SetWindowPos rather than through WPF layout.
    /// Size and position changes glide instead of jumping.
    /// </summary>
    internal sealed class TaskbarWindowAnimator
    {
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(250);

        private readonly HWND _hwnd;
        private readonly Func<bool> _animationsDisabled;
        private readonly Progress _progress = new();

        // Null while the window is off screen: before its first placement, and after Hide.
        private WidgetBounds? _current;
        private WidgetBounds? _target;
        private WidgetBounds _from;

        public TaskbarWindowAnimator(IntPtr handle, Func<bool> animationsDisabled)
        {
            _hwnd = (HWND)handle;
            _animationsDisabled = animationsDisabled;
            _progress.ValueChanged += OnProgressChanged;
        }

        public void MoveTo(WidgetBounds target)
        {
            if (_target == target)
                return;

            _target = target;

            // A window that is not on screen yet has nothing to glide from, so it appears in place.
            if (_current is not { } from || _animationsDisabled())
            {
                // Pinning the start at the target keeps the progress reset below from moving the window.
                _from = target;
                StopAnimation();
                ApplyBounds(target, ensureVisible: true);
                return;
            }

            // Starting wherever the window is right now lets a target picked up mid-flight continue smoothly.
            _from = from;

            _progress.Animate(
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = AnimationDuration,
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );
        }

        /// <summary>Hides the window. Returns true if it was visible.</summary>
        public bool Hide()
        {
            if (_current is null)
                return false;

            _current = null;
            _target = null;
            StopAnimation();

            PInvoke.SetWindowPos(
                _hwnd,
                HWND.Null,
                0,
                0,
                0,
                0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE
                    | SET_WINDOW_POS_FLAGS.SWP_NOSIZE
                    | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                    | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE
                    | SET_WINDOW_POS_FLAGS.SWP_HIDEWINDOW
            );
            return true;
        }

        // A finished animation holds its end value without ticking, so it is only cancelled where that
        // held value would get in the way.
        public void StopAnimation() => _progress.Animate(null);

        private void OnProgressChanged(double progress)
        {
            if (_target is { } target)
                ApplyBounds(WidgetBounds.Lerp(_from, target, progress));
        }

        private void ApplyBounds(WidgetBounds bounds, bool ensureVisible = false)
        {
            _current = bounds;

            var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;
            if (ensureVisible)
                flags |= SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW;

            PInvoke.SetWindowPos(_hwnd, HWND.Null, bounds.Left, bounds.Top, bounds.Width, bounds.Height, flags);
        }

        /// <summary>
        /// A single animatable double. WPF animates dependency properties, and window bounds set through
        /// SetWindowPos are not one, so the transition drives this and the bounds follow from it.
        /// </summary>
        private sealed class Progress : Animatable
        {
            private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(Progress),
                new PropertyMetadata(0.0, (d, e) => ((Progress)d).ValueChanged?.Invoke((double)e.NewValue))
            );

            public event Action<double>? ValueChanged;

            public void Animate(AnimationTimeline? animation) => BeginAnimation(ValueProperty, animation);

            protected override Freezable CreateInstanceCore() => new Progress();
        }
    }
}
