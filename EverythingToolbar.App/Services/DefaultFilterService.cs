using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Linq;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.App.Services
{
    public class DefaultFilterService : ObservableObject
    {
        public Filter AllFilter { get; }

        public ObservableCollection<Filter> DefaultFilters { get; }

        public ObservableCollection<Filter> Filters => GetReorderedFilters();

        private readonly ISettings _settings;

        public DefaultFilterService(IFilterNames names, ISettings settings)
        {
            _settings = settings;

            AllFilter = new Filter { Name = names.All, Icon = Glyph("\xE71D") };

            DefaultFilters =
            [
                AllFilter,
                new()
                {
                    Name = names.File,
                    Icon = Glyph("\xE7C3"),
                    Search = "file:",
                },
                new()
                {
                    Name = names.Folder,
                    Icon = Glyph("\xE8B7"),
                    Search = "folder:",
                },
                new()
                {
                    Name = names.Audio,
                    Icon = Glyph("\xE8D6"),
                    Macro = "audio",
                    Search =
                        "ext:aac;ac3;aif;aifc;aiff;au;cda;dts;fla;flac;it;m1a;m2a;m3u;m4a;mid;"
                        + "midi;mka;mod;mp2;mp3;mpa;ogg;ra;rmi;spc;rmi;snd;umx;voc;wav;wma;xm",
                },
                new()
                {
                    Name = names.Compressed,
                    Icon = Glyph("\xE7B8"),
                    Macro = "zip",
                    Search =
                        "ext:7z;ace;arj;bz2;cab;gz;gzip;jar;r00;r01;r02;r03;r04;r05;r06;r07;"
                        + "r08;r09;r10;r11;r12;r13;r14;r15;r16;r17;r18;r19;r20;r21;r22;r23;r24;"
                        + "r25;r26;r27;r28;r29;rar;tar;tgz;z;zip",
                },
                new()
                {
                    Name = names.Document,
                    Icon = Glyph("\xF585"),
                    Macro = "doc",
                    Search =
                        "ext:c;chm;cpp;csv;cxx;doc;docm;docx;dot;dotm;dotx;h;hpp;htm;html;hxx;"
                        + "ini;java;lua;mht;mhtml;odt;pdf;potx;potm;ppam;ppsm;ppsx;pps;ppt;pptm;"
                        + "pptx;rtf;sldm;sldx;thmx;txt;vsd;wpd;wps;wri;xlam;xls;xlsb;xlsm;xlsx;xltm;xltx;xml",
                },
                new()
                {
                    Name = names.Executable,
                    Icon = Glyph("\xECAA"),
                    Macro = "exe",
                    Search = "ext:bat;cmd;exe;msi;msp;scr",
                },
                new()
                {
                    Name = names.Picture,
                    Icon = Glyph("\xE8B9"),
                    Macro = "pic",
                    Search = "ext:ani;bmp;gif;ico;jpe;jpeg;jpg;pcx;png;psd;tga;tif;tiff;webp;wmf",
                },
                new()
                {
                    Name = names.Video,
                    Icon = Glyph("\xE714"),
                    Macro = "video",
                    Search =
                        "ext:3g2;3gp;3gp2;3gpp;amr;amv;asf;avi;bdmv;bik;d2v;divx;drc;dsa;dsm;"
                        + "dss;dsv;evo;f4v;flc;fli;flic;flv;hdmov;ifo;ivf;m1v;m2p;m2t;m2ts;m2v;"
                        + "m4b;m4p;m4v;mkv;mp2v;mp4;mp4v;mpe;mpeg;mpg;mpls;mpv2;mpv4;mov;mts;ogm;"
                        + "ogv;pss;pva;qt;ram;ratdvd;rm;rmm;rmvb;roq;rpm;smil;smk;swf;tp;tpr;ts;"
                        + "vob;vp6;webm;wm;wmp;wmv",
                },
            ];

            _settings.PropertyChanged += OnSettingsChanged;
        }

        private static string Glyph(string glyph) =>
            Environment.OSVersion.Version >= Utils.WindowsVersion.Windows10 ? glyph : "";

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.FilterOrder))
            {
                OnPropertyChanged(nameof(Filters));
            }
        }

        private ObservableCollection<Filter> GetReorderedFilters()
        {
            var reorderedIndices = GetValidFilterOrder();
            var reordered = reorderedIndices.Select(i => DefaultFilters[i]).ToList();
            return new ObservableCollection<Filter>(reordered);
        }

        public int[] GetValidFilterOrder()
        {
            var order = _settings.FilterOrder;
            var validOrder = FilterOrderValidator.GetValidFilterOrder(order, DefaultFilters.Count);

            if (!string.IsNullOrWhiteSpace(order) && validOrder.SequenceEqual(Enumerable.Range(0, DefaultFilters.Count)))
                _settings.FilterOrder = string.Empty;

            return validOrder;
        }

    }
}