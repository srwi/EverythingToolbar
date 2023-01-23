using System;
using System.IO;

namespace EverythingToolbar.Helpers
{
    public class Utils
    {
        private Utils() { }

        public static class WindowsVersion
        {
            public static Version Windows10 = new Version(10, 0, 10240);
            public static Version Windows10Anniversary = new Version(10, 0, 14393);
            public static Version Windows11 = new Version(10, 0, 22000);
        }

        // Property can be removed once start menu replacement is implemented in Windows 11
        public static bool IsWindows11 => Environment.OSVersion.Version >= WindowsVersion.Windows11;

        public static string GetHumanReadableFileSize(string path)
        {
            long length;
            try
            {
                length = new FileInfo(path).Length;
            }
            catch
            {
                return "";
            }

            // Get absolute value
            long absolute = length < 0 ? -length : length;

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
