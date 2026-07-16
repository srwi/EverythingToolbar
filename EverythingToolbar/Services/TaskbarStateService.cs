using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EverythingToolbar.Services
{
    public enum Edge : uint
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    public sealed partial class TaskbarStateService : ObservableObject
    {
        [ObservableProperty]
        private Edge _taskbarEdge = Edge.Bottom;

        [ObservableProperty]
        private Size _taskbarSize;

        [ObservableProperty]
        private bool _isIcon;
    }
}
