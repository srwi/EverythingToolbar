using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class LauncherOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public LauncherOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsTrayIconEnabled) or nameof(IsSetupAssistantDisabled))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsTrayIconEnabled
        {
            get => ToolbarSettings.User.IsTrayIconEnabled;
            set => ToolbarSettings.User.IsTrayIconEnabled = value;
        }

        public bool IsSetupAssistantDisabled
        {
            get => ToolbarSettings.User.IsSetupAssistantDisabled;
            set => ToolbarSettings.User.IsSetupAssistantDisabled = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}