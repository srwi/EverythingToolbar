using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class SortOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SortOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(SortBy) or nameof(IsSortDescending))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public int SortBy
        {
            get => ToolbarSettings.User.SortBy;
            set => ToolbarSettings.User.SortBy = value;
        }

        public bool IsSortDescending
        {
            get => ToolbarSettings.User.IsSortDescending;
            set => ToolbarSettings.User.IsSortDescending = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}