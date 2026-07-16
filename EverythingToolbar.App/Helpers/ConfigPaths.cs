using System;
using System.IO;

namespace EverythingToolbar.App.Helpers
{
    public static class ConfigPaths
    {
        public static string GetConfigDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EverythingToolbar"
            );
        }
    }
}