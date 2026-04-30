using System;
using Avalonia;
using Avalonia.Controls;
using InventoryKamera.Configuration;

namespace InventoryKamera.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        private ISettingsService _settings;

        public MainWindow()
        {
            InitializeComponent();

            // Resolve the settings service lazily after the DI container is built.
            // Locator.Services is set in App.OnFrameworkInitializationCompleted before
            // MainWindow is constructed, so this is safe.
            try
            {
                _settings = Locator.Services?.GetService(typeof(ISettingsService)) as ISettingsService;
            }
            catch
            {
                _settings = null;
            }

            ApplyPersistedWindowState();

            Closing += OnClosing;
        }

        private void ApplyPersistedWindowState()
        {
            if (_settings?.Current is not ScanSettings s) return;

            // Only apply if both dimensions look reasonable (avoids clamping to a 0×0 window).
            if (s.WindowWidth >= 400 && s.WindowHeight >= 300)
            {
                Width = s.WindowWidth;
                Height = s.WindowHeight;
            }

            if (!double.IsNaN(s.WindowX) && !double.IsNaN(s.WindowY))
            {
                // WindowStartupLocation must be Manual for Position to take effect.
                WindowStartupLocation = WindowStartupLocation.Manual;
                Position = new PixelPoint((int)s.WindowX, (int)s.WindowY);
            }

            if (s.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void OnClosing(object sender, EventArgs e)
        {
            if (_settings?.Current is not ScanSettings s) return;

            // Persist size only when not maximized so the next "restore" returns to the
            // user's preferred un-maximized size, not the maximized one.
            if (WindowState == WindowState.Normal)
            {
                s.WindowWidth = Width;
                s.WindowHeight = Height;
                s.WindowX = Position.X;
                s.WindowY = Position.Y;
            }
            s.WindowMaximized = WindowState == WindowState.Maximized;
            _settings.Save();
        }
    }
}
