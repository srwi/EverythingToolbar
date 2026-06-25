using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace EverythingToolbar.Helpers
{
    public static class EverythingProcessHelper
    {
        private const string Dll64 = "Everything64.dll";

        [DllImport(Dll64, EntryPoint = "Everything_QueryW", CharSet = CharSet.Unicode)]
        private static extern bool Everything64_QueryW(bool wait);

        [DllImport(Dll64, EntryPoint = "Everything_IsDBLoaded")]
        private static extern bool Everything64_IsDBLoaded();

        [DllImport(Dll64, EntryPoint = "Everything_GetLastError")]
        private static extern uint Everything64_GetLastError();

        /// <summary>
        /// Checks if Everything is running and DB is loaded.
        /// </summary>
        public static bool IsRunning()
        {
            try
            {
                // Try a dummy query (empty string = no results)
                if (!Everything64_QueryW(false))
                    return false;

                return Everything64_IsDBLoaded();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ensures Everything is running.
        /// If not, starts Everything.exe in background with -startup.
        /// Waits until DB is loaded or timeout expires.
        /// </summary>
        public static bool EnsureRunning(int timeoutMs = 5000)
        {
            if (IsRunning())
            {
                return true;
            }

            string[] possiblePaths =
            {
               ToolbarSettings.User.EverythingPath,
               @"C:\Program Files\Everything\Everything.exe",
               @"C:\Program Files (x86)\Everything\Everything.exe",
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything\\Everything.exe"),
            };

            var everythingPath = possiblePaths.FirstOrDefault(p => File.Exists(p));
            if (everythingPath == null)
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = everythingPath,
                    Arguments = "-startup -first-instance",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    if (IsRunning())
                        return true;

                    Thread.Sleep(millisecondsTimeout: 10);
                }
            }
            catch (Exception)
            {
                // don't block. User can start Everything on their own as well.
            }

            return false;
        }
    }
}