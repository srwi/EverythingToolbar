using System;
using System.IO;
using EverythingToolbar.Properties;

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
            if (Settings.Default.OSBuildNumberOverride != 0)
                return new Version(10, 0, Settings.Default.OSBuildNumberOverride);

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
            // Get absolute value
            var absolute = length < 0 ? -length : length;

            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (length >> 50);
            }
            else if (absolute >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (length >> 40);
            }
            else if (absolute >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (length >> 30);
            }
            else if (absolute >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (length >> 20);
            }
            else if (absolute >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (length >> 10);
            }
            else if (absolute >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = length;
            }
            else
            {
                return length.ToString("0 B"); // Byte
            }

            // Divide by 1024 to get fractional value
            readable /= 1024;

            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}
