using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using EverythingToolbar.Data;
using EverythingToolbar.Properties;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace EverythingToolbar.Helpers
{
    class FilterLoader : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Filter> _defaultFilters = new ObservableCollection<Filter>
        {
            new Filter {
                Name = Resources.DefaultFilterAll,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE71D  " : "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "",
                Search = ""
            },
            new Filter {
                Name = Resources.DefaultFilterFile,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE7C3  " : "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "",
                Search = "file:"
            },
            new Filter {
                Name = Resources.DefaultFilterFolder,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE8B7  " : "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "",
                Search = "folder:"
            }
        };
        public ObservableCollection<Filter> DefaultFilters
        { 
            get
            {
                if (Settings.Default.isRegExEnabled)
                {
                    return new ObservableCollection<Filter>(_defaultFilters.Skip(0).Take(1));
                }
                else
                {
                    return _defaultFilters;
                }
            }
        }
        
        public readonly ObservableCollection<Filter> DefaultUserFilters = new ObservableCollection<Filter>()
        {
            new Filter {
                Name = Resources.UserFilterAudio,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "audio",
                Search = "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm"
            },
            new Filter {
                Name = Resources.UserFilterCompressed,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "zip",
                Search = "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;r25;r26;r27;r28;r29;rar;tar;tgz;z;zip"
            },
            new Filter {
                Name = Resources.UserFilterDocument,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "doc",
                Search = "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml"
            },
            new Filter {
                Name = Resources.UserFilterExecutable,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "exe",
                Search = "ext:bat;cmd;exe;msi;msp;scr"
            },
            new Filter {
                Name = Resources.UserFilterPicture,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "pic",
                Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf"
            },
            new Filter {
                Name = Resources.UserFilterVideo,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "video",
                Search = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv"
            }
        };
        private ObservableCollection<Filter> _userFiltersCache;
        public ObservableCollection<Filter> UserFilters
        { 
            get
            {
                if (Settings.Default.isRegExEnabled)
                {
                    return new ObservableCollection<Filter>();
                }
                else
                {
                    if (Settings.Default.isImportFilters)
                    {
                        return _userFiltersCache ?? LoadFilters();
                    }
                    else
                    {
                        return DefaultUserFilters;
                    }
                }
            }
        }

        public static readonly FilterLoader Instance = new FilterLoader();
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly ILogger _logger = ToolbarLogger.GetLogger<FilterLoader>();
        private FileSystemWatcher _watcher;

        private FilterLoader()
        {
            if (Settings.Default.filtersPath == "")
                Settings.Default.filtersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                       "Everything",
                                                                       "Filters.csv");
            Settings.Default.PropertyChanged += OnSettingsChanged;

            RefreshFilters();
            CreateFileWatcher();
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled" || e.PropertyName == "isImportFilters")
                RefreshFilters();
        }

        private void RefreshFilters()
        {
            NotifyPropertyChanged(nameof(DefaultFilters));
            NotifyPropertyChanged(nameof(UserFilters));
        }

        private ObservableCollection<Filter> LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (!File.Exists(Settings.Default.filtersPath))
            {
                _logger.Info("Filters.csv could not be found at " + Settings.Default.filtersPath);

                MessageBox.Show(Resources.MessageBoxSelectFiltersCsv,
                                Resources.MessageBoxSelectFiltersCsvTitle,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = Path.Combine(Settings.Default.filtersPath, "..");
                    openFileDialog.Filter = "Filters.csv|Filters.csv|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Settings.Default.filtersPath = openFileDialog.FileName;
                        CreateFileWatcher();
                    }
                    else
                    {
                        Settings.Default.isImportFilters = false;
                        return DefaultUserFilters;
                    }
                }
            }

            try
            {
                using (TextFieldParser csvParser = new TextFieldParser(Settings.Default.filtersPath))
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
                        fields[0] = fields[0].Replace("AUDIO", Resources.UserFilterAudio);
                        fields[0] = fields[0].Replace("COMPRESSED", Resources.UserFilterCompressed);
                        fields[0] = fields[0].Replace("DOCUMENT", Resources.UserFilterDocument);
                        fields[0] = fields[0].Replace("EXECUTABLE", Resources.UserFilterExecutable);
                        fields[0] = fields[0].Replace("PICTURE", Resources.UserFilterPicture);
                        fields[0] = fields[0].Replace("VIDEO", Resources.UserFilterVideo);

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
                _logger.Error(e, "Parsing Filters.csv failed.");
                return DefaultUserFilters;
            }

            _userFiltersCache = filters;
            return filters;
        }

        private void CreateFileWatcher()
        {
            if (!File.Exists(Settings.Default.filtersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(Settings.Default.filtersPath),
                Filter = Path.GetFileName(Settings.Default.filtersPath),
                NotifyFilter = NotifyFilters.FileName
            };

            _watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            _watcher.Created += new FileSystemEventHandler(OnFileChanged);
            _watcher.Deleted += new FileSystemEventHandler(OnFileChanged);
            _watcher.Renamed += new RenamedEventHandler(OnFileRenamed);

            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            LoadFilters();
            RefreshFilters();
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            LoadFilters();
            RefreshFilters();
        }

        public Filter GetLastFilter()
        {
            if (Settings.Default.isRememberFilter)
            {
                foreach (Filter filter in DefaultFilters.Union(UserFilters))
                {
                    if (filter.Name == Settings.Default.lastFilter)
                        return filter;
                }
            }

            return DefaultFilters[0];
        }
    }
}
