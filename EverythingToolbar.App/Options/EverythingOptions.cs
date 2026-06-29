using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class EverythingOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EverythingOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(EverythingPath) or nameof(InstanceName))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string EverythingPath
        {
            get => ToolbarSettings.User.EverythingPath;
            set => ToolbarSettings.User.EverythingPath = value;
        }

        public string InstanceName
        {
            get => ToolbarSettings.User.InstanceName;
            set => ToolbarSettings.User.InstanceName = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}