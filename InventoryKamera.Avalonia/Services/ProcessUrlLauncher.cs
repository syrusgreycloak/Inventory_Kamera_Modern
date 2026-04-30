using System.Diagnostics;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Production implementation of <see cref="IUrlLauncher"/> that uses
    /// <see cref="Process.Start"/> with <c>UseShellExecute = true</c>.
    /// Works for both URLs and folder paths on all platforms.
    /// </summary>
    public class ProcessUrlLauncher : IUrlLauncher
    {
        /// <inheritdoc />
        public void Open(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
