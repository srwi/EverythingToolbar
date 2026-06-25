using System;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace EverythingToolbar.Helpers
{
    public sealed class WindowsPolicy(ThemeOptions themeOptions, TaskbarWindowOptions taskbarWindowOptions)
    {
        public Version GetWindowsVersion()
        {
            if (themeOptions.ForceWin10Behavior)
                return Utils.WindowsVersion.Windows10Anniversary;

            return Environment.OSVersion.Version;
        }

        public bool IsLightTheme()
        {
            if (themeOptions.ThemeOverride.ToLower() == "light")
                return true;
            if (themeOptions.ThemeOverride.ToLower() == "dark")
                return false;

            return SystemThemeManager.GetCachedSystemTheme() == SystemTheme.Light;
        }

        public bool IsEffectiveAnimationsDisabled =>
            themeOptions.IsAnimationsDisabled || !SystemSettings.GetSystemAnimationsEnabled();

        public bool IsTaskbarWindowActive() =>
            taskbarWindowOptions.TaskbarWindowEnabled && GetWindowsVersion() >= Utils.WindowsVersion.Windows11;

        public bool IsTaskbarCenterAligned()
        {
            if (taskbarWindowOptions.IsForceCenterAlignment)
                return true;

            if (GetWindowsVersion() < Utils.WindowsVersion.Windows11)
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