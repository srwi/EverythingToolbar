using System.Diagnostics;
using System.IO;
using EverythingToolbar.Core.Platform;

namespace EverythingToolbar.Services
{
    public sealed class FileLauncherAdapter : IFileLauncher
    {
        public void Open(string path, string? workingDirectory = null)
        {
            if (Directory.Exists(path) && !path.EndsWith("\\"))
                path += "\\";

            Process.Start(new ProcessStartInfo(path) { WorkingDirectory = workingDirectory, UseShellExecute = true });
        }

        public void OpenAsAdmin(string path)
        {
            Process.Start(new ProcessStartInfo(path) { Verb = "runas", UseShellExecute = true });
        }

        public void OpenWithArguments(string path, string arguments)
        {
            Process.Start(path, arguments);
        }

        public void RunCommand(string commandLine, string? workingDirectory = null)
        {
            ShellUtils.CreateProcessFromCommandLine(commandLine, workingDirectory);
        }
    }
}
