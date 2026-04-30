using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MessageBox.Avalonia.Enums;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Production implementation of <see cref="IDialogService"/> using MsBox.Avalonia.
    /// </summary>
    public class MsBoxDialogService : IDialogService
    {
        private static Window GetMainWindow() =>
            (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        /// <inheritdoc />
        public async Task<bool> ConfirmAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                title, message,
                ButtonEnum.YesNo,
                Icon.Question);

            var owner = GetMainWindow();
            var result = owner != null
                ? await box.ShowWindowDialogAsync(owner)
                : await box.ShowAsync();

            return result == ButtonResult.Yes;
        }

        /// <inheritdoc />
        public async Task ShowInfoAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                title, message,
                ButtonEnum.Ok,
                Icon.Info);

            var owner = GetMainWindow();
            if (owner != null)
                await box.ShowWindowDialogAsync(owner);
            else
                await box.ShowAsync();
        }

        /// <inheritdoc />
        public async Task ShowErrorAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                title, message,
                ButtonEnum.Ok,
                Icon.Error);

            var owner = GetMainWindow();
            if (owner != null)
                await box.ShowWindowDialogAsync(owner);
            else
                await box.ShowAsync();
        }
    }
}
