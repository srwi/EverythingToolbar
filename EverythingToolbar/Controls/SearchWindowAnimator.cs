using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EverythingToolbar.Controls
{
    internal sealed class SearchWindowAnimator
    {
        private readonly Window _window;
        private readonly FrameworkElement _contentGrid;
        private readonly Action _setTopmostBelowTaskbar;
        private readonly Action _onHideCompleted;
        private readonly Func<bool> _animationsDisabled;
        private readonly bool _isWindows11OrGreater;

        private bool _isRenderingHooked;

        public SearchWindowAnimator(
            Window window,
            FrameworkElement contentGrid,
            Action setTopmostBelowTaskbar,
            Action onHideCompleted,
            Func<bool> animationsDisabled,
            bool isWindows11OrGreater)
        {
            _window = window;
            _contentGrid = contentGrid;
            _setTopmostBelowTaskbar = setTopmostBelowTaskbar;
            _onHideCompleted = onHideCompleted;
            _animationsDisabled = animationsDisabled;
            _isWindows11OrGreater = isWindows11OrGreater;
        }

        public void AnimateShow(double left, double top, double width, double height, Edge taskbarEdge)
        {
            // A running hide animation's Completed handler may never fire if we interrupt it; unhook here.
            UnhookRendering();
            ClearAnimations();

            _window.Width = width;
            _window.Height = height;

            var (_, _, horizontal) = EdgeGeometry(taskbarEdge);
            if (horizontal)
                _window.Left = left;
            else
                _window.Top = top;

            _setTopmostBelowTaskbar();

            if (_isWindows11OrGreater)
                AnimateShowWin11(left, top, width, height, taskbarEdge);
            else
                AnimateShowWin10(left, top, taskbarEdge);
        }

        public void AnimateHide(Edge taskbarEdge)
        {
            HookRendering();

            if (_isWindows11OrGreater)
                AnimateHideWin11(taskbarEdge);
            else
                AnimateHideWin10(taskbarEdge);
        }

        public void ClearAnimations()
        {
            _window.BeginAnimation(Window.LeftProperty, null);
            _window.BeginAnimation(Window.TopProperty, null);
            _window.BeginAnimation(UIElement.OpacityProperty, null);
            _contentGrid.BeginAnimation(FrameworkElement.MarginProperty, null);
        }

        // Forces a render every frame, so only hook this while the hide animation needs DwmFlush sync.
        public void HookRendering()
        {
            if (_isRenderingHooked)
                return;

            _isRenderingHooked = true;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        public void UnhookRendering()
        {
            if (!_isRenderingHooked)
                return;

            _isRenderingHooked = false;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }

        private void AnimateShowWin10(double left, double top, Edge taskbarEdge)
        {
            if (_animationsDisabled())
            {
                _window.Opacity = 1;
                _window.Left = left;
                _window.Top = top;
                return;
            }

            var (axis, sign, horizontal) = EdgeGeometry(taskbarEdge);
            var basePos = horizontal ? top : left;

            _window.BeginAnimation(
                axis,
                new DoubleAnimation
                {
                    From = basePos + sign * 150,
                    To = basePos,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );

            _window.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.4),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );

            _contentGrid.BeginAnimation(
                FrameworkElement.MarginProperty,
                new ThicknessAnimation
                {
                    From = SlideMargin(horizontal, sign),
                    To = new Thickness(0),
                    Duration = TimeSpan.FromSeconds(0.8),
                    EasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut },
                }
            );
        }

        private void AnimateShowWin11(double left, double top, double width, double height, Edge taskbarEdge)
        {
            if (_animationsDisabled())
            {
                _window.Opacity = 1;
                _window.Left = left;
                _window.Top = top;
                return;
            }

            var (axis, sign, horizontal) = EdgeGeometry(taskbarEdge);
            var basePos = horizontal ? top : left;
            var magnitude = horizontal ? height : width;

            _window.BeginAnimation(
                axis,
                new DoubleAnimation
                {
                    From = basePos + sign * magnitude,
                    To = basePos,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );

            _contentGrid.BeginAnimation(
                FrameworkElement.MarginProperty,
                new ThicknessAnimation
                {
                    From = SlideMargin(horizontal, sign),
                    To = new Thickness(0),
                    Duration = TimeSpan.FromSeconds(0.3),
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 5 },
                }
            );
        }

        private void AnimateHideWin10(Edge taskbarEdge)
        {
            if (_animationsDisabled())
            {
                _window.Dispatcher.BeginInvoke(_onHideCompleted);
                return;
            }

            _window.BeginAnimation(
                UIElement.OpacityProperty,
                new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(30),
                }
            );

            var (axis, sign, horizontal) = EdgeGeometry(taskbarEdge);
            var basePos = horizontal ? _window.RestoreBounds.Top : _window.RestoreBounds.Left;

            var animation = new DoubleAnimation { To = basePos + sign * 150, Duration = TimeSpan.FromMilliseconds(30) };
            animation.Completed += (_, _) => _onHideCompleted();
            _window.BeginAnimation(axis, animation);
        }

        private void AnimateHideWin11(Edge taskbarEdge)
        {
            if (_animationsDisabled())
            {
                _window.Dispatcher.BeginInvoke(_onHideCompleted);
                return;
            }

            var (axis, sign, horizontal) = EdgeGeometry(taskbarEdge);
            var basePos = horizontal ? _window.RestoreBounds.Top : _window.RestoreBounds.Left;
            const double extraOffset = 50; // To include all possible window decorations
            var magnitude = (horizontal ? _window.Height : _window.Width) + extraOffset;

            var animation = new DoubleAnimation
            {
                From = basePos,
                To = basePos + sign * magnitude,
                Duration = TimeSpan.FromSeconds(0.25),
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseIn, Power = 6 },
            };
            animation.Completed += (_, _) => _onHideCompleted();
            _window.BeginAnimation(axis, animation);
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            NativeMethods.DwmFlush();
        }

        private static (DependencyProperty axis, double sign, bool horizontal) EdgeGeometry(Edge taskbarEdge) =>
            taskbarEdge switch
            {
                Edge.Left => (Window.LeftProperty, -1.0, false),
                Edge.Right => (Window.LeftProperty, 1.0, false),
                Edge.Top => (Window.TopProperty, -1.0, true),
                Edge.Bottom => (Window.TopProperty, 1.0, true),
                _ => throw new ArgumentOutOfRangeException(nameof(taskbarEdge)),
            };

        private static Thickness SlideMargin(bool horizontal, double sign)
        {
            var offset = sign * 50;
            return horizontal ? new Thickness(0, offset, 0, -offset) : new Thickness(offset, 0, -offset, 0);
        }
    }
}
