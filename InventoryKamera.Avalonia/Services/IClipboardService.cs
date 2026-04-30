using System.Threading.Tasks;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Abstracts clipboard access so ViewModels can be tested without a live Avalonia window.
    /// </summary>
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
    }
}
