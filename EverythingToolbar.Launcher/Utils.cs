using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace EverythingToolbar.Launcher
{
    internal class Utils
    {
        public enum WindowsTheme
        {
            Dark,
            Light
        }

        private static int buildNumber = -1;
        public static bool IsWindows11
        {
            get
            {
                if (buildNumber == -1)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    {
                        object currentBuildNumber = key?.GetValue("CurrentBuildNumber");
                        buildNumber = Convert.ToInt32(currentBuildNumber);
                    }
                }

                return buildNumber >= 22000;
            }
        }

        public static string GetTaskbarShortcutPath()
        {
            const string relativePath = @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\EverythingToolbar.lnk";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), relativePath);
        }

        public static bool IsTaskbarCenterAligned()
        {
            if (!IsWindows11)
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

            if (System.IO.File.Exists(taskbarShortcutPath))
                System.IO.File.Delete(taskbarShortcutPath);

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
