using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using EverythingToolbar.App.Data;
using EverythingToolbar.App.Helpers;
using EverythingToolbar.Platform;
using NLog;

namespace EverythingToolbar.App.Services
{
    public sealed class CustomActionService(
        ISettings settings,
        INotifier notifier,
        IFileLauncher fileLauncher)
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<CustomActionService>();

        private static string CustomActionsPath =>
            Path.Combine(ConfigPaths.GetConfigDirectory(), "rules.xml");

        public List<Rule> Load()
        {
            if (!File.Exists(CustomActionsPath))
                return [];

            try
            {
                var serializer = new XmlSerializer(typeof(List<Rule>));
                using var reader = XmlReader.Create(CustomActionsPath);
                return serializer.Deserialize(reader) as List<Rule> ?? [];
            }
            catch
            {
                return [];
            }
        }

        public void Save(List<Rule> rules)
        {
            if (Path.GetDirectoryName(CustomActionsPath) is { } parent)
                Directory.CreateDirectory(parent);

            var serializer = new XmlSerializer(typeof(List<Rule>));
            using var writer = XmlWriter.Create(CustomActionsPath);
            serializer.Serialize(writer, rules);
        }

        public bool TryRun(SearchResult? searchResult, string command = "")
        {
            if (searchResult == null)
                return false;

            if (settings.IsAutoApplyCustomActions && string.IsNullOrEmpty(command))
            {
                foreach (var r in Load())
                {
                    var regexCond =
                        !string.IsNullOrEmpty(r.Expression)
                        && Regex.IsMatch(searchResult.FullPathAndFileName, r.Expression);
                    var typeCond =
                        searchResult.IsFile && r.Type != FileType.Folder
                        || !searchResult.IsFile && r.Type != FileType.File;
                    if (regexCond && typeCond)
                    {
                        command = r.Command;
                    }
                }
            }

            if (!string.IsNullOrEmpty(command))
            {
                command = command.Replace("%file%", "\"" + searchResult.FullPathAndFileName + "\"");
                command = command.Replace("%filename%", "\"" + searchResult.FileName + "\"");
                command = command.Replace("%path%", "\"" + searchResult.Path + "\"");
                try
                {
                    fileLauncher.RunCommand(command, searchResult.Path);
                    return true;
                }
                catch (Win32Exception e)
                {
                    Logger.Error(e, "Failed to run custom action command.");
                    notifier.ShowError("MessageBoxFailedToRunCommand", command);
                }
            }

            return false;
        }
    }
}