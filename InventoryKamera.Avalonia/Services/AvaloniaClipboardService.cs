using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Production implementation of <see cref="IClipboardService"/> that delegates to
    /// Avalonia's <see cref="Avalonia.Input.Platform.IClipboard"/>.
    /// </summary>
    public class AvaloniaClipboardService : IClipboardService
    {
        /// <inheritdoc />
        public async Task SetTextAsync(string text)
        {
            var mainWindow = (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow == null)
                return;

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel?.Clipboard == null)
                return;

            await topLevel.Clipboard.SetTextAsync(text);
        }
    }
}
