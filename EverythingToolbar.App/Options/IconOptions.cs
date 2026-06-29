using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class IconOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IconOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IconName))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string IconName
        {
            get => ToolbarSettings.User.IconName;
            set => ToolbarSettings.User.IconName = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}