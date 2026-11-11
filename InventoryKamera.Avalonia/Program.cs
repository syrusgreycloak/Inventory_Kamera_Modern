using Avalonia;
using InventoryKamera.Configuration;
using NLog;
using System;
using System.IO;
using System.Runtime.InteropServices;

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

            // Catch unhandled exceptions on background threads (worker pool, scraper threads,
            // etc.) and route them to NLog before the runtime tears the process down. Without
            // this, a worker-thread crash exits to desktop with an empty log.
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Fatal(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "unknown"),
                             "Unhandled exception (terminating={0})", e.IsTerminating);
                LogManager.Flush();
            };
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.Error(e.Exception, "Unobserved task exception");
                e.SetObserved();
            };

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                // Usually before any window exists: say so instead of vanishing without a trace
                Logger.Fatal(ex, "Avalonia app crashed");
                LogManager.Flush();
                ShowStartupError(ex);
                Environment.ExitCode = 1;
            }
            LogManager.Shutdown();
        }

        private static void ShowStartupError(Exception ex)
        {
            if (!OperatingSystem.IsWindows()) return;
            try { MessageBoxW(IntPtr.Zero, Services.StartupError.Message(ex), "Inventory Kamera Modern", 0x10 /* MB_ICONERROR */); }
            catch { /* nothing more we can do */ }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

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
