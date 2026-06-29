using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class CustomActionsOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public CustomActionsOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsAutoApplyCustomActions))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsAutoApplyCustomActions
        {
            get => ToolbarSettings.User.IsAutoApplyCustomActions;
            set => ToolbarSettings.User.IsAutoApplyCustomActions = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}