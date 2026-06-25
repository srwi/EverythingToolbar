using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class MatchOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public MatchOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(IsMatchCase) or nameof(IsMatchPath)
                    or nameof(IsMatchWholeWord) or nameof(IsRegExEnabled))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public bool IsMatchCase
        {
            get => ToolbarSettings.User.IsMatchCase;
            set => ToolbarSettings.User.IsMatchCase = value;
        }

        public bool IsMatchPath
        {
            get => ToolbarSettings.User.IsMatchPath;
            set => ToolbarSettings.User.IsMatchPath = value;
        }

        public bool IsMatchWholeWord
        {
            get => ToolbarSettings.User.IsMatchWholeWord;
            set => ToolbarSettings.User.IsMatchWholeWord = value;
        }

        public bool IsRegExEnabled
        {
            get => ToolbarSettings.User.IsRegExEnabled;
            set => ToolbarSettings.User.IsRegExEnabled = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}