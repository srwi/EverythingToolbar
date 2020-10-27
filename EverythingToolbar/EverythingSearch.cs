using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EverythingToolbar
{
	class EverythingSearch
	{
		public static class Filter
		{
			public static string Audio { get { return "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm "; } }
			public static string Compressed { get { return "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;r25;r26;r27;r28;r29;rar;tar;tgz;z;zip "; } }
			public static string Document { get { return "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml "; } }
			public static string Everything { get { return ""; } }
			public static string Executable { get { return "ext:bat;cmd;exe;msi;msp;scr "; } }
			public static string File { get { return "file:"; } }
			public static string Folder { get { return "folder:"; } }
			public static string Picture { get { return "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf "; } }
			public static string Video { get { return "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv "; } }
		}

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

		public List<SearchResult> Query(int offset, int count)
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

			List<SearchResult> results = new List<SearchResult>();

			for (uint i = 0; i < resultsCount; i++)
			{
				string path = Properties.Settings.Default.isDetailedView ? Marshal.PtrToStringUni(Everything_GetResultHighlightedPath(i)).ToString() : "";
				string filename = Marshal.PtrToStringUni(Everything_GetResultHighlightedFileName(i)).ToString();
				bool isFile = Everything_IsFileResult(i);
				StringBuilder full_path = new StringBuilder(4096);
				Everything_GetResultFullPathNameW(i, full_path, 4096);

				results.Add(new SearchResult()
				{
					Path = path.ToString(),
					FullPathAndFileName = full_path.ToString(),
					FileName = filename,
					IsFile = isFile
				});
			}

            return results;
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

			string args = " -s" + " \"" + Regex.Replace(SearchMacro + SearchTerm, @"(\\+)$", @"$1$1") + "\"";
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
