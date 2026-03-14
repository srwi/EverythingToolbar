using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Serialization;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Data
{
    public enum FileType
    {
        Any,
        File,
        Folder,
    }

    [Serializable]
    public class Rule : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
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

        public bool ExpressionValid
        {
            get
            {
                try
                {
                    bool _ = Regex.IsMatch("", Expression);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
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

                Icon = null;
            }
        }

        [field: NonSerialized]
        private ImageSource? _icon;

        [field: NonSerialized]
        private bool _iconLoadFailed;

        [XmlIgnore]
        public ImageSource? Icon
        {
            get
            {
                if (_icon != null || _iconLoadFailed)
                    return _icon;

                if (string.IsNullOrWhiteSpace(Command))
                {
                    _iconLoadFailed = true;
                    return null;
                }

                string executableName = GetExecutableFromCommandLine(Command);
                if (string.IsNullOrWhiteSpace(executableName))
                {
                    _iconLoadFailed = true;
                    return null;
                }

                string? executablePath = FindExecutablePath(executableName);
                if (executablePath == null)
                {
                    _iconLoadFailed = true;
                    return null;
                }

                _icon = IconProvider.GetImage(
                    executablePath,
                    true,
                    16,
                    source =>
                    {
                        Icon = source;
                    }
                );

                if (_icon == null)
                    _iconLoadFailed = true;

                return _icon;
            }
            set
            {
                _iconLoadFailed = false;
                _icon = value;
                NotifyPropertyChanged();
            }
        }

        public static string GetExecutableFromCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return string.Empty;

            commandLine = commandLine.Trim();

            if (commandLine.StartsWith("\""))
            {
                int endQuote = commandLine.IndexOf("\"", 1);
                return endQuote > 0 ? commandLine.Substring(1, endQuote - 1) : commandLine.Substring(1);
            }

            int spaceIndex = commandLine.IndexOf(' ');
            return spaceIndex > 0 ? commandLine.Substring(0, spaceIndex) : commandLine;
        }

        public static string? FindExecutablePath(string exeName)
        {
            if (File.Exists(exeName))
                return Path.GetFullPath(exeName);

            string[] extensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? [".exe"];
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];

            foreach (var path in paths)
            {
                foreach (var ext in extensions)
                {
                    string candidate = Path.Combine(
                        path,
                        exeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? exeName : exeName + ext
                    );
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            return null;
        }
    }
}
