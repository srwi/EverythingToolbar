using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace EverythingToolbar.Data
{
    public enum FileType
    {
        Any,
        File,
        Folder
    }

    [Serializable]
    public class Rule : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        private FileType _filetype;
        public FileType Type
        {
            get => _filetype;
            set
            {
                _filetype = value;
                NotifyPropertyChanged();
            }
        }

        private string _expression;
        public string Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ExpressionValid));
            }
        }

        private string _command;
        public string Command
        {
            get => _command;
            set
            {
                _command = value;
                NotifyPropertyChanged();
            }
        }

        public bool ExpressionValid
        {
            get
            {
                try
                {
                    Regex.IsMatch("", Expression);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }
    }
}