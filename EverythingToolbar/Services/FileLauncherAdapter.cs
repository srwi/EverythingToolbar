using System;
using System.ComponentModel;
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
            try
            {
                Process.Start(new ProcessStartInfo(path) { Verb = "runas", UseShellExecute = true });
            }
            catch (Win32Exception e) when (e.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                // The user dismissed the UAC elevation prompt; treat as a no-op rather than a failure.
                throw new OperationCanceledException();
            }
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
