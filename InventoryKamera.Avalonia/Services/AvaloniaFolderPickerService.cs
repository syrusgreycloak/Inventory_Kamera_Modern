using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Production implementation of <see cref="IFolderPickerService"/> that delegates to
    /// Avalonia's <see cref="IStorageProvider"/> for a native folder-picker dialog.
    /// </summary>
    public class AvaloniaFolderPickerService : IFolderPickerService
    {
        /// <inheritdoc />
        public async Task<string> PickFolderAsync(string initialPath = null)
        {
            var mainWindow = (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow == null)
                return null;

            var topLevel = TopLevel.GetTopLevel(mainWindow);
            if (topLevel == null)
                return null;

            var options = new FolderPickerOpenOptions
            {
                Title = "Select output folder",
                AllowMultiple = false,
            };

            // Set initial location if the caller supplied a valid path
            if (!string.IsNullOrWhiteSpace(initialPath))
            {
                try
                {
                    var folder = await topLevel.StorageProvider
                        .TryGetFolderFromPathAsync(initialPath);
                    if (folder != null)
                        options.SuggestedStartLocation = folder;
                }
                catch
                {
                    // Ignore — bad path just means we don't pre-populate the dialog.
                }
            }

            var results = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

            if (results == null || results.Count == 0)
                return null;

            return results[0].TryGetLocalPath();
        }
    }
}
