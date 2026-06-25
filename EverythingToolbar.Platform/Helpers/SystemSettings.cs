using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NLog;

namespace EverythingToolbar.Helpers
{
    public static class SystemSettings
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

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

        private const int SpiGetclientareaanimation = 0x1042;
        private const int SpiSetclientareaanimation = 0x1043;
        private const int SpifSendchange = 0x0002;

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
    }
}