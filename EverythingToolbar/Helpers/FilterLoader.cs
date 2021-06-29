using EverythingToolbar.Data;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EverythingToolbar.Helpers
{
    class FilterLoader : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Filter> defaultFilters = new ObservableCollection<Filter>
        {
            new Filter {
                Name = Properties.Resources.DefaultFilterAll,
                Icon = "\xE71D  ",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "",
                Search = ""
            },
            new Filter {
                Name = Properties.Resources.DefaultFilterFile,
                Icon = "\xE7C3  ",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "",
                Search = "file:"
            },
            new Filter {
                Name = Properties.Resources.DefaultFilterFolder,
                Icon = "\xE8B7  ",
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
                if (Properties.Settings.Default.isRegExEnabled)
                {
                    return new ObservableCollection<Filter>(defaultFilters.Skip(0).Take(1));
                }
                else
                {
                    return defaultFilters;
                }
            }
        }
        

        public readonly ObservableCollection<Filter> DefaultUserFilters = new ObservableCollection<Filter>()
        {
            new Filter {
                Name = Properties.Resources.UserFilterAudio,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "audio",
                Search = "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm"
            },
            new Filter {
                Name = Properties.Resources.UserFilterCompressed,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "zip",
                Search = "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;r25;r26;r27;r28;r29;rar;tar;tgz;z;zip"
            },
            new Filter {
                Name = Properties.Resources.UserFilterDocument,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "doc",
                Search = "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml"
            },
            new Filter {
                Name = Properties.Resources.UserFilterExecutable,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "exe",
                Search = "ext:bat;cmd;exe;msi;msp;scr"
            },
            new Filter {
                Name = Properties.Resources.UserFilterPicture,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "pic",
                Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf"
            },
            new Filter {
                Name = Properties.Resources.UserFilterVideo,
                Icon = "",
                IsMatchCase = null,
                IsMatchWholeWord = null,
                IsMatchPath = null,
                IsRegExEnabled = null,
                Macro = "video",
                Search = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;vob;vp6;webm;wm;wmp;wmv"
            }
        };
        private ObservableCollection<Filter> userFiltersCache;
        public ObservableCollection<Filter> UserFilters
        { 
            get
            {
                if (Properties.Settings.Default.isRegExEnabled)
                {
                    return new ObservableCollection<Filter>();
                }
                else
                {
                    if (Properties.Settings.Default.isImportFilters)
                    {
                        return userFiltersCache ?? LoadFilters();
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
        private FileSystemWatcher watcher;

        private FilterLoader()
        {
            if (Properties.Settings.Default.filtersPath == "")
                Properties.Settings.Default.filtersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                       "Everything",
                                                                       "Filters.csv");
            Properties.Settings.Default.PropertyChanged += OnPropertyChanged;

            using (var ifc = new InstalledFontCollection())
            {
                if (!ifc.Families.Any(f => f.Name == "Segoe MDL2 Assets"))
                {
                    foreach (Filter defaultFilter in defaultFilters)
                        defaultFilter.Icon = "";
                }
            }

            RefreshFilters();
            CreateFileWatcher();
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRegExEnabled" || e.PropertyName == "isImportFilters")
                RefreshFilters();
        }

        private void RefreshFilters()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DefaultFilters"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UserFilters"));
        }

        private ObservableCollection<Filter> LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (!File.Exists(Properties.Settings.Default.filtersPath))
            {
                ToolbarLogger.GetLogger("EverythingToolbar").Info("Filters.csv could not be found at " + Properties.Settings.Default.filtersPath);

                MessageBox.Show(Properties.Resources.MessageBoxSelectFiltersCsv,
                                Properties.Resources.MessageBoxSelectFiltersCsvTitle,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = Path.Combine(Properties.Settings.Default.filtersPath, "..");
                    openFileDialog.Filter = "Filters.csv|Filters.csv|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Properties.Settings.Default.filtersPath = openFileDialog.FileName;
                        CreateFileWatcher();
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Properties.Settings.Default.isImportFilters = false;
                        Properties.Settings.Default.Save();
                        return DefaultUserFilters;
                    }
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
                        fields[0] = fields[0].Replace("AUDIO", Properties.Resources.UserFilterAudio);
                        fields[0] = fields[0].Replace("COMPRESSED", Properties.Resources.UserFilterCompressed);
                        fields[0] = fields[0].Replace("DOCUMENT", Properties.Resources.UserFilterDocument);
                        fields[0] = fields[0].Replace("EXECUTABLE", Properties.Resources.UserFilterExecutable);
                        fields[0] = fields[0].Replace("PICTURE", Properties.Resources.UserFilterPicture);
                        fields[0] = fields[0].Replace("VIDEO", Properties.Resources.UserFilterVideo);

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
                return DefaultUserFilters;
            }

            userFiltersCache = filters;
            return filters;
        }

        private void CreateFileWatcher()
        {
            if (!File.Exists(Properties.Settings.Default.filtersPath))
                return;

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
            if (Properties.Settings.Default.isRememberFilter)
            {
                foreach (Filter filter in DefaultFilters)
                {
                    if (filter.Name == Properties.Settings.Default.lastFilter)
                        return filter;
                }

                foreach (Filter filter in UserFilters)
                {
                    if (filter.Name == Properties.Settings.Default.lastFilter)
                        return filter;
                }
            }

            return DefaultFilters[0];
        }
    }
}
