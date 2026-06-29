using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class LanguageOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public LanguageOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(UILanguage))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string UILanguage
        {
            get => ToolbarSettings.User.UILanguage;
            set => ToolbarSettings.User.UILanguage = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}