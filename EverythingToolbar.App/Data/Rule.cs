using System;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EverythingToolbar.App.Data
{
    public enum FileType
    {
        Any,
        File,
        Folder,
    }

    [Serializable]
    public partial class Rule : ObservableObject
    {
        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private FileType _type;

        [ObservableProperty]
        private string _expression = "";

        [XmlIgnore]
        private Regex? _compiledRegex;

        [XmlIgnore]
        private bool _expressionValid = true;

        partial void OnExpressionChanged(string? oldValue, string newValue)
        {
            try
            {
                _compiledRegex = new Regex(newValue ?? "", RegexOptions.Compiled);
                _expressionValid = true;
            }
            catch (ArgumentException)
            {
                _compiledRegex = null;
                _expressionValid = false;
            }

            OnPropertyChanged(nameof(ExpressionValid));
        }

        public bool ExpressionValid => _expressionValid;

        public bool IsExpressionMatch(string input)
        {
            if (string.IsNullOrEmpty(Expression) || input == null)
                return false;

            return _compiledRegex?.IsMatch(input) ?? false;
        }

        [ObservableProperty]
        private string _command = "";
    }
}
