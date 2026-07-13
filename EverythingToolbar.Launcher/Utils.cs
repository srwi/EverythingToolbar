using System;
using System.Diagnostics;
using System.IO;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using NLog;
using Shell32;
using File = System.IO.File;

namespace EverythingToolbar.Launcher
{
    internal class Utils
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<Utils>();

        public static bool IsTaskbarPinned()
        {
            var taskBarPath = GetTaskbarShortcutPath();
            return File.Exists(taskBarPath);
        }

        public static string GetTaskbarShortcutPath()
        {
            var taskBarPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"
            );

            if (Directory.Exists(taskBarPath) && GetExecutablePath() is { } executablePath)
            {
                try
                {
                    var executableFileName = Path.GetFileName(executablePath);
                    var lnkFiles = Directory.GetFiles(taskBarPath, "*.lnk");
                    var shell = new Shell();
                    foreach (var lnkFile in lnkFiles)
                    {
                        var folder = shell.NameSpace(Path.GetDirectoryName(lnkFile));
                        var folderItem = folder.ParseName(Path.GetFileName(lnkFile));
                        if (folderItem is { IsLink: true })
                        {
                            var link = (ShellLinkObject)folderItem.GetLink;
                            var linkFileName = Path.GetFileName(link.Path);

                            if (linkFileName == executableFileName)
                                return lnkFile;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to scan taskbar icon links. Using default path...");
                }
            }

            return Path.Combine(taskBarPath, "EverythingToolbar.lnk");
        }

        public static void ChangeTaskbarPinIcon(string iconName, bool restartExplorer)
        {
            if (GetExecutablePath() is not { } executableFilename)
                return;

            var taskbarShortcutPath = GetTaskbarShortcutPath();

            if (File.Exists(taskbarShortcutPath))
                File.Delete(taskbarShortcutPath);

            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(taskbarShortcutPath);
            shortcut.TargetPath = executableFilename;
            shortcut.IconLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconName);
            shortcut.Save();

            if (!restartExplorer)
                return;

            foreach (var process in Process.GetProcessesByName("explorer"))
                process.Kill();
        }

        public static string GetThemedAppIconPath(bool absolute = false)
        {
            var relativePath = "Icons/Medium.ico";

            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
                );
                object? systemUsesLightTheme = key?.GetValue("SystemUsesLightTheme");
                if (systemUsesLightTheme != null && (int)systemUsesLightTheme == 1)
                {
                    relativePath = "Icons/Light.ico";
                }
                else
                {
                    relativePath = "Icons/Dark.ico";
                }
            }
            catch (Exception)
            {
                Logger.Error("Failed to get icon name.");
            }

            if (absolute)
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            return relativePath;
        }

        private static string? GetExecutablePath()
        {
            if (Process.GetCurrentProcess().MainModule is not { } mainModule)
                return null;

            return mainModule.FileName;
        }
    }
}
