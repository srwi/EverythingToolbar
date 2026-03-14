using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Settings
{
    public class FilterOrderItem
    {
        public string Name { get; set; }
        public int OriginalIndex { get; init; }
    }

    public partial class Filters : INotifyPropertyChanged
    {
        private ObservableCollection<FilterOrderItem> _filterOrderItems;
        private bool _isDragging;
        private Point _startPoint;

        public ObservableCollection<FilterOrderItem> FilterOrderItems
        {
            get => _filterOrderItems;
            set
            {
                _filterOrderItems = value;
                OnPropertyChanged();
            }
        }

        public Filters()
        {
            InitializeComponent();
            DataContext = this;

            LoadFilterOrder();
        }

        private void LoadFilterOrder()
        {
            var defaultFilters = DefaultFilterLoader.Instance.DefaultFilters;

            // Use the validation logic from DefaultFilterLoader
            var validOrder = DefaultFilterLoader.Instance.GetValidFilterOrder();

            FilterOrderItems = new ObservableCollection<FilterOrderItem>(
                validOrder.Select(i => new FilterOrderItem { Name = defaultFilters[i].Name, OriginalIndex = i })
            );
        }

        private void SaveOrder()
        {
            var orderString = string.Join(",", FilterOrderItems.Select(item => item.OriginalIndex));
            ToolbarSettings.User.FilterOrder = orderString;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
