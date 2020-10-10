using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EverythingToolbar
{
	class EverythingSearch
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

		public string SearchTerm { get; set; }
		public string SearchMacro { get; set; }

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

		public static readonly EverythingSearch Instance = new EverythingSearch();
		private readonly ILogger logger;

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
		}

		public IEnumerable<SearchResult> Query(int offset, int count)
		{
			uint flags = EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME;
			flags |= EVERYTHING_REQUEST_HIGHLIGHTED_FILE_NAME;
			flags |= Properties.Settings.Default.isDetailedView ? EVERYTHING_REQUEST_HIGHLIGHTED_PATH : (uint)0;

			Everything_SetSearchW(SearchMacro + SearchTerm);
			Everything_SetRequestFlags(flags);
			Everything_SetSort((uint)Properties.Settings.Default.sortBy);
			Everything_SetMatchCase(Properties.Settings.Default.isMatchCase);
			Everything_SetMatchPath(Properties.Settings.Default.isMatchPath);
			Everything_SetMatchWholeWord(Properties.Settings.Default.isMatchWholeWord);
			Everything_SetRegex(Properties.Settings.Default.isRegExEnabled);
			Everything_SetMax((uint)count);
			Everything_SetOffset((uint)offset);

			if(!Everything_QueryW(true))
			{
				HandleError((ErrorCode)Everything_GetLastError());
			}

			uint resultsCount = Everything_GetNumResults();

			for (uint i = 0; i < resultsCount; i++)
			{
				string path = Properties.Settings.Default.isDetailedView ? Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i)).ToString() : "";
				string filename = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i)).ToString();
				bool isFile = Everything_IsFileResult(i);
				StringBuilder full_path = new StringBuilder(4096);
				Everything_GetResultFullPathNameW(i, full_path, 4096);

				yield return new SearchResult()
				{
					Path = path.ToString(),
					FullPathAndFileName = full_path.ToString(),
					FileName = filename,
					IsFile = isFile
				};
			}
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
