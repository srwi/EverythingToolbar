using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class FilterOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public FilterOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(LastFilter) or nameof(IsRememberFilter)
                    or nameof(IsImportFilters) or nameof(FiltersPath) or nameof(FilterOrder)
                    or nameof(MaxTabItems))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string LastFilter
        {
            get => ToolbarSettings.User.LastFilter;
            set => ToolbarSettings.User.LastFilter = value;
        }

        public bool IsRememberFilter
        {
            get => ToolbarSettings.User.IsRememberFilter;
            set => ToolbarSettings.User.IsRememberFilter = value;
        }

        public bool IsImportFilters
        {
            get => ToolbarSettings.User.IsImportFilters;
            set => ToolbarSettings.User.IsImportFilters = value;
        }

        public string FiltersPath
        {
            get => ToolbarSettings.User.FiltersPath;
            set => ToolbarSettings.User.FiltersPath = value;
        }

        public string FilterOrder
        {
            get => ToolbarSettings.User.FilterOrder;
            set => ToolbarSettings.User.FilterOrder = value;
        }

        public int MaxTabItems
        {
            get => ToolbarSettings.User.MaxTabItems;
            set => ToolbarSettings.User.MaxTabItems = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}