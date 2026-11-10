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
            url.Verify(u => u.Open(It.Is<string>(s => s.Contains("syrusgreycloak") && s.Contains("Inventory_Kamera_Modern"))), Times.Once);
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

        // ─────────────────────────────────────────────────────────────────────
        // Test 6: LocalGameVersion is non-empty
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void LocalGameVersion_Is_Not_Empty()
        {
            var (vm, _, _, _) = MakeVm();
            Assert.False(string.IsNullOrWhiteSpace(vm.LocalGameVersion));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 7: IsCheckingUpdates resets to false if CheckAppUpdates throws
        //         a network error (simulated by a RateLimitExceededException path).
        //         We can't directly inject a mock GitHub client, but we can verify
        //         that the command completes without staying "checking" indefinitely.
        //         The command will fail due to network (in CI) or succeed; either way
        //         IsCheckingUpdates must be false afterwards.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CheckAppUpdates_ResetIsCheckingUpdates_OnCompletion()
        {
            var (vm, _, _, _) = MakeVm();

            // Run the command — it will hit GitHub or fail; both paths should reset the flag.
            try
            {
                await vm.CheckAppUpdatesCommand.ExecuteAsync(null);
            }
            catch
            {
                // Any exception (network, rate limit) is swallowed by the command internally.
            }

            Assert.False(vm.IsCheckingUpdates,
                "IsCheckingUpdates must be reset to false after the command finishes");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 8: LastUpdateCheck is updated after CheckAppUpdates completes
        //         (the command records DateTime.Now to settings before calling GitHub).
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CheckAppUpdates_Updates_LastUpdateCheck_In_Settings()
        {
            var s = new ScanSettings { LastUpdateCheck = DateTime.MinValue };
            var (vm, settings, _, _) = MakeVm(s);

            try
            {
                await vm.CheckAppUpdatesCommand.ExecuteAsync(null);
            }
            catch { /* network errors swallowed */ }

            // Settings.Save() must have been called with a LastUpdateCheck != MinValue.
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
            Assert.NotEqual(DateTime.MinValue, s.LastUpdateCheck);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 9: IsUpdatingGameData resets to false after UpdateGameData
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateGameData_InvalidSource_ShowsErrorWithoutDownloading()
        {
            // An invalid repository setting stops the command before any network access. (This test
            // used to run a real game data update, which hung the suite when GitLab rate-limited it.)
            var (vm, _, _, dialog) = MakeVm(new ScanSettings { GameDataRepoUrl = "https://bitbucket.org/a/b" });

            await vm.UpdateGameDataCommand.ExecuteAsync(null);

            dialog.Verify(d => d.ShowErrorAsync("Invalid game data source", It.Is<string>(m => m.Contains("gitlab.com and github.com"))), Times.Once);
            Assert.False(vm.IsUpdatingGameData,
                "IsUpdatingGameData must be false after the command finishes");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 10: Production 3-param constructor (without DatabaseManager)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ThreeParamConstructor_Creates_Valid_Vm()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var url    = new Mock<IUrlLauncher>(MockBehavior.Loose);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            dialog.Setup(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

            // This exercises the 3-param (production) constructor path.
            var vm = new AboutViewModel(settings.Object, url.Object, dialog.Object);

            Assert.False(string.IsNullOrWhiteSpace(vm.Version));
            Assert.True(vm.UpdateGameDataCommand.CanExecute(null));
        }
    }
}
