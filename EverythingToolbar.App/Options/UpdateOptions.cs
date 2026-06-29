using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class UpdateOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public UpdateOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsUpdateNotificationsEnabled)
                    or nameof(SkippedUpdate)
                    or nameof(VersionBeforeUpdate))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsUpdateNotificationsEnabled
        {
            get => ToolbarSettings.User.IsUpdateNotificationsEnabled;
            set => ToolbarSettings.User.IsUpdateNotificationsEnabled = value;
        }

        public string SkippedUpdate
        {
            get => ToolbarSettings.User.SkippedUpdate;
            set => ToolbarSettings.User.SkippedUpdate = value;
        }

        public string VersionBeforeUpdate
        {
            get => ToolbarSettings.User.VersionBeforeUpdate;
            set => ToolbarSettings.User.VersionBeforeUpdate = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}