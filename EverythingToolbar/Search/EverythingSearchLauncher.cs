using System.IO;
using System.Windows.Forms;
using EverythingToolbar.Data;
using EverythingToolbar.Helpers;
using EverythingToolbar.Platform;
using NLog;

namespace EverythingToolbar.Search
{
    public sealed class EverythingSearchLauncher(
        INotifier notifier,
        IFileLauncher fileLauncher,
        EverythingOptions everythingOptions)
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<EverythingSearchLauncher>();

        public void OpenSearchInEverything(SearchState searchState, string filenameToHighlight = "")
        {
            if (!File.Exists(everythingOptions.EverythingPath))
            {
                notifier.ShowInformation("MessageBoxSelectEverythingExe");

                using var openFileDialog = new OpenFileDialog();
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "Everything.exe|Everything.exe|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    everythingOptions.EverythingPath = openFileDialog.FileName;
                }
                else
                {
                    return;
                }
            }

            var searchTerm = searchState.BuildSearchTerm();
            var args = "";
            if (!string.IsNullOrEmpty(everythingOptions.InstanceName))
                args += " -instance \"" + everythingOptions.InstanceName + "\"";
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
            fileLauncher.OpenWithArguments(everythingOptions.EverythingPath, args);
        }
    }
}