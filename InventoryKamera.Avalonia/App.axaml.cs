using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using InventoryKamera;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Avalonia.Views;
using InventoryKamera.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
#if WINDOWS
using InventoryKamera.Infrastructure;
#else
using InventoryKamera.Avalonia.Services.PlatformStubs;
#endif

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
            // On the net8.0 TFM (Linux/macOS) platform stubs are registered instead.
#if WINDOWS
            services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
            services.AddSingleton<IInputSimulator, WindowsInputSimulator>();
            services.AddSingleton<IOcrEngine, OcrEnginePool>();
            services.AddSingleton<IImageProcessor, WinFormsImageProcessor>();
            // IGameDataService needs a GameDataService instance; construct it here.
            services.AddSingleton<IGameDataService>(sp =>
            {
                var db = new DatabaseManager();
                var core = new GameDataService(db);
                return new InfraGameDataService(core);
            });
            services.AddSingleton<IUserInterface, AvaloniaUserInterface>();
            // ScanProfile: profile path and aspect ratio resolved at scan-start time.
            services.AddSingleton<IScanProfileService>(sp => new ScanProfileManager());
            // IScanRunner: orchestrates the full inventory scan using injected services.
            // IUserInterface is resolved via Func<> to break a DI cycle:
            // ScanViewModel -> IScanRunner -> IUserInterface (AvaloniaUserInterface) -> ScanViewModel.
            services.AddSingleton<IScanRunner>(sp => new WindowsScanRunner(
                sp.GetRequiredService<IScreenCapture>(),
                sp.GetRequiredService<IInputSimulator>(),
                sp.GetRequiredService<IOcrEngine>(),
                sp.GetRequiredService<IImageProcessor>(),
                () => sp.GetRequiredService<IUserInterface>(),
                sp.GetRequiredService<IGameDataService>(),
                sp.GetRequiredService<IScanProfileService>()));
#else
            // Non-Windows: register throw-on-use stubs so DI resolves on all platforms.
            // ScanViewModel shows a platform warning dialog before any stub is ever called.
            services.AddSingleton<IScreenCapture, StubScreenCapture>();
            services.AddSingleton<IInputSimulator, StubInputSimulator>();
            services.AddSingleton<IOcrEngine, StubOcrEngine>();
            services.AddSingleton<IImageProcessor, StubImageProcessor>();
            services.AddSingleton<IGameDataService, StubGameDataService>();
            services.AddSingleton<IUserInterface, NullUserInterface>();
            services.AddSingleton<IScanRunner, NullScanRunner>();
#endif

            // Settings service is cross-platform — registered outside any #if WINDOWS block.
            services.AddSingleton<ISettingsService>(sp => new JsonSettingsService(JsonSettingsService.DefaultPath));

            // Folder picker — wraps Avalonia StorageProvider; cross-platform.
            services.AddSingleton<IFolderPickerService, AvaloniaFolderPickerService>();

            // Clipboard, URL launcher, and dialog service — cross-platform abstractions.
            services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
            services.AddSingleton<IUrlLauncher, ProcessUrlLauncher>();
            services.AddSingleton<IDialogService, MsBoxDialogService>();

            // Register view models (cross-platform — no Infrastructure dependency)
            services.AddTransient<MainViewModel>();
            services.AddSingleton<ScanViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<ExportViewModel>();
            services.AddTransient<AboutViewModel>();
        }
    }
}
