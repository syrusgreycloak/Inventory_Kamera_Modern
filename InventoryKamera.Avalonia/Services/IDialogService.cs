using System.Threading.Tasks;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Abstracts modal dialogs so ViewModels can be tested without a live Avalonia window.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Shows a yes/no confirmation dialog. Returns <c>true</c> if the user clicks Yes.</summary>
        Task<bool> ConfirmAsync(string title, string message);

        /// <summary>Shows an informational message dialog.</summary>
        Task ShowInfoAsync(string title, string message);

        /// <summary>Shows an error message dialog.</summary>
        Task ShowErrorAsync(string title, string message);
    }
}
