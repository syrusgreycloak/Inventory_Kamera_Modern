using System;
using System.Threading.Tasks;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="AboutViewModel"/>.
    /// All services are mocked — no Avalonia.Headless, no network, no file I/O required.
    /// DatabaseManager is constructed from the test output directory which contains
    /// copied inventory JSON files (see InventoryKamera.Tests.csproj).
    /// </summary>
    public class AboutViewModelTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory helpers
        // ─────────────────────────────────────────────────────────────────────

        private static (
            AboutViewModel    vm,
            Mock<ISettingsService> settings,
            Mock<IUrlLauncher>     url,
            Mock<IDialogService>   dialog)
            MakeVm(ScanSettings initialSettings = null)
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(initialSettings ?? new ScanSettings());

            var url    = new Mock<IUrlLauncher>(MockBehavior.Loose);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            dialog.Setup(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);
            dialog.Setup(d => d.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);
            dialog.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(false);

            // Use the internal test constructor with a real DatabaseManager.
            // The test output directory has inventorylists/ copied by the .csproj.
            var db = new DatabaseManager();
            var vm = new AboutViewModel(settings.Object, url.Object, dialog.Object, db);
            return (vm, settings, url, dialog);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 1: Constructor null-guards
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_Throws_When_Settings_Null()
        {
            var url    = new Mock<IUrlLauncher>();
            var dialog = new Mock<IDialogService>();
            var db     = new DatabaseManager();
            Assert.Throws<ArgumentNullException>(() =>
                new AboutViewModel(null, url.Object, dialog.Object, db));
        }

        [Fact]
        public void Constructor_Throws_When_UrlLauncher_Null()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var dialog = new Mock<IDialogService>();
            var db     = new DatabaseManager();
            Assert.Throws<ArgumentNullException>(() =>
                new AboutViewModel(settings.Object, null, dialog.Object, db));
        }

        [Fact]
        public void Constructor_Throws_When_Dialog_Null()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var url = new Mock<IUrlLauncher>();
            var db  = new DatabaseManager();
            Assert.Throws<ArgumentNullException>(() =>
                new AboutViewModel(settings.Object, url.Object, null, db));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 2: Version property is not empty
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Version_Is_Not_Empty()
        {
            var (vm, _, _, _) = MakeVm();
            Assert.False(string.IsNullOrWhiteSpace(vm.Version));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 3: Link commands open correct URLs
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenGitHub_Opens_Correct_Url()
        {
            var (vm, _, url, _) = MakeVm();
            vm.OpenGitHubCommand.Execute(null);
            url.Verify(u => u.Open(It.Is<string>(s => s.Contains("Andrewthe13th") && s.Contains("Inventory_Kamera"))), Times.Once);
        }

        [Fact]
        public void OpenReleases_Opens_Releases_Url()
        {
            var (vm, _, url, _) = MakeVm();
            vm.OpenReleasesCommand.Execute(null);
            url.Verify(u => u.Open(It.Is<string>(s => s.Contains("releases"))), Times.Once);
        }

        [Fact]
        public void OpenIssues_Opens_Issues_Url()
        {
            var (vm, _, url, _) = MakeVm();
            vm.OpenIssuesCommand.Execute(null);
            url.Verify(u => u.Open(It.Is<string>(s => s.Contains("issues"))), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 4: LastUpdateCheck mirrors settings
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void LastUpdateCheck_Reflects_Settings()
        {
            var expected = new DateTime(2025, 1, 15, 12, 0, 0);
            var s = new ScanSettings { LastUpdateCheck = expected };
            var (vm, _, _, _) = MakeVm(s);
            Assert.Equal(expected, vm.LastUpdateCheck);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 5: CanExecute gating while updating
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void UpdateGameData_CanExecute_Initially_True()
        {
            var (vm, _, _, _) = MakeVm();
            Assert.True(vm.UpdateGameDataCommand.CanExecute(null));
        }

        [Fact]
        public void CheckAppUpdates_CanExecute_Initially_True()
        {
            var (vm, _, _, _) = MakeVm();
            Assert.True(vm.CheckAppUpdatesCommand.CanExecute(null));
        }
    }
}
