using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Avalonia.Views;
using InventoryKamera.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            // Register platform-specific infrastructure services.
            // WINDOWS is defined for the net8.0-windows TFM (see .csproj DefineConstants).
            // On the net8.0 TFM this block is compiled out; chunk 9 adds platform stubs.
#if WINDOWS
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
            services.AddSingleton<IUserInterface, InventoryKamera.Infrastructure.NullUserInterface>();
#endif

            // Settings service is cross-platform — registered outside any #if WINDOWS block.
            services.AddSingleton<ISettingsService>(sp => new JsonSettingsService(JsonSettingsService.DefaultPath));

            // Register view models (cross-platform — no Infrastructure dependency)
            services.AddTransient<MainViewModel>();
            services.AddTransient<ScanViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<ExportViewModel>();
            services.AddTransient<AboutViewModel>();
        }
    }
}
