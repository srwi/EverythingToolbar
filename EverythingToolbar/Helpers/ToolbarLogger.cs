using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace EverythingToolbar.Helpers
{
    public static class ToolbarLogger
    {
        private static readonly string DebugFlagFileName = Path.Combine(Utils.GetConfigDirectory(), "debug.txt");
        private static readonly LogFactory LogFactory = new LogFactory();

        public static ILogger GetLogger(string name)
        {
            return LogFactory.GetLogger(name);
        }

        public static ILogger GetLogger<T>()
        {
            return LogFactory.GetLogger(typeof(T).FullName);
        }

        private static LogLevel GetLogLevel()
        {
            return File.Exists(DebugFlagFileName) ? LogLevel.Debug : LogLevel.Info;
        }

        public static void Initialize()
        {
            var logfile = new FileTarget("logfile")
            {
                FileName = Path.Combine(Path.GetTempPath(), "EverythingToolbar.log"),
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveNumbering = ArchiveNumberingMode.Date,
                MaxArchiveFiles = 3,
                KeepFileOpen = true,
                OpenFileCacheTimeout = 30,
                ConcurrentWrites = true,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=tostring}"
            };
            var fileRule = new LoggingRule("*", GetLogLevel(), logfile);
            var config = new LoggingConfiguration();
            config.LoggingRules.Add(fileRule);
            LogFactory.Configuration = config;
            GetLogger("ToolbarLogger").Debug("Debug logging enabled.");
        }
    }
}
