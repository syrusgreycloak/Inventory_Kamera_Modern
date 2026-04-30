using System.Threading.Tasks;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Abstracts the platform folder-picker dialog so <see cref="ViewModels.SettingsViewModel"/>
    /// can be tested without a live Avalonia window.
    /// </summary>
    public interface IFolderPickerService
    {
        /// <summary>
        /// Opens a folder-picker dialog and returns the selected local path,
        /// or <c>null</c> if the user cancels.
        /// </summary>
        /// <param name="initialPath">
        /// Optional starting directory shown when the dialog opens.
        /// Pass <c>null</c> to let the OS choose.
        /// </param>
        Task<string> PickFolderAsync(string initialPath = null);
    }
}
