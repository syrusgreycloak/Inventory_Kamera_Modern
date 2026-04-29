using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;

namespace InventoryKamera.Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            Locator.SetServices(serviceProvider);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = serviceProvider.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // Register infrastructure services.
            // The Infrastructure project is referenced conditionally for Windows only (see .csproj).
            // On non-Windows platforms these registrations are skipped; chunk 9 adds platform stubs.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<IScreenCapture, InventoryKamera.Infrastructure.WindowsScreenCapture>();
                services.AddSingleton<IInputSimulator, InventoryKamera.Infrastructure.WindowsInputSimulator>();
                services.AddSingleton<IOcrEngine, InventoryKamera.Infrastructure.OcrEnginePool>();
                services.AddSingleton<IImageProcessor, InventoryKamera.Infrastructure.WinFormsImageProcessor>();
                // IGameDataService needs a GameDataService instance; construct it here.
                services.AddSingleton<IGameDataService>(sp =>
                {
                    var db = new DatabaseManager();
                    var core = new GameDataService(db);
                    return new InventoryKamera.Infrastructure.InfraGameDataService(core);
                });
            }

            // Register view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<ScanViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<ExportViewModel>();
            services.AddTransient<AboutViewModel>();
        }
    }
}
