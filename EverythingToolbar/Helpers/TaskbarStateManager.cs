using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        public static TaskbarStateManager Instance = new TaskbarStateManager();

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

        private double _taskbarHeight;
        public double TaskbarHeight
        {
            get => _taskbarHeight;
            set
            {
                _taskbarHeight = value;
                NotifyPropertyChanged();
            }
        }

        private double _taskbarWidth;
        public double TaskbarWidth
        {
            get => _taskbarWidth;
            set
            {
                _taskbarWidth = value;
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

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
