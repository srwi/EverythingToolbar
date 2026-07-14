using System;
using Microsoft.Win32;

namespace EverythingToolbar.Helpers
{
    public sealed class WindowsPolicy(ISettings settings)
    {
        public Version GetWindowsVersion()
        {
            if (settings.ForceWin10Behavior)
                return Utils.WindowsVersion.Windows10Anniversary;

            return Environment.OSVersion.Version;
        }


        public bool IsEffectiveAnimationsDisabled =>
            settings.IsAnimationsDisabled || !SystemSettings.GetSystemAnimationsEnabled();

        public bool IsTaskbarWindowActive() =>
            settings.TaskbarWindowEnabled && GetWindowsVersion() >= Utils.WindowsVersion.Windows11;

        public bool IsTaskbarCenterAligned()
        {
            if (settings.IsForceCenterAlignment)
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