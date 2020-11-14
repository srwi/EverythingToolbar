using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace EverythingToolbar
{
	public enum FileType
	{
		Any,
		File,
		Folder
	}

	[Serializable()]
	public class Rule : INotifyPropertyChanged
	{
		[field: NonSerialized]
		public event PropertyChangedEventHandler PropertyChanged;

		private string _name;
		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
			}
		}

		private FileType _filetype;
		public FileType Type
		{
			get => _filetype;
			set
			{
				_filetype = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileType"));
			}
		}

		private string _expression;
		public string Expression
		{
			get => _expression;
			set
			{
				_expression = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Expression"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ExpressionValid"));
			}
		}

		private string _command;
		public string Command
		{
			get => _command;
			set
			{
				_command = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Command"));
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
