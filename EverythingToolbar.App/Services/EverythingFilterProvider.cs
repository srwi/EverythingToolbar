using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using EverythingToolbar.App.Helpers;
using EverythingToolbar.Core.Data;
using EverythingToolbar.Core.Platform;
using Microsoft.VisualBasic.FileIO;
using NLog;

namespace EverythingToolbar.App.Services
{
    public class EverythingFilterProvider : ObservableObject
    {
        private ObservableCollection<Filter>? _filters;
        public ObservableCollection<Filter>? Filters => _filters ??= LoadFilters();

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<EverythingFilterProvider>();
        private FileSystemWatcher? _watcher;
        private readonly ISettings _settings;
        private readonly IFilterNames _names;
        private readonly INotifier _notifier;
        private readonly IShellDialogs _shellDialogs;
        private readonly SynchronizationContext? _syncContext;

        public EverythingFilterProvider(
            IFilterNames names,
            INotifier notifier,
            IShellDialogs shellDialogs,
            ISettings settings
        )
        {
            _names = names;
            _notifier = notifier;
            _shellDialogs = shellDialogs;
            _settings = settings;
            _syncContext = SynchronizationContext.Current;
            _settings.PropertyChanged += OnSettingsChanged;

            if (_settings.IsImportFilters)
                CreateFileWatcher();
        }

        private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.IsImportFilters))
            {
                if (_settings.IsImportFilters)
                {
                    CreateFileWatcher();
                }
                else
                {
                    StopFileWatcher();
                }
                ResetFilters();
            }
            else if (e.PropertyName == nameof(ISettings.FiltersPath))
            {
                if (_settings.IsImportFilters)
                {
                    CreateFileWatcher();
                    ResetFilters();
                }
            }
        }

        private void ResetFilters()
        {
            _filters = null;
            OnPropertyChanged(nameof(Filters));
        }

        private ObservableCollection<Filter>? LoadFilters()
        {
            var filters = new ObservableCollection<Filter>();

            if (string.IsNullOrWhiteSpace(_settings.FiltersPath))
                _settings.FiltersPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Everything",
                    "Filters.csv"
                );

            if (!File.Exists(_settings.FiltersPath))
            {
                Logger.Info("Filters.csv could not be found at " + _settings.FiltersPath);

                _notifier.ShowInformation("MessageBoxSelectFiltersCsv");
                var pickedPath = _shellDialogs.BrowseForFile(
                    "Filters.csv",
                    "Filters.csv",
                    Path.Combine(_settings.FiltersPath, "..")
                );

                if (pickedPath != null)
                {
                    _settings.FiltersPath = pickedPath;
                }
                else
                {
                    _settings.IsImportFilters = false;
                    return null;
                }
            }

            try
            {
                using var csvParser = new TextFieldParser(_settings.FiltersPath);
                csvParser.CommentTokens = ["#"];
                csvParser.SetDelimiters(",");
                csvParser.HasFieldsEnclosedInQuotes = true;

                var header = csvParser.ReadFields();

                if (header == null)
                    return filters;

                while (!csvParser.EndOfData)
                {
                    var fields = csvParser.ReadFields();

                    if (fields == null)
                        continue;

                    var filterDict = new Dictionary<string, string>();
                    for (var i = 0; i < Math.Min(header.Length, fields.Length); i++)
                        filterDict[header[i]] = fields[i];

                    filters.Add(ParseFilterFromDict(filterDict));
                }

                return filters;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Parsing Filters.csv failed.");
            }

            return null;
        }

        private static string GetColumnOrEmpty(Dictionary<string, string> row, string column)
        {
            if (!row.TryGetValue(column, out var value))
                return "";

            return value;
        }

        private Filter ParseFilterFromDict(Dictionary<string, string> dict)
        {
            return new Filter
            {
                Name = LocalizeName(GetColumnOrEmpty(dict, "Name")),
                IsMatchCase = GetColumnOrEmpty(dict, "Case") == "1",
                IsMatchWholeWord = GetColumnOrEmpty(dict, "Whole Word") == "1",
                IsMatchPath = GetColumnOrEmpty(dict, "Path") == "1",
                IsRegExEnabled = GetColumnOrEmpty(dict, "Regex") == "1",
                Search = GetColumnOrEmpty(dict, "Search"),
                Macro = GetColumnOrEmpty(dict, "Macro"),
            };
        }

        private string LocalizeName(string name) =>
            name.Trim() switch
            {
                "EVERYTHING" => _names.All,
                "FILE" => _names.File,
                "FOLDER" => _names.Folder,
                "AUDIO" => _names.Audio,
                "COMPRESSED" => _names.Compressed,
                "DOCUMENT" => _names.Document,
                "EXECUTABLE" => _names.Executable,
                "PICTURE" or "IMAGE" => _names.Picture,
                "VIDEO" => _names.Video,
                _ => name,
            };

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

            if (!File.Exists(_settings.FiltersPath))
                return;

            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(_settings.FiltersPath)!,
                Filter = Path.GetFileName(_settings.FiltersPath),
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
                _syncContext.Post(_ => _settings.FiltersPath = e.FullPath, null);
            else
                _settings.FiltersPath = e.FullPath;
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            ResetFilters();
        }
    }
}
