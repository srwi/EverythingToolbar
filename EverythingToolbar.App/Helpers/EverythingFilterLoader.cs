using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using EverythingToolbar.Data;
using EverythingToolbar.Platform;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace EverythingToolbar.Helpers
{
    public class EverythingFilterLoader : INotifyPropertyChanged
    {
        private ObservableCollection<Filter>? _filters;
        public ObservableCollection<Filter>? Filters => _filters ??= LoadFilters();

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<EverythingFilterLoader>();
        private FileSystemWatcher? _watcher;
        private readonly FilterOptions _options;
        private readonly IFilterNames _names;
        private readonly INotifier _notifier;
        private readonly IShellDialogs _shellDialogs;
        private readonly SynchronizationContext? _syncContext;

        public EverythingFilterLoader(IFilterNames names, INotifier notifier, IShellDialogs shellDialogs, FilterOptions options)
        {
            _names = names;
            _notifier = notifier;
            _shellDialogs = shellDialogs;
            _options = options;
            _syncContext = SynchronizationContext.Current;
            _options.PropertyChanged += OnSettingsChanged;

            if (_options.IsImportFilters)
                CreateFileWatcher();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterOptions.IsImportFilters))
            {
                if (_options.IsImportFilters)
                {
                    CreateFileWatcher();
                }
                else
                {
                    StopFileWatcher();
                }
                ResetFilters();
            }
            else if (e.PropertyName == nameof(FilterOptions.FiltersPath))
            {
                if (_options.IsImportFilters)
                {
                    CreateFileWatcher();
                    ResetFilters();
                }
            }
        }

        private void ResetFilters()
        {
            _filters = null;
            NotifyPropertyChanged(nameof(Filters));
        }

        private ObservableCollection<Filter>? LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (string.IsNullOrWhiteSpace(_options.FiltersPath))
                _options.FiltersPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Everything",
                    "Filters.csv"
                );

            if (!File.Exists(_options.FiltersPath))
            {
                Logger.Info("Filters.csv could not be found at " + _options.FiltersPath);

                _notifier.ShowInformation("MessageBoxSelectFiltersCsv");
                var pickedPath = _shellDialogs.BrowseForFile(
                    "Filters.csv", "Filters.csv", Path.Combine(_options.FiltersPath, ".."));

                if (pickedPath != null)
                {
                    _options.FiltersPath = pickedPath;
                }
                else
                {
                    _options.IsImportFilters = false;
                    return null;
                }
            }

            try
            {
                using var csvParser = new TextFieldParser(_options.FiltersPath);
                csvParser.CommentTokens = ["#"];
                csvParser.SetDelimiters(",");
                csvParser.HasFieldsEnclosedInQuotes = true;

                var header = csvParser.ReadFields();

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();

                    if (header == null || fields == null)
                        continue;

                    var filterDict = header.Zip(fields, (h, f) => new { h, f }).ToDictionary(x => x.h, x => x.f);
                    var filter = ParseFilterFromDict(filterDict);

                    filter.Name = filter
                        .Name.Replace("EVERYTHING", _names.All)
                        .Replace("FOLDER", _names.Folder)
                        .Replace("FILE", _names.File)
                        .Replace("AUDIO", _names.Audio)
                        .Replace("COMPRESSED", _names.Compressed)
                        .Replace("DOCUMENT", _names.Document)
                        .Replace("EXECUTABLE", _names.Executable)
                        .Replace("PICTURE", _names.Picture)
                        .Replace("VIDEO", _names.Video);
                    filters.Add(filter);
                }

                return filters;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Parsing Filters.csv failed.");
            }

            return null;
        }

        private Filter ParseFilterFromDict(Dictionary<string, string> dict)
        {
            return new Filter
            {
                Name = dict["Name"],
                IsMatchCase = dict["Case"] == "1",
                IsMatchWholeWord = dict["Whole Word"] == "1",
                IsMatchPath = dict["Path"] == "1",
                IsRegExEnabled = dict["Regex"] == "1",
                Search = dict["Search"],
                Macro = dict["Macro"],
            };
        }

        private void StopFileWatcher()
        {
            if (_watcher == null)
                return;

            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        private void CreateFileWatcher()
        {
            StopFileWatcher();

            if (!File.Exists(_options.FiltersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_options.FiltersPath)!,
                Filter = Path.GetFileName(_options.FiltersPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;

            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            // Marshal to the thread that captured _syncContext; this callback runs on a thread-pool thread.
            if (_syncContext != null)
                _syncContext.Post(_ => _options.FiltersPath = e.FullPath, null);
            else
                _options.FiltersPath = e.FullPath;
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            ResetFilters();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}