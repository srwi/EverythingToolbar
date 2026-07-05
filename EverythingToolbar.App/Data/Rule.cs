using System;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EverythingToolbar.Data
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

        partial void OnExpressionChanged(string? oldValue, string newValue)
        {
            OnPropertyChanged(nameof(ExpressionValid));
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

        [ObservableProperty]
        private string _command = "";
    }
}