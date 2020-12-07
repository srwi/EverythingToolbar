using EverythingToolbar.Data;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace EverythingToolbar.Helpers
{
	class FilterLoader : INotifyPropertyChanged
	{
		public ObservableCollection<Filter> DefaultFilters { get; } = new ObservableCollection<Filter>()
		{
			new Filter {
				Name = "All",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "",
				Search = ""
			},
			new Filter {
				Name = "File",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "",
				Search = "file:"
			},
			new Filter {
				Name = "Folder",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "",
				Search = "folder:"
			}
		};

		private readonly ObservableCollection<Filter> defaultUserFilters = new ObservableCollection<Filter>()
		{
			new Filter {
				Name = "Audio",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "audio",
				Search = "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm"
			},
			new Filter {
				Name = "Compressed",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "zip",
				Search = "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;r25;r26;r27;r28;r29;rar;tar;tgz;z;zip"
			},
			new Filter {
				Name = "Document",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "doc",
				Search = "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml"
			},
			new Filter {
				Name = "Executable",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "exe",
				Search = "ext:bat;cmd;exe;msi;msp;scr"
			},
			new Filter {
				Name = "Picture",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "pic",
				Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf"
			},
			new Filter {
				Name = "Video",
				IsMatchCase = false,
				IsMatchWholeWord = false,
				IsMatchPath = false,
				IsRegExEnabled = false,
				Macro = "video",
				Search = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv"
			}
		};
		public ObservableCollection<Filter> UserFilters { get; set; }

		public static readonly FilterLoader Instance = new FilterLoader();
		public event PropertyChangedEventHandler PropertyChanged;
		private FileSystemWatcher watcher;

		private FilterLoader()
		{
			if (Properties.Settings.Default.filtersPath == "")
				Properties.Settings.Default.filtersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
																	   "Everything",
																	   "Filters.csv");
			Properties.Settings.Default.PropertyChanged += OnPropertyChanged;

			RefreshFilters();
			CreateFileWatcher();
		}

		private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "isRegExEnabled")
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DefaultFilters"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserFilters"));
			}
			else if (e.PropertyName == "isImportFilters")
			{
				RefreshFilters();
			}
		}

		void RefreshFilters()
		{
			if (Properties.Settings.Default.isImportFilters)
				UserFilters = LoadFilters();
			else
				UserFilters = defaultUserFilters;

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserFilters"));
		}

		ObservableCollection<Filter> LoadFilters()
		{
			var filters = new ObservableCollection<Filter>();

			if (!File.Exists(Properties.Settings.Default.filtersPath))
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Info("Filters.csv could not be found at " + Properties.Settings.Default.filtersPath);

				string everythingIniPath = Path.Combine(Properties.Settings.Default.filtersPath, "..", "Everything.ini");
				if (!File.Exists(everythingIniPath))
				{
					ToolbarLogger.GetLogger("EverythingToolbar").Info("Everything.ini could not be found.");
					MessageBox.Show("Pleae select Filters.csv. It gets created after editing filters in Everything for the first time.");
					using (OpenFileDialog openFileDialog = new OpenFileDialog())
					{
						openFileDialog.InitialDirectory = "c:\\";
						openFileDialog.Filter = "Filters.csv|Filters.csv|All files (*.*)|*.*";
						openFileDialog.FilterIndex = 1;

						if (openFileDialog.ShowDialog() == DialogResult.OK)
						{
							Properties.Settings.Default.filtersPath = openFileDialog.FileName;
							Properties.Settings.Default.Save();
						}
						else
						{
							return defaultUserFilters;
						}
					}
				}
				else
				{
					ToolbarLogger.GetLogger("EverythingToolbar").Info("Filters.csv does not exist. Using default filters.");
					return defaultUserFilters;
				}
			}

			try
			{
				using (TextFieldParser csvParser = new TextFieldParser(Properties.Settings.Default.filtersPath))
				{
					csvParser.CommentTokens = new string[] { "#" };
					csvParser.SetDelimiters(new string[] { "," });
					csvParser.HasFieldsEnclosedInQuotes = true;

					// Skip header row
					csvParser.ReadLine();

					while (!csvParser.EndOfData)
					{
						string[] fields = csvParser.ReadFields();

						// Skip default filters
						string search = fields[6].Trim();
						if (search == "file:" ||
							search == "folder:" ||
							search == "")
							continue;

						// Everything's default filters are uppercase
						fields[0] = fields[0].Replace("AUDIO", "Audio");
						fields[0] = fields[0].Replace("COMPRESSED", "Compressed");
						fields[0] = fields[0].Replace("DOCUMENT", "Document");
						fields[0] = fields[0].Replace("EXECUTABLE", "Executable");
						fields[0] = fields[0].Replace("PICTURE", "Picture");
						fields[0] = fields[0].Replace("VIDEO", "Video");

						filters.Add(new Filter()
						{
							Name = fields[0],
							IsMatchCase = fields[1] == "1",
							IsMatchWholeWord = fields[2] == "1",
							IsMatchPath = fields[3] == "1",
							IsRegExEnabled = fields[5] == "1",
							Search = fields[6],
							Macro = fields[7]
						});
					}
				}
			}
			catch (Exception e)
			{
				ToolbarLogger.GetLogger("EverythingToolbar").Error(e, "Parsing Filters.csv failed.");
				return defaultUserFilters;
			}

			return filters;
		}

		public void CreateFileWatcher()
		{
			watcher = new FileSystemWatcher
			{
				Path = Path.GetDirectoryName(Properties.Settings.Default.filtersPath),
				Filter = Path.GetFileName(Properties.Settings.Default.filtersPath),
				NotifyFilter = NotifyFilters.FileName
			};

			watcher.Changed += new FileSystemEventHandler(OnFileChanged);
			watcher.Created += new FileSystemEventHandler(OnFileChanged);
			watcher.Deleted += new FileSystemEventHandler(OnFileChanged);
			watcher.Renamed += new RenamedEventHandler(OnFileRenamed);

			watcher.EnableRaisingEvents = true;
		}

		private void OnFileRenamed(object sender, RenamedEventArgs e)
		{
			RefreshFilters();
		}

		private void OnFileChanged(object source, FileSystemEventArgs e)
		{
			RefreshFilters();
		}
	}
}
