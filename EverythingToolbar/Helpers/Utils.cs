using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;
using Wpf.Ui.Appearance;

namespace EverythingToolbar.Helpers
{
    public static class Utils
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger(nameof(Utils));

        public static string GetConfigDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EverythingToolbar"
            );
        }

        public static bool GetWindowsSearchEnabledState()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Search");
            var searchboxTaskbarMode = key?.GetValue("SearchboxTaskbarMode");
            return searchboxTaskbarMode != null && (int)searchboxTaskbarMode > 0;
        }

        public static void SetWindowsSearchEnabledState(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Search",
                RegistryKeyPermissionCheck.ReadWriteSubTree
            );
            try
            {
                key?.SetValue("SearchboxTaskbarMode", enabled ? 1 : 0);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to set taskbar search icon mode.");
            }
        }

        public static Version GetWindowsVersion()
        {
            if (ToolbarSettings.User.ForceWin10Behavior)
                return WindowsVersion.Windows10Anniversary;

            return Environment.OSVersion.Version;
        }

        public static class WindowsVersion
        {
            public static readonly Version Windows10 = new Version(10, 0, 10240);
            public static readonly Version Windows10Anniversary = new Version(10, 0, 14393);
            public static readonly Version Windows11 = new Version(10, 0, 22000);
        }

        public static string GetHumanReadableFileSize(long length)
        {
            var absolute = length < 0 ? -length : length;

            string suffix;
            double readable;
            if (absolute >= 0x1000000000000000)
            {
                suffix = "EB";
                readable = length >> 50;
            }
            else if (absolute >= 0x4000000000000)
            {
                suffix = "PB";
                readable = length >> 40;
            }
            else if (absolute >= 0x10000000000)
            {
                suffix = "TB";
                readable = length >> 30;
            }
            else if (absolute >= 0x40000000)
            {
                suffix = "GB";
                readable = length >> 20;
            }
            else if (absolute >= 0x100000)
            {
                suffix = "MB";
                readable = length >> 10;
            }
            else if (absolute >= 0x400)
            {
                suffix = "KB";
                readable = length;
            }
            else
            {
                return length.ToString("0 B");
            }

            readable /= 1024;

            // Limit to 3 significant digits
            if (readable >= 100)
                return readable.ToString($"0 {suffix}");
            if (readable >= 10)
                return readable.ToString($"0.# {suffix}");
            else
                return readable.ToString($"0.## {suffix}");
        }

        public static bool IsWindowsTransparencyEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"
                );
                var value = key?.GetValue("EnableTransparency");
                return value is int intValue && intValue == 1;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsLightTheme()
        {
            if (ToolbarSettings.User.ThemeOverride.ToLower() == "light")
                return true;
            if (ToolbarSettings.User.ThemeOverride.ToLower() == "dark")
                return false;

            return SystemThemeManager.GetCachedSystemTheme() == SystemTheme.Light;
        }

        private const int SpiGetclientareaanimation = 0x1042;
        private const int SpiSetclientareaanimation = 0x1043;
        private const int SpifSendchange = 0x0002;

        public static bool IsEffectiveAnimationsDisabled
        {
            get
            {
                if (ToolbarSettings.User.IsAnimationsDisabled)
                    return true;

                return !GetSystemAnimationsEnabled();
            }
        }

        public static bool GetSystemAnimationsEnabled()
        {
            SystemParametersInfo(SpiGetclientareaanimation, 0, out var enabled, 0);
            return enabled;
        }

        public static void SetSystemAnimationsEnabled(bool enabled)
        {
            SystemParametersInfo(SpiSetclientareaanimation, 0, enabled, SpifSendchange);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, out bool pvParam, int fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(int uiAction, int uiParam, bool pvParam, int fWinIni);

        public static bool IsTaskbarCenterAligned()
        {
            if (ToolbarSettings.User.IsForceCenterAlignment)
                return true;

            if (GetWindowsVersion() < WindowsVersion.Windows11)
                return false;

            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
            );
            var taskbarAlignment = key?.GetValue("TaskbarAl");
            var leftAligned = taskbarAlignment != null && (int)taskbarAlignment == 0;
            return !leftAligned;
        }
    }
}
