using CSDeskBand;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EverythingToolbar
{
    public partial class SearchResultsPopup : Popup
    {
        public static Edge taskbarEdge;
        Size dragStartSize = new Size();
        Point dragStartPosition = new Point();

        public SearchResultsPopup()
        {
            InitializeComponent();
            DataContext = EverythingSearch.Instance;

			EverythingSearch.Instance.PropertyChanged += OnCurrentFilterChanged;
        }

		private void OnCurrentFilterChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
            if (e.PropertyName == "CurrentFilter")
            {
                Keyboard.Focus(SearchBox);
			}
		}

		private void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            dragStartSize.Height = Height;
            dragStartSize.Width = Width;
            dragStartPosition = PointToScreen(Mouse.GetPosition(this));
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            Point mousePos = PointToScreen(Mouse.GetPosition(this));
            int widthModifier = (sender as Thumb).HorizontalAlignment == HorizontalAlignment.Left ? -1 : 1;
            int heightModifier = (sender as Thumb).VerticalAlignment == VerticalAlignment.Top ? -1 : 1;
            double widthAdjust = dragStartSize.Width + widthModifier * (mousePos.X - dragStartPosition.X);
            double heightAdjust = dragStartSize.Height + heightModifier * (mousePos.Y - dragStartPosition.Y);

            if (widthAdjust >= 300)
            {
                Width = widthAdjust;
            }
            if (heightAdjust >= 300)
			{
                Height = heightAdjust;
			}
        }

        private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Properties.Settings.Default.popupSize = new Size(Width, Height);
            Properties.Settings.Default.Save();
        }

		private void OnOpened(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.isIconOnly)
                Keyboard.Focus(SearchBox);

			switch (taskbarEdge)
            {
                case Edge.Top:
                    Placement = PlacementMode.Bottom;
                    PopupBorder.BorderThickness = new Thickness(1, 0, 1, 1);
                    PopupMarginBorder.Margin = new Thickness(10, 0, 10, 10);
                    break;
                case Edge.Left:
                    Placement = PlacementMode.Right;
                    PopupBorder.BorderThickness = new Thickness(0, 1, 1, 1);
                    PopupMarginBorder.Margin = new Thickness(0, 10, 10, 10);
                    break;
                case Edge.Right:
                    Placement = PlacementMode.Left;
                    PopupBorder.BorderThickness = new Thickness(1, 1, 0, 1);
                    PopupMarginBorder.Margin = new Thickness(10, 10, 0, 10);
                    break;
                case Edge.Bottom:
                    Placement = PlacementMode.Top;
                    PopupBorder.BorderThickness = new Thickness(1, 1, 1, 0);
                    PopupMarginBorder.Margin = new Thickness(10, 10, 10, 0);
                    break;
            }

            Height = Properties.Settings.Default.popupSize.Height;
            Width = Properties.Settings.Default.popupSize.Width;

			QuinticEase ease = new QuinticEase
            {
                EasingMode = EasingMode.EaseOut
            };

			int modifier = taskbarEdge == Edge.Right || taskbarEdge == Edge.Bottom ? 1 : -1;
			DoubleAnimation outer = new DoubleAnimation(modifier * 150, 0, TimeSpan.FromSeconds(0.4))
			{
				EasingFunction = ease
			};
			DependencyProperty outerProp = taskbarEdge == Edge.Bottom || taskbarEdge == Edge.Top ? TranslateTransform.YProperty : TranslateTransform.XProperty;
            translateTransform?.BeginAnimation(outerProp, outer);

			DoubleAnimation opacity = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
			{
				EasingFunction = ease
			};
			PopupBorder?.BeginAnimation(OpacityProperty, opacity);

			ThicknessAnimation inner = new ThicknessAnimation(new Thickness(0), TimeSpan.FromSeconds(0.8))
			{
				EasingFunction = ease
			};
			if (taskbarEdge == Edge.Top)
                inner.From = new Thickness(0, -50, 0, 50);
            else if (taskbarEdge == Edge.Right)
                inner.From = new Thickness(50, 0, -50, 0);
            else if (taskbarEdge == Edge.Bottom)
                inner.From = new Thickness(0, 50, 0, -50);
            else if (taskbarEdge == Edge.Left)
                inner.From = new Thickness(-50, 0, 50, 0);
            ContentGrid?.BeginAnimation(MarginProperty, inner);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            EverythingSearch.Instance.Reset();
        }
	}
}
