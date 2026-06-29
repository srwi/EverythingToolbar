using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EverythingToolbar
{
    public sealed class ThemeOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ThemeOptions()
        {
            ToolbarSettings.User.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ThemeOverride) or nameof(ItemTemplate)
                    or nameof(ForceWin10Behavior) or nameof(IsAnimationsDisabled)
                    or nameof(IsThumbnailsEnabled))
                    OnPropertyChanged(e.PropertyName);
            };
        }

        public string ThemeOverride
        {
            get => ToolbarSettings.User.ThemeOverride;
            set => ToolbarSettings.User.ThemeOverride = value;
        }

        public string ItemTemplate
        {
            get => ToolbarSettings.User.ItemTemplate;
            set => ToolbarSettings.User.ItemTemplate = value;
        }

        public bool ForceWin10Behavior
        {
            get => ToolbarSettings.User.ForceWin10Behavior;
            set => ToolbarSettings.User.ForceWin10Behavior = value;
        }

        public bool IsAnimationsDisabled
        {
            get => ToolbarSettings.User.IsAnimationsDisabled;
            set => ToolbarSettings.User.IsAnimationsDisabled = value;
        }

        public bool IsThumbnailsEnabled
        {
            get => ToolbarSettings.User.IsThumbnailsEnabled;
            set => ToolbarSettings.User.IsThumbnailsEnabled = value;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}