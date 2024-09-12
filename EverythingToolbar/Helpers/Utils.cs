using System;
using System.IO;

namespace EverythingToolbar.Helpers
{
    public static class Utils
    {
        public static string GetConfigDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EverythingToolbar");
        }

        public static Version GetWindowsVersion()
        {
            if (ToolbarSettings.User.OsBuildNumberOverride != 0)
                return new Version(10, 0, ToolbarSettings.User.OsBuildNumberOverride);

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
    }
}
