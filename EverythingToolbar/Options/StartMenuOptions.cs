using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class StartMenuOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public StartMenuOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsReplaceStartMenuSearch))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsReplaceStartMenuSearch
        {
            get => ToolbarSettings.User.IsReplaceStartMenuSearch;
            set => ToolbarSettings.User.IsReplaceStartMenuSearch = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}