using System;
using System.Diagnostics;
using System.IO;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using File = System.IO.File;

namespace EverythingToolbar.Launcher
{
    internal class Utils
    {
        public enum WindowsTheme
        {
            Dark,
            Light
        }

        public static class WindowsVersion
        {
            public static Version Windows10 = new Version(10, 0, 10240);
            public static Version Windows10Anniversary = new Version(10, 0, 14393);
            public static Version Windows11 = new Version(10, 0, 22000);
        }

        public static string GetTaskbarShortcutPath()
        {
            const string relativePath = @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\EverythingToolbar.lnk";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), relativePath);
        }

        public static bool IsTaskbarCenterAligned()
        {
            if (Environment.OSVersion.Version < WindowsVersion.Windows11)
                return false;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
            {
                object taskbarAlignment = key?.GetValue("TaskbarAl");
                bool leftAligned = taskbarAlignment != null && (int)taskbarAlignment == 0;
                return !leftAligned;
            }
        }

        public static bool GetWindowsSearchEnabledState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
            {
                object searchboxTaskbarMode = key?.GetValue("SearchboxTaskbarMode");
                return searchboxTaskbarMode != null && (int)searchboxTaskbarMode > 0;
            }
        }

        public static void SetWindowsSearchEnabledState(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search", true))
            {
                key.SetValue("SearchboxTaskbarMode", enabled ? 1 : 0);
            }
        }

        public static bool GetAutostartState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                return key?.GetValue("EverythingToolbar") != null;
            }
        }

        public static void SetAutostartState(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enabled)
                    key.SetValue("EverythingToolbar", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                else
                    key.DeleteValue("EverythingToolbar", false);
            }
        }

        public static WindowsTheme GetWindowsTheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object systemUsesLightTheme = key?.GetValue("SystemUsesLightTheme");
                bool isLightTheme = systemUsesLightTheme != null && (int)systemUsesLightTheme > 0;
                return isLightTheme ? WindowsTheme.Light : WindowsTheme.Dark;
            }
        }

        public static string GetIconPath(WindowsTheme theme)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", theme.ToString() + ".ico");
        }

        public static string GetThemedIconPath()
        {
            return GetIconPath(GetWindowsTheme());
        }

        public static void ChangeTaskbarPinIcon(WindowsTheme theme)
        {
            string taskbarShortcutPath = GetTaskbarShortcutPath();

            if (File.Exists(taskbarShortcutPath))
                File.Delete(taskbarShortcutPath);

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(taskbarShortcutPath);
            shortcut.TargetPath = Process.GetCurrentProcess().MainModule.FileName;
            shortcut.IconLocation = GetIconPath(theme);
            shortcut.Save();

            foreach (Process process in Process.GetProcessesByName("explorer"))
            {
                process.Kill();
            }
        }
    }
}
