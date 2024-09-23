using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Castle.Core.Internal;
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
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE71D" : ""
            },
            new Filter {
                Name = Resources.DefaultFilterFile,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE7C3" : "",
                Search = "file:"
            },
            new Filter {
                Name = Resources.DefaultFilterFolder,
                Icon = Utils.GetWindowsVersion() >= Utils.WindowsVersion.Windows10 ? "\xE8B7" : "",
                Search = "folder:"
            }
        };
        public ObservableCollection<Filter> DefaultFilters
        { 
            get
            {
                if (ToolbarSettings.User.IsRegExEnabled)
                    return new ObservableCollection<Filter>(_defaultFilters.Skip(0).Take(1));
                
                return _defaultFilters;
            }
        }
        
        public readonly ObservableCollection<Filter> DefaultUserFilters = new ObservableCollection<Filter>
        {
            new Filter {
                Name = Resources.UserFilterAudio,
                Macro = "audio",
                Search = "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;" +
                         "midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm"
            },
            new Filter {
                Name = Resources.UserFilterCompressed,
                Macro = "zip",
                Search = "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;" +
                         "r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;" +
                         "r25;r26;r27;r28;r29;rar;tar;tgz;z;zip"
            },
            new Filter {
                Name = Resources.UserFilterDocument,
                Macro = "doc",
                Search = "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;" +
                         "ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;" +
                         "pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml"
            },
            new Filter {
                Name = Resources.UserFilterExecutable,
                Macro = "exe",
                Search = "ext:bat;cmd;exe;msi;msp;scr"
            },
            new Filter {
                Name = Resources.UserFilterPicture,
                Macro = "pic",
                Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf"
            },
            new Filter {
                Name = Resources.UserFilterVideo,
                Macro = "video",
                Search = "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;" +
                         "dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;" +
                         "m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;" +
                         "ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;" +
                         "vob;vp6;webm;wm;wmp;wmv"
            }
        };
        private ObservableCollection<Filter> _userFiltersCache;
        public ObservableCollection<Filter> UserFilters
        { 
            get
            {
                if (ToolbarSettings.User.IsRegExEnabled)
                    return new ObservableCollection<Filter>();

                if (ToolbarSettings.User.IsImportFilters && _userFiltersCache.IsNullOrEmpty())
                    LoadFilters();

                return _userFiltersCache ?? DefaultUserFilters;
            }
        }

        public static readonly FilterLoader Instance = new FilterLoader();
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<FilterLoader>();
        private FileSystemWatcher _watcher;

        private FilterLoader()
        {
            if (ToolbarSettings.User.FiltersPath == "")
                ToolbarSettings.User.FiltersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                "Everything",
                                                                "Filters.csv");
            ToolbarSettings.User.PropertyChanged += OnSettingsChanged;

            NotifyFiltersChanged();
            
            if (ToolbarSettings.User.IsImportFilters)
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
                case nameof(ToolbarSettings.User.IsRegExEnabled):
                    NotifyFiltersChanged();
                    break;
                case nameof(ToolbarSettings.User.IsImportFilters):
                {
                    if (ToolbarSettings.User.IsImportFilters)
                    {
                        CreateFileWatcher();
                    }
                    else
                    {
                        StopFileWatcher();
                        _userFiltersCache = null;
                        foreach (var filter in DefaultFilters)
                            filter.Reset();
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

        private void LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (!File.Exists(ToolbarSettings.User.FiltersPath))
            {
                Logger.Info("Filters.csv could not be found at " + ToolbarSettings.User.FiltersPath);

                MessageBox.Show(Resources.MessageBoxSelectFiltersCsv,
                                Resources.MessageBoxSelectFiltersCsvTitle,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = Path.Combine(ToolbarSettings.User.FiltersPath, "..");
                    openFileDialog.Filter = "Filters.csv|Filters.csv|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ToolbarSettings.User.FiltersPath = openFileDialog.FileName;
                        CreateFileWatcher();
                    }
                    else
                    {
                        ToolbarSettings.User.IsImportFilters = false;
                        return;
                    }
                }
            }

            try
            {
                using (var csvParser = new TextFieldParser(ToolbarSettings.User.FiltersPath))
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
                        
                        var filterDict = header
                            .Zip(fields, (h, f) => new { h, f })
                            .ToDictionary(x => x.h, x => x.f);
                        var filter = parseFilterFromDict(filterDict);

                        if (filter.Name == "EVERYTHING")
                        {
                            _defaultFilters[0].IsMatchCase = filter.IsMatchCase;
                            _defaultFilters[0].IsMatchWholeWord = filter.IsMatchWholeWord;
                            _defaultFilters[0].IsMatchPath = filter.IsMatchPath;
                            _defaultFilters[0].IsRegExEnabled = filter.IsRegExEnabled;
                            _defaultFilters[0].Search = filter.Search;
                            _defaultFilters[0].Macro = filter.Macro;
                        }
                        else if (filter.Name == "FOLDER")
                        {
                            _defaultFilters[2].IsMatchCase = filter.IsMatchCase;
                            _defaultFilters[2].IsMatchWholeWord = filter.IsMatchWholeWord;
                            _defaultFilters[2].IsMatchPath = filter.IsMatchPath;
                            _defaultFilters[2].IsRegExEnabled = filter.IsRegExEnabled;
                            _defaultFilters[2].Search = filter.Search;
                            _defaultFilters[2].Macro = filter.Macro;
                        }
                        else
                        {
                            // Everything's default filters are uppercase
                            filter.Name = filter.Name
                                .Replace("AUDIO", Resources.UserFilterAudio)
                                .Replace("COMPRESSED", Resources.UserFilterCompressed)
                                .Replace("DOCUMENT", Resources.UserFilterDocument)
                                .Replace("EXECUTABLE", Resources.UserFilterExecutable)
                                .Replace("PICTURE", Resources.UserFilterPicture)
                                .Replace("VIDEO", Resources.UserFilterVideo);
                            filters.Add(filter);
                        }
                    }
                }
                _userFiltersCache = filters;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Parsing Filters.csv failed.");
            }

        }

        private Filter parseFilterFromDict(Dictionary<string, string> dict)
        {
            return new Filter
            {
                Name = dict["Name"],
                IsMatchCase = dict["Case"] == "1",
                IsMatchWholeWord = dict["Whole Word"] == "1",
                IsMatchPath = dict["Path"] == "1",
                IsRegExEnabled = dict["Regex"] == "1",
                Search = dict["Search"],
                Macro = dict["Macro"]
            };
        }

        private void StopFileWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
        }

        private void CreateFileWatcher()
        {
            if (!File.Exists(ToolbarSettings.User.FiltersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ToolbarSettings.User.FiltersPath),
                Filter = Path.GetFileName(ToolbarSettings.User.FiltersPath),
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
            ToolbarSettings.User.FiltersPath = e.FullPath;
            CreateFileWatcher();
            LoadFilters();
            NotifyFiltersChanged();
            EverythingSearch.Instance.Reset();
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            LoadFilters();
            NotifyFiltersChanged();
            EverythingSearch.Instance.Reset();
        }

        public Filter GetLastFilter()
        {
            if (!ToolbarSettings.User.IsRememberFilter)
                return DefaultFilters[0];
            
            foreach (var filter in DefaultFilters.Union(UserFilters))
            {
                if (filter.Name == ToolbarSettings.User.LastFilter)
                    return filter;
            }

            return DefaultFilters[0];
        }
    }
}
