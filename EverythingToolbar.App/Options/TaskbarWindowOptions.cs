using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class TaskbarWindowOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public TaskbarWindowOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(TaskbarWindowAlignment) or nameof(TaskbarWindowEnabled)
                    or nameof(IsForceCenterAlignment))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string TaskbarWindowAlignment
        {
            get => ToolbarSettings.User.TaskbarWindowAlignment;
            set => ToolbarSettings.User.TaskbarWindowAlignment = value;
        }

        public bool TaskbarWindowEnabled
        {
            get => ToolbarSettings.User.TaskbarWindowEnabled;
            set => ToolbarSettings.User.TaskbarWindowEnabled = value;
        }

        public bool IsForceCenterAlignment
        {
            get => ToolbarSettings.User.IsForceCenterAlignment;
            set => ToolbarSettings.User.IsForceCenterAlignment = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}