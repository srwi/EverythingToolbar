using System;
using Microsoft.Win32;
using NLog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

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

        public static unsafe bool GetSystemAnimationsEnabled()
        {
            BOOL enabled = default;
            PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETCLIENTAREAANIMATION, 0, &enabled, 0);
            return enabled;
        }

        public static unsafe void SetSystemAnimationsEnabled(bool enabled)
        {
            PInvoke.SystemParametersInfo(
                SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETCLIENTAREAANIMATION,
                0,
                (void*)(enabled ? 1 : 0),
                SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDCHANGE
            );
        }
    }
}