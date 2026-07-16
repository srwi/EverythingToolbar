using System;
using System.IO;
using EverythingToolbar.Helpers;
using Microsoft.Win32;
using NLog;

namespace EverythingToolbar.Services
{
    public sealed class AutostartService : IAutostartService
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryValueName = "EverythingToolbar";

        private static readonly ILogger Logger = ToolbarLogger.GetLogger<AutostartService>();

        public bool IsEnabled
        {
            get => GetAutostartState();
            set => SetAutostartState(value);
        }

        private static bool GetAutostartState()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var registryValue = key?.GetValue(RegistryValueName) as string;

            if (string.IsNullOrEmpty(registryValue))
                return false;

            return File.Exists(registryValue.Trim('"'));
        }

        private static void SetAutostartState(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                RegistryKeyPath,
                RegistryKeyPermissionCheck.ReadWriteSubTree
            );
            try
            {
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