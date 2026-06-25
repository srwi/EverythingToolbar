using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace EverythingToolbar
{
    public sealed class ShortcutOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ShortcutOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ShortcutModifiers) or nameof(ShortcutKey))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public int ShortcutModifiers
        {
            get => ToolbarSettings.User.ShortcutModifiers;
            set => ToolbarSettings.User.ShortcutModifiers = value;
        }

        public int ShortcutKey
        {
            get => ToolbarSettings.User.ShortcutKey;
            set => ToolbarSettings.User.ShortcutKey = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}