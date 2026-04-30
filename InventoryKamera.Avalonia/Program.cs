using Avalonia;
using InventoryKamera.Configuration;
using NLog;
using System;
using System.IO;

namespace InventoryKamera.Avalonia
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main(string[] args)
        {
            // Ensure the settings directory exists before DI bootstrap so
            // JsonSettingsService can write settings.json on first run.
            var settingsDir = Path.GetDirectoryName(JsonSettingsService.DefaultPath());
            if (settingsDir != null) Directory.CreateDirectory(settingsDir);

            ConfigureLogging();
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Avalonia app crashed");
                throw;
            }
            LogManager.Shutdown();
        }

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static void ConfigureLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Layout shared by both file targets.
            const string layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff}|${level:uppercase=true}|${logger:shortName=True}|${message:withexception=true}";

            // Debug log: every Debug+ message, archived on startup so each run starts fresh.
            var debugFile = new NLog.Targets.FileTarget("debugfile")
            {
                Layout = layout,
                FileName = "./logging/InventoryKamera.Avalonia.debug.log",
                ArchiveFileName = "logging/archives/InventoryKamera.Avalonia.{####}.debug.log",
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveDateFormat = "yyyyMMddHHmmss",
                MaxArchiveFiles = 4,
                ConcurrentWrites = true,
                KeepFileOpen = true,
                ArchiveOldFileOnStartup = true,
                ArchiveFileKind = NLog.Targets.FilePathKind.Relative,
            };

            // Info+ log: same archive-on-startup behavior.
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                Layout = layout,
                FileName = "./logging/InventoryKamera.Avalonia.log",
                ArchiveFileName = "logging/archives/InventoryKamera.Avalonia.{####}.log",
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                ArchiveDateFormat = "yyyyMMddHHmmss",
                MaxArchiveFiles = 4,
                ConcurrentWrites = true,
                KeepFileOpen = true,
                ArchiveOldFileOnStartup = true,
                ArchiveFileKind = NLog.Targets.FilePathKind.Relative,
            };

            var logConsole = new NLog.Targets.ConsoleTarget("logconsole") { Layout = layout };

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, debugFile);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
            LogManager.Configuration = config;
        }
    }
}
