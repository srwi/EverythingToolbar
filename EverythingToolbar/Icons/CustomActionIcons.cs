using System;
using System.IO;
using System.Windows.Media;

namespace EverythingToolbar.Icons
{
    public static class CustomActionIcons
    {
        public static ImageSource? Load(string command)
        {
            var executableName = GetExecutableFromCommandLine(command);
            if (string.IsNullOrWhiteSpace(executableName))
                return null;

            var executablePath = FindExecutablePath(executableName);
            if (executablePath == null)
                return null;

            return IconProvider.GetImage(executablePath, true, 16);
        }

        private static string GetExecutableFromCommandLine(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                return string.Empty;

            commandLine = commandLine.Trim();

            if (commandLine.StartsWith("\""))
            {
                int endQuote = commandLine.IndexOf('"', 1);
                return endQuote > 0 ? commandLine.Substring(1, endQuote - 1) : commandLine.Substring(1);
            }

            int spaceIndex = commandLine.IndexOf(' ');
            return spaceIndex > 0 ? commandLine.Substring(0, spaceIndex) : commandLine;
        }

        private static string? FindExecutablePath(string exeName)
        {
            if (File.Exists(exeName))
                return Path.GetFullPath(exeName);

            string[] extensions = Environment.GetEnvironmentVariable("PATHEXT")?.Split(';') ?? [".exe"];
            string[] paths = Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? [];

            foreach (var path in paths)
            {
                foreach (var ext in extensions)
                {
                    string candidate = Path.Combine(
                        path,
                        exeName.EndsWith(ext, StringComparison.OrdinalIgnoreCase) ? exeName : exeName + ext
                    );
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            return null;
        }
    }
}
