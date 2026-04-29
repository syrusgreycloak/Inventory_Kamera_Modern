using Avalonia;
using NLog;
using System;

namespace InventoryKamera.Avalonia
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static void Main(string[] args)
        {
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
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.fff}|${level:uppercase=true}|${logger:shortName=True}|${message:withexception=true}",
                FileName = "./logging/InventoryKamera.Avalonia.log",
                KeepFileOpen = true,
                ConcurrentWrites = true,
            };
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);
            LogManager.Configuration = config;
        }
    }
}
