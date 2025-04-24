using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace EverythingToolbar.Helpers
{
    public enum Edge : uint
    {
        Left,
        Top,
        Right,
        Bottom,
    }

    public class TaskbarStateManager : INotifyPropertyChanged
    {
        public static readonly TaskbarStateManager Instance = new TaskbarStateManager();

        private TaskbarStateManager() { }

        private Edge _taskbarEdge = Edge.Bottom;
        public Edge TaskbarEdge
        {
            get => _taskbarEdge;
            set
            {
                _taskbarEdge = value;
                NotifyPropertyChanged();
            }
        }

        private Size _taskbarSize;
        public Size TaskbarSize
        {
            get => _taskbarSize;
            set
            {
                _taskbarSize = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isIcon;
        public bool IsIcon
        {
            get => _isIcon;
            set
            {
                _isIcon = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}