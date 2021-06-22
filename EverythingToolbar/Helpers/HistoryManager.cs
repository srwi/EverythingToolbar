using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace EverythingToolbar.Helpers
{
    class HistoryManager
    {
        private string historyPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EverythingToolbar", "history.xml");
        private const int MAX_HISTORY_SIZE = 50;
        private int currentIndex;
        private int maxHistoryCount;
        private List<string> history = new List<string>(MAX_HISTORY_SIZE);

        public static HistoryManager Instance = new HistoryManager();

        private HistoryManager()
        {
            history = LoadHistory();
            currentIndex = history.Count;
            maxHistoryCount = Properties.Settings.Default.isEnableHistory ? MAX_HISTORY_SIZE : 0;
            Properties.Settings.Default.PropertyChanged += OnSettingChanged;
        }

        private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isEnableHistory")
            {
                if (Properties.Settings.Default.isEnableHistory)
                {
                    maxHistoryCount = MAX_HISTORY_SIZE;
                }
                else
                {
                    maxHistoryCount = 0;
                    history.Clear();
                }
            }
        }

        public List<string> LoadHistory()
        {
            if (File.Exists(historyPath))
            {
                var serializer = new XmlSerializer(history.GetType());
                using (var reader = XmlReader.Create(historyPath))
                {
                    return (List<string>)serializer.Deserialize(reader);
                }
            }

            return new List<string>();
        }

        public void SaveHistory()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(historyPath));
            var serializer = new XmlSerializer(history.GetType());
            using (var writer = XmlWriter.Create(historyPath))
            {
                serializer.Serialize(writer, history);
            }
        }

        public void AddToHistory(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return;

            if (history.Count > 0 && history.ElementAt(history.Count - 1) == searchTerm)
                return;

            history.Add(searchTerm);
            while (history.Count > maxHistoryCount)
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
