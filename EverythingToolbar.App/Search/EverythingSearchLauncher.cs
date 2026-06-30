using System.IO;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Platform;
using NLog;

namespace EverythingToolbar.Search
{
    public sealed class EverythingSearchLauncher(
        INotifier notifier,
        IShellDialogs shellDialogs,
        IFileLauncher fileLauncher,
        ISettings settings)
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<EverythingSearchLauncher>();

        public void OpenSearchInEverything(SearchState searchState, string filenameToHighlight = "")
        {
            if (!File.Exists(settings.EverythingPath))
            {
                notifier.ShowInformation("MessageBoxSelectEverythingExe");

                var pickedPath = shellDialogs.BrowseForFile("Everything.exe", "Everything.exe", "c:\\");
                if (pickedPath != null)
                {
                    settings.EverythingPath = pickedPath;
                }
                else
                {
                    return;
                }
            }

            var searchTerm = searchState.BuildSearchTerm();
            var args = "";
            if (!string.IsNullOrEmpty(settings.InstanceName))
                args += " -instance \"" + settings.InstanceName + "\"";
            if (!string.IsNullOrEmpty(filenameToHighlight))
                args += " -select \"" + filenameToHighlight + "\"";
            args += " -sort \"" + searchState.SortBy.ToCliName() + "\"";
            args += searchState.IsSortDescending ? " -sort-descending" : " -sort-ascending";
            args += searchState.IsMatchCase ? " -case" : " -nocase";
            args += searchState.IsMatchPath ? " -matchpath" : " -nomatchpath";
            args += searchState.IsMatchWholeWord && !searchState.IsRegExEnabled ? " -ww" : " -noww";
            args += searchState.IsRegExEnabled ? " -regex" : " -noregex";
            args += " -s \"" + searchTerm.Replace("\"", "\"\"") + "\"";

            Logger.Debug("Showing in Everything with args: " + args);
            fileLauncher.OpenWithArguments(settings.EverythingPath, args);
        }
    }
}