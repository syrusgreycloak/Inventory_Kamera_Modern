using System;

namespace InventoryKamera.Avalonia
{
    /// <summary>
    /// Service locator — a small static wrapper around the DI container.
    /// Used primarily by XAML DataTemplates that need to resolve view models.
    /// Populated once by App.OnFrameworkInitializationCompleted before any window is shown.
    /// </summary>
    public static class Locator
    {
        private static IServiceProvider _services;

        /// <summary>Gets the application service provider.</summary>
        public static IServiceProvider Services => _services
            ?? throw new InvalidOperationException("Locator has not been initialized. Call Locator.SetServices() first.");

        /// <summary>Called once from App bootstrap to wire up the provider.</summary>
        internal static void SetServices(IServiceProvider provider)
        {
            _services = provider ?? throw new ArgumentNullException(nameof(provider));
        }
    }
}
