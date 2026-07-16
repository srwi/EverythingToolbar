using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using EverythingToolbar.App.Helpers;
using NLog;

namespace EverythingToolbar.App.Services
{
    public class SearchHistoryService
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<SearchHistoryService>();
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EverythingToolbar",
            "history.xml"
        );

        private const int MaxHistorySize = 50;

        private int _currentHistorySize;
        private int _currentIndex;
        private readonly List<string> _history;
        private readonly ISettings _settings;

        public SearchHistoryService(ISettings settings)
        {
            _settings = settings;
            _history = LoadHistory();
            _currentIndex = _history.Count;
            _currentHistorySize = _settings.IsEnableHistory ? MaxHistorySize : 0;
            _settings.PropertyChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettings.IsEnableHistory))
            {
                if (_settings.IsEnableHistory)
                {
                    _currentHistorySize = MaxHistorySize;
                }
                else
                {
                    _currentHistorySize = 0;
                    ClearHistory();
                }
            }
        }

        private List<string> LoadHistory()
        {
            if (!File.Exists(HistoryPath))
                return [];

            try
            {
                var serializer = new XmlSerializer(typeof(List<string>));
                using var reader = XmlReader.Create(HistoryPath);
                return serializer.Deserialize(reader) as List<string> ?? [];
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to load search term history.");
            }

            return [];
        }

        private void SaveHistory()
        {
            try
            {
                if (Path.GetDirectoryName(HistoryPath) is { } path)
                    Directory.CreateDirectory(path);

                var serializer = new XmlSerializer(typeof(List<string>));
                using var writer = XmlWriter.Create(HistoryPath);
                serializer.Serialize(writer, _history);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to save search term history.");
            }
        }

        public void ClearHistory()
        {
            _history.Clear();

            SaveHistory();
        }

        public void AddToHistory(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return;

            if (_history.Count > 0 && _history.Last() == searchTerm)
                return;

            _history.Add(searchTerm);
            while (_history.Count > _currentHistorySize)
                _history.RemoveAt(0);
            _currentIndex = _history.Count;

            SaveHistory();
        }

        public string GetPreviousItem()
        {
            if (_history.Count == 0)
                return "";

            _currentIndex = Math.Max(0, _currentIndex - 1);
            return _history.ElementAt(_currentIndex);
        }

        public string GetNextItem()
        {
            if (_history.Count == 0)
                return "";

            if (_currentIndex >= _history.Count - 1)
            {
                _currentIndex = _history.Count;
                return "";
            }

            _currentIndex = Math.Min(_currentIndex + 1, _history.Count - 1);
            return _history.ElementAt(_currentIndex);
        }
    }
}
