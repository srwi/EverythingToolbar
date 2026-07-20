using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace EverythingToolbar.Settings
{
    public class FilterOrderItem
    {
        public string Name { get; set; } = "";
        public int OriginalIndex { get; init; }
    }

    [ObservableObject]
    public partial class Filters
    {
        [ObservableProperty]
        private ObservableCollection<FilterOrderItem> _filterOrderItems = new();

        private bool _isDragging;
        private Point _startPoint;
        public ISettings Settings { get; } = Ioc.Default.GetRequiredService<ISettings>();
        private readonly DefaultFilterProvider _defaultFilterProvider =
            Ioc.Default.GetRequiredService<DefaultFilterProvider>();

        public Filters()
        {
            InitializeComponent();
            DataContext = this;

            LoadFilterOrder();
        }

        private void LoadFilterOrder()
        {
            var defaultFilters = _defaultFilterProvider.DefaultFilters;

            // Use the validation logic from DefaultFilterProvider
            var validOrder = _defaultFilterProvider.GetValidFilterOrder();

            FilterOrderItems = new ObservableCollection<FilterOrderItem>(
                validOrder.Select(i => new FilterOrderItem { Name = defaultFilters[i].Name, OriginalIndex = i })
            );
        }

        private void SaveOrder()
        {
            var orderString = string.Join(",", FilterOrderItems.Select(item => item.OriginalIndex));
            Settings.FilterOrder = orderString;
        }

        private void OnOrderListItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void OnOrderListItemMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance
                )
                {
                    _isDragging = true;
                    ListBoxItem? listBoxItem = sender as ListBoxItem;

                    if (listBoxItem?.DataContext is FilterOrderItem item)
                    {
                        DragDrop.DoDragDrop(listBoxItem, item, DragDropEffects.Move);
                    }
                    _isDragging = false;
                }
            }
        }

        private void OnOrderListDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        private void OnOrderListDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(FilterOrderItem)) is FilterOrderItem draggedItem)
            {
                if (sender is not ListBox listBox)
                    return;

                Point dropPosition = e.GetPosition(listBox);

                int newIndex = GetDropIndex(listBox, dropPosition);
                int oldIndex = FilterOrderItems.IndexOf(draggedItem);

                if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0)
                {
                    FilterOrderItems.Move(oldIndex, newIndex);
                    SaveOrder();
                }
            }
        }

        private int GetDropIndex(ListBox listBox, Point dropPosition)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem item)
                {
                    var bounds = VisualTreeHelper.GetDescendantBounds(item);
                    var itemPosition = item.TranslatePoint(new Point(0, 0), listBox);
                    var itemRect = new Rect(itemPosition, bounds.Size);

                    if (dropPosition.Y < itemRect.Bottom)
                    {
                        return i;
                    }
                }
            }
            return listBox.Items.Count - 1;
        }
    }
}
