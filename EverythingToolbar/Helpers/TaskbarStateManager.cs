using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EverythingToolbar.Helpers
{
    public enum Edge : uint
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    public sealed partial class TaskbarStateManager : ObservableObject
    {
        [ObservableProperty]
        private Edge _taskbarEdge = Edge.Bottom;

        [ObservableProperty]
        private Size _taskbarSize;

        [ObservableProperty]
        private bool _isIcon;
    }
}
