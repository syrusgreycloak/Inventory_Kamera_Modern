namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Abstracts URL/folder launching so ViewModels can be tested without shell execution.
    /// </summary>
    public interface IUrlLauncher
    {
        /// <summary>Opens a URL or folder path using the OS default handler.</summary>
        void Open(string url);
    }
}
