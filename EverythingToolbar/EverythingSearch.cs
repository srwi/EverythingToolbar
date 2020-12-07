using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;

namespace EverythingToolbar
{
	class EverythingSearch : INotifyPropertyChanged
	{
		private enum ErrorCode
		{
			EVERYTHING_OK,
			EVERYTHING_ERROR_MEMORY,
			EVERYTHING_ERROR_IPC,
			EVERYTHING_ERROR_REGISTERCLASSEX,
			EVERYTHING_ERROR_CREATEWINDOW,
			EVERYTHING_ERROR_CREATETHREAD,
			EVERYTHING_ERROR_INVALIDINDEX,
			EVERYTHING_ERROR_INVALIDCALL
		}

		private const int EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
		private const int EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME = 0x00002000;
		private const int EVERYTHING_REQUEST_HIGHLIGHTED_PATH = 0x00004000;

		[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
		private static extern uint Everything_SetSearchW(string lpSearchString);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetMatchPath(bool bEnable);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetMatchCase(bool bEnable);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetMatchWholeWord(bool bEnable);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetRegex(bool bEnable);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetMax(uint dwMax);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetOffset(uint dwOffset);
		[DllImport("Everything64.dll")]
		private static extern bool Everything_QueryW(bool bWait);
		[DllImport("Everything64.dll")]
		private static extern uint Everything_GetNumResults();
		[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
		private static extern void Everything_GetResultFullPathNameW(uint nIndex, StringBuilder lpString, uint nMaxCount);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetSort(uint dwSortType);
		[DllImport("Everything64.dll")]
		private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
		[DllImport("Everything64.dll")]
		private static extern bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);
		[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultHighlightedFileName(uint nIndex);
		[DllImport("Everything64.dll")]
		private static extern uint Everything_IncRunCountFromFileName(string lpFileName);
		[DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr Everything_GetResultHighlightedPath(uint nIndex);
		[DllImport("Everything64.dll")]
		private static extern bool Everything_IsFileResult(uint nIndex);
		[DllImport("Everything64.dll")]
		public static extern uint Everything_GetLastError();
		[DllImport("Everything64.dll")]
		public static extern uint Everything_GetMajorVersion();
		[DllImport("Everything64.dll")]
		public static extern uint Everything_GetMinorVersion();
		[DllImport("Everything64.dll")]
		public static extern uint Everything_GetRevision();

		private string _searchTerm;
		public string SearchTerm
		{
			get
			{
				return _searchTerm;
			}
			set
			{
				_searchTerm = value; 
				SearchResults.Clear();
				QueryBatch();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SearchTerm"));
			}
		}

		private Filter _currentFilter;
		public Filter CurrentFilter
		{
			get
			{
				return _currentFilter ?? FilterLoader.Instance.DefaultFilters[0];
			}
			set
			{
				_currentFilter = value;
				SearchResults.Clear();
				QueryBatch();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentFilter"));
			}
		}

		public ObservableCollection<SearchResult> SearchResults = new ObservableCollection<SearchResult>();
		public int BatchSize = 100;
		public static readonly EverythingSearch Instance = new EverythingSearch();
		private readonly object _searchResultsLock = new object();
		private readonly ILogger logger;
		private CancellationTokenSource cancellationTokenSource;
		private CancellationToken cancellationToken;

		public event PropertyChangedEventHandler PropertyChanged;

		private EverythingSearch()
		{
			logger = ToolbarLogger.GetLogger("EverythingToolbar");

			try
			{
				uint major = Everything_GetMajorVersion();
				uint minor = Everything_GetMinorVersion();
				uint revision = Everything_GetRevision();

				if ((major > 1) || ((major == 1) && (minor > 4)) || ((major == 1) && (minor == 4) && (revision >= 1)))
				{
					logger.Info("Everything version: {major}.{minor}.{revision}", major, minor, revision);
				}
				else
				{
					logger.Error("Everything version {major}.{minor}.{revision} is not supported.", major, minor, revision);
				}
			}
			catch (Exception e)
			{
				logger.Error(e, "Everything64.dll could not be opened.");
			}

			Properties.Settings.Default.PropertyChanged += OnSettingChanged;
			BindingOperations.EnableCollectionSynchronization(SearchResults, _searchResultsLock);
		}

		private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "isMatchCase" ||
				e.PropertyName == "isRegExEnabled" ||
				e.PropertyName == "isMatchPath" ||
				e.PropertyName == "isMatchWholeWord" ||
				e.PropertyName == "sortBy")
			{
				SearchResults.Clear();
				QueryBatch();
			}
		}

		public void QueryBatch()
		{
			cancellationTokenSource?.Cancel();

			if (SearchTerm == null)
				return;

			cancellationTokenSource = new CancellationTokenSource();
			cancellationToken = cancellationTokenSource.Token;

			Task.Run(() =>
			{
				try
				{
					uint flags = EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME;
					flags |= EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME;
					flags |= EVERYTHING_REQUEST_HIGHLIGHTED_PATH;

					Everything_SetSearchW(CurrentFilter.Search + " " + SearchTerm);
					Everything_SetRequestFlags(flags);
					Everything_SetSort((uint)Properties.Settings.Default.sortBy);
					Everything_SetMatchCase(Properties.Settings.Default.isMatchCase);
					Everything_SetMatchPath(Properties.Settings.Default.isMatchPath);
					Everything_SetMatchWholeWord(Properties.Settings.Default.isMatchWholeWord);
					Everything_SetRegex(Properties.Settings.Default.isRegExEnabled);
					Everything_SetMax((uint)BatchSize);
					Everything_SetOffset((uint)SearchResults.Count);

					if (!Everything_QueryW(true))
					{
						HandleError((ErrorCode)Everything_GetLastError());
						return;
					}

					uint resultsCount = Everything_GetNumResults();

					for (uint i = 0; i < resultsCount; i++)
					{
						cancellationToken.ThrowIfCancellationRequested();
						string path = Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i)).ToString();
						string filename = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i)).ToString();
						bool isFile = Everything_IsFileResult(i);
						StringBuilder fullPath = new StringBuilder(4096);
						Everything_GetResultFullPathNameW(i, fullPath, 4096);

						lock (_searchResultsLock)
						{
							SearchResults.Add(new SearchResult()
							{
								HighlightedPath = path.ToString(),
								FullPathAndFileName = fullPath.ToString(),
								HighlightedFileName = filename,
								IsFile = isFile
							});
						}
					}
				}
				catch (OperationCanceledException) { }
			}, cancellationToken);
		}

		public void Reset()
		{
			SearchTerm = null;
		}

		public void OpenLastSearchInEverything(string highlighted_file = "")
		{
			if(!File.Exists(Properties.Settings.Default.everythingPath))
			{
				MessageBox.Show("Please select Everything.exe...");
				using (OpenFileDialog openFileDialog = new OpenFileDialog())
				{
					openFileDialog.InitialDirectory = "c:\\";
					openFileDialog.Filter = "Everything.exe|Everything.exe|All files (*.*)|*.*";
					openFileDialog.FilterIndex = 1;

					if (openFileDialog.ShowDialog() == DialogResult.OK)
					{
						Properties.Settings.Default.everythingPath = openFileDialog.FileName;
						Properties.Settings.Default.Save();
					}
					else
					{
						return;
					}
				}
			}

			string args = "";
			if (Properties.Settings.Default.sortBy <= 2) args += " -sort \"Name\"";
			else if (Properties.Settings.Default.sortBy <= 4) args += " -sort \"Path\"";
			else if (Properties.Settings.Default.sortBy <= 6) args += " -sort \"Size\"";
			else if (Properties.Settings.Default.sortBy <= 8) args += " -sort \"Extension\"";
			else if (Properties.Settings.Default.sortBy <= 10) args += " -sort \"Type name\"";
			else if (Properties.Settings.Default.sortBy <= 12) args += " -sort \"Date created\"";
			else if (Properties.Settings.Default.sortBy <= 14) args += " -sort \"Date modified\"";
			else if (Properties.Settings.Default.sortBy <= 16) args += " -sort \"Attributes\"";
			else if (Properties.Settings.Default.sortBy <= 18) args += " -sort \"File list filename\"";
			else if (Properties.Settings.Default.sortBy <= 20) args += " -sort \"Run count\"";
			else if (Properties.Settings.Default.sortBy <= 22) args += " -sort \"Date recently changed\"";
			else if (Properties.Settings.Default.sortBy <= 24) args += " -sort \"Date accessed\"";
			else if (Properties.Settings.Default.sortBy <= 26) args += " -sort \"Date run\"";
			if (Properties.Settings.Default.sortBy % 2 > 0) args += " -sort-ascending";
			else args += " -sort-descending";
			if (highlighted_file != "") args += " -select \"" + highlighted_file + "\"";
			args += Properties.Settings.Default.isMatchCase ? " -case" : " -nocase";
			args += Properties.Settings.Default.isMatchPath ? " -matchpath" : " -nomatchpath";
			args += Properties.Settings.Default.isMatchWholeWord ? " -ww" : " -noww";
			args += Properties.Settings.Default.isRegExEnabled ? " -regex" : " -noregex";
			args += " -s \"" + (CurrentFilter.Search + " " + SearchTerm).Replace("\"", "\"\"") + "\"";

			Process.Start(Properties.Settings.Default.everythingPath, args);
		}

		public void IncrementRunCount(string path)
		{
			Everything_IncRunCountFromFileName(path);
		}

		private void HandleError(ErrorCode code)
		{
			switch(code)
			{
				case ErrorCode.EVERYTHING_ERROR_MEMORY:
					logger.Error("Failed to allocate memory for the search query.");
					break;
				case ErrorCode.EVERYTHING_ERROR_IPC:
					logger.Error("IPC is not available.");
					break;
				case ErrorCode.EVERYTHING_ERROR_REGISTERCLASSEX:
					logger.Error("Failed to register the search query window class.");
					break;
				case ErrorCode.EVERYTHING_ERROR_CREATEWINDOW:
					logger.Error("Failed to create the search query window.");
					break;
				case ErrorCode.EVERYTHING_ERROR_CREATETHREAD:
					logger.Error("Failed to create the search query thread.");
					break;
				case ErrorCode.EVERYTHING_ERROR_INVALIDINDEX:
					logger.Error("Invalid index.");
					break;
				case ErrorCode.EVERYTHING_ERROR_INVALIDCALL:
					logger.Error("Invalid call.");
					break;
			}
		}
	}
}
