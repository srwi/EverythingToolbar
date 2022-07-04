using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace EverythingToolbar.Launcher
{
    internal class Utils
    {
        public enum IconType
        {
            Dark,
            Light
        }

        public static string GetTaskbarShortcutPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                @"\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\EverythingToolbar.lnk";
        }

        public static bool GetWindowsSearchEnabledState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search"))
            {
                object registryValueObject = key?.GetValue("SearchboxTaskbarMode");
                return registryValueObject != null && (int)registryValueObject > 0;
            }
        }

        public static void SetWindowsSearchEnabledState(bool enabled)
        {
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", enabled ? 1 : 0);
        }

        public static bool GetAutostartState()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                object registryValueObject = key?.GetValue("EverythingToolbar");
                return registryValueObject != null;
            }
        }

        public static void SetAutostartState(bool enabled)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (enabled)
                key.SetValue("EverythingToolbar", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
            else
                key.DeleteValue("EverythingToolbar", false);
        }

        public static IconType GetIconTypeBasedOnWindowsTheme()
        {
            bool systemUsesLightTheme;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object registryValueObject = key?.GetValue("SystemUsesLightTheme");
                systemUsesLightTheme = registryValueObject != null && (int)registryValueObject > 0;
            }

            return systemUsesLightTheme ? IconType.Light : IconType.Dark;
        }

        public static string GetIconPath(IconType iconType)
        {
            string processPath = Process.GetCurrentProcess().MainModule.FileName;
            return Directory.GetParent(processPath).FullName + "\\Icons\\" + iconType.ToString() + ".ico";
        }

        public static string GetThemedIconPath()
        {
            return GetIconPath(GetIconTypeBasedOnWindowsTheme());
        }

        public static void ChangeTaskbarPinIcon(IconType iconType)
        {
            string taskbarShortcutPath = GetTaskbarShortcutPath();

            const int maxTries = 1000;
            for (int i = 0; i < maxTries; i++)
            {
                try
                {
                    if (System.IO.File.Exists(taskbarShortcutPath))
                        System.IO.File.Delete(taskbarShortcutPath);

                    break;
                }
                catch (Exception e)
                {
                    if (e is IOException || e is UnauthorizedAccessException)
                        Thread.Sleep(100);
                    else
                        throw;
                }
            }

            if (System.IO.File.Exists(taskbarShortcutPath))
                throw new IOException("Could not access shortcut.");

            string targetPath = Process.GetCurrentProcess().MainModule.FileName;
            string iconPath = Utils.GetIconPath(iconType);

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(taskbarShortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.IconLocation = iconPath;
            shortcut.Save();
        }
    }
}
