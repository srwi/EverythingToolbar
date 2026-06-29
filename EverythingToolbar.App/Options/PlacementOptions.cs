using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class PlacementOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public PlacementOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(PopupWidth) or nameof(PopupHeight))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public int PopupWidth
        {
            get => ToolbarSettings.User.PopupWidth;
            set => ToolbarSettings.User.PopupWidth = value;
        }

        public int PopupHeight
        {
            get => ToolbarSettings.User.PopupHeight;
            set => ToolbarSettings.User.PopupHeight = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}