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

        private Edge _taskbarEdge;
        public Edge TaskbarEdge
        {
            get => _taskbarEdge;
            set
            {
                _taskbarEdge = value;
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

        private bool _isDeskband;
        public bool IsDeskband
        {
            get => _isDeskband;
            set
            {
                _isDeskband = value;
                NotifyPropertyChanged();

                if (!_isDeskband)
                    IsIcon = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
