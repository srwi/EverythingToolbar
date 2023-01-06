using Microsoft.Win32;
using System;
using System.IO;

namespace EverythingToolbar.Helpers
{
    class Utils
    {
        private static int buildNumber = -1;
        public static bool IsWindows11
        {
            get
            {
                if (buildNumber == -1)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    {
                        object currentBuildNumber = key?.GetValue("CurrentBuildNumber");
                        buildNumber = Convert.ToInt32(currentBuildNumber);
                    }
                }

                return buildNumber >= 22000;
            }
        }

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
            long absolute_i = length < 0 ? -length : length;

            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (length >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (length >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (length >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (length >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (length >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
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
