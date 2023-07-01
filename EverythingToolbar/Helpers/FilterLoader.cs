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
    internal class FilterLoader : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Filter> _defaultFilters = new ObservableCollection<Filter>
        {
            new Filter {
                Name = Resources.DefaultFilterAll,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE71D  " : "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "",
                Search = ""
            },
            new Filter {
                Name = Resources.DefaultFilterFile,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE7C3  " : "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "",
                Search = "file:"
            },
            new Filter {
                Name = Resources.DefaultFilterFolder,
                Icon = Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? "\xE8B7  " : "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "",
                Search = "folder:"
            }
        };
        public ObservableCollection<Filter> DefaultFilters
        { 
            get
            {
                if (Settings.Default.isRegExEnabled)
                    return new ObservableCollection<Filter>(_defaultFilters.Skip(0).Take(1));
                
                return _defaultFilters;
            }
        }
        
        public readonly ObservableCollection<Filter> DefaultUserFilters = new ObservableCollection<Filter>()
        {
            new Filter {
                Name = Resources.UserFilterAudio,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "audio",
                Search = "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm"
            },
            new Filter {
                Name = Resources.UserFilterCompressed,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "zip",
                Search = "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;r25;r26;r27;r28;r29;rar;tar;tgz;z;zip"
            },
            new Filter {
                Name = Resources.UserFilterDocument,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "doc",
                Search = "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml"
            },
            new Filter {
                Name = Resources.UserFilterExecutable,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "exe",
                Search = "ext:bat;cmd;exe;msi;msp;scr"
            },
            new Filter {
                Name = Resources.UserFilterPicture,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
                Macro = "pic",
                Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf"
            },
            new Filter {
                Name = Resources.UserFilterVideo,
                Icon = "",
                IsMatchCase = false,
                IsMatchWholeWord = false,
                IsMatchPath = false,
                IsRegExEnabled = false,
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
                    return new ObservableCollection<Filter>();

                if (Settings.Default.isImportFilters)
                    return _userFiltersCache ?? LoadFilters();

                return DefaultUserFilters;
            }
        }

        public static readonly FilterLoader Instance = new FilterLoader();
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<FilterLoader>();
        private FileSystemWatcher _watcher;

        private FilterLoader()
        {
            if (Settings.Default.filtersPath == "")
                Settings.Default.filtersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                       "Everything",
                                                                       "Filters.csv");
            Settings.Default.PropertyChanged += OnSettingsChanged;

            NotifyFiltersChanged();
            
            if (Settings.Default.isImportFilters)
                CreateFileWatcher();
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "isRegExEnabled":
                    NotifyFiltersChanged();
                    break;
                case "isImportFilters":
                {
                    if (Settings.Default.isImportFilters)
                    {
                        CreateFileWatcher();
                    }
                    else
                    {
                        StopFileWatcher();
                        _userFiltersCache = null;
                    }
                
                    NotifyFiltersChanged();
                    break;
                }
            }
        }

        private void NotifyFiltersChanged()
        {
            NotifyPropertyChanged(nameof(DefaultFilters));
            NotifyPropertyChanged(nameof(UserFilters));
        }

        private ObservableCollection<Filter> LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (!File.Exists(Settings.Default.filtersPath))
            {
                Logger.Info("Filters.csv could not be found at " + Settings.Default.filtersPath);

                MessageBox.Show(Resources.MessageBoxSelectFiltersCsv,
                                Resources.MessageBoxSelectFiltersCsvTitle,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                using (var openFileDialog = new OpenFileDialog())
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
                using (var csvParser = new TextFieldParser(Settings.Default.filtersPath))
                {
                    csvParser.CommentTokens = new[] { "#" };
                    csvParser.SetDelimiters(",");
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    var header = csvParser.ReadFields();

                    while (!csvParser.EndOfData)
                    {
                        var fields = csvParser.ReadFields();

                        if (header == null || fields == null)
                            continue;
                        
                        var filter = header.Zip(fields, (h, f) => new { h, f }).ToDictionary(x => x.h, x => x.f);

                        // Skip default filters
                        if (filter["Name"] == "EVERYTHING" || filter["Name"] == "FOLDER")
                            continue;

                        // Everything's default filters are uppercase
                        filter["Name"] = filter["Name"].Replace("AUDIO", Resources.UserFilterAudio);
                        filter["Name"] = filter["Name"].Replace("COMPRESSED", Resources.UserFilterCompressed);
                        filter["Name"] = filter["Name"].Replace("DOCUMENT", Resources.UserFilterDocument);
                        filter["Name"] = filter["Name"].Replace("EXECUTABLE", Resources.UserFilterExecutable);
                        filter["Name"] = filter["Name"].Replace("PICTURE", Resources.UserFilterPicture);
                        filter["Name"] = filter["Name"].Replace("VIDEO", Resources.UserFilterVideo);

                        filters.Add(new Filter()
                        {
                            Name = filter["Name"],
                            IsMatchCase = filter["Case"] == "1",
                            IsMatchWholeWord = filter["Whole Word"] == "1",
                            IsMatchPath = filter["Path"] == "1",
                            IsRegExEnabled = filter["Regex"] == "1",
                            Search = filter["Search"],
                            Macro = filter["Macro"]
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Parsing Filters.csv failed.");
                return DefaultUserFilters;
            }

            _userFiltersCache = filters;
            return filters;
        }

        private void StopFileWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
        }

        private void CreateFileWatcher()
        {
            if (!File.Exists(Settings.Default.filtersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(Settings.Default.filtersPath),
                Filter = Path.GetFileName(Settings.Default.filtersPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Settings.Default.filtersPath = e.FullPath;
            CreateFileWatcher();
            LoadFilters();
            NotifyFiltersChanged();
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            LoadFilters();
            NotifyFiltersChanged();
        }

        public Filter GetLastFilter()
        {
            if (!Settings.Default.isRememberFilter)
                return DefaultFilters[0];
            
            foreach (var filter in DefaultFilters.Union(UserFilters))
            {
                if (filter.Name == Settings.Default.lastFilter)
                    return filter;
            }

            return DefaultFilters[0];
        }
    }
}
