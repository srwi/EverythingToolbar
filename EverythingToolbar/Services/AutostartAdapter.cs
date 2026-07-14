using System;
using System.IO;
using Microsoft.Win32;
using NLog;

namespace EverythingToolbar.Services
{
    public sealed class AutostartAdapter : IAutostart
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryValueName = "EverythingToolbar";

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<AutostartAdapter>();

        public bool IsEnabled
        {
            get => GetAutostartState();
            set => SetAutostartState(value);
        }

        private static bool GetAutostartState()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                var registryValue = key?.GetValue(RegistryValueName) as string;

                if (string.IsNullOrEmpty(registryValue))
                    return false;

                return File.Exists(registryValue.Trim('"'));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to read autostart state.");
                return false;
            }
        }

        private static void SetAutostartState(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    RegistryKeyPath,
                    RegistryKeyPermissionCheck.ReadWriteSubTree
                );

                if (enabled)
                {
                    var executablePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(executablePath))
                        key?.SetValue(RegistryValueName, "\"" + executablePath + "\"");
                }
                else
                {
                    key?.DeleteValue(RegistryValueName, false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to set autostart state.");
            }
        }
    }
}
