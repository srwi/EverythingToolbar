using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NLog;

namespace EverythingToolbar.Helpers
{
    public class HistoryManager
    {
        private static readonly ILogger _logger = ToolbarLogger.GetLogger<HistoryManager>();
        private static readonly string HISTORY_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                                   "EverythingToolbar",
                                                                   "history.xml");
        private static readonly int MAX_HISTORY_SIZE = 50;
        private readonly List<string> history = new List<string>(MAX_HISTORY_SIZE);
        private int currentIndex;
        private int currentHistorySize;

        public static HistoryManager Instance = new HistoryManager();

        private HistoryManager()
        {
            history = LoadHistory();
            currentIndex = history.Count;
            currentHistorySize = ToolbarSettings.User.IsEnableHistory ? MAX_HISTORY_SIZE : 0;
            ToolbarSettings.User.PropertyChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ToolbarSettings.User.IsEnableHistory))
            {
                if (ToolbarSettings.User.IsEnableHistory)
                {
                    currentHistorySize = MAX_HISTORY_SIZE;
                }
                else
                {
                    currentHistorySize = 0;
                    ClearHistory();
                }
            }
        }

        public List<string> LoadHistory()
        {
            if (File.Exists(HISTORY_PATH))
            {
                try
                {
                    var serializer = new XmlSerializer(history.GetType());
                    using (var reader = XmlReader.Create(HISTORY_PATH))
                    {
                        return (List<string>)serializer.Deserialize(reader);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to load search term history.");
                }
            }

            return new List<string>();
        }

        public void SaveHistory()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HISTORY_PATH));
                var serializer = new XmlSerializer(history.GetType());
                using (var writer = XmlWriter.Create(HISTORY_PATH))
                {
                    serializer.Serialize(writer, history);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to save search term history.");
            }
        }

        public void ClearHistory()
        {
            history.Clear();
            SaveHistory();
        }

        public void AddToHistory(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return;

            if (history.Count > 0 && history.Last() == searchTerm)
                return;

            history.Add(searchTerm);
            while (history.Count > currentHistorySize)
                history.RemoveAt(0);
            currentIndex = history.Count;
            SaveHistory();
        }

        public string GetPreviousItem()
        {
            if (history.Count == 0)
                return "";

            currentIndex = Math.Max(0, currentIndex - 1);
            return history.ElementAt(currentIndex);
        }

        public string GetNextItem()
        {
            if (history.Count == 0)
                return "";

            if (currentIndex >= history.Count - 1)
            {
                currentIndex = history.Count;
                return "";
            }

            currentIndex = Math.Min(currentIndex + 1, history.Count - 1);
            return history.ElementAt(currentIndex);
        }
    }
}
