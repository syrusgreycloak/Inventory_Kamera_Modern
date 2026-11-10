using System;
using System.Threading.Tasks;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    public class SettingsViewModelGameDataSourceTests
    {
        private static (SettingsViewModel vm, ScanSettings current, Mock<ISettingsService> settings) MakeVm(
            Func<GameDataSource, Version> getRemoteVersion)
        {
            var current = new ScanSettings();
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(current);
            var picker = new Mock<IFolderPickerService>(MockBehavior.Loose);
            return (new SettingsViewModel(settings.Object, picker.Object, getRemoteVersion), current, settings);
        }

        [Fact]
        public void Defaults_AreAnimeGameData2OnMain()
        {
            var (vm, _, _) = MakeVm(_ => null);
            Assert.Equal("https://gitlab.com/Dimbreath/animegamedata2", vm.GameDataRepoUrl);
            Assert.Equal("main", vm.GameDataBranch);
        }

        [Fact]
        public void SettingRepoUrl_SavesSettings()
        {
            var (vm, current, settings) = MakeVm(_ => null);
            vm.GameDataRepoUrl = "https://github.com/Owner/GameData";
            Assert.Equal("https://github.com/Owner/GameData", current.GameDataRepoUrl);
            settings.Verify(s => s.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void Reset_RestoresDefaults()
        {
            var (vm, current, _) = MakeVm(_ => null);
            vm.GameDataRepoUrl = "https://github.com/Owner/GameData";
            vm.GameDataBranch = "dev";

            vm.ResetGameDataSourceCommand.Execute(null);

            Assert.Equal(GameDataSource.DefaultRepoUrl, current.GameDataRepoUrl);
            Assert.Equal(GameDataSource.DefaultBranch, current.GameDataBranch);
        }

        [Fact]
        public async Task Test_ReachableRepo_ReportsVersionAndUsesParsedSource()
        {
            GameDataSource tested = null;
            var (vm, _, _) = MakeVm(s => { tested = s; return new Version(7, 2, 0); });
            vm.GameDataRepoUrl = "https://github.com/Owner/GameData/tree/main";
            vm.GameDataBranch = "";

            await vm.TestGameDataSourceCommand.ExecuteAsync(null);

            Assert.Equal(GameDataHost.GitHub, tested.Host);
            Assert.Equal("main", tested.Branch);
            Assert.Contains("7.2.0", vm.GameDataSourceStatus);
            Assert.StartsWith("OK", vm.GameDataSourceStatus);
        }

        [Fact]
        public async Task Test_Failure_ReportsError()
        {
            var (vm, _, _) = MakeVm(_ => throw new FormatException("404 Project Not Found"));

            await vm.TestGameDataSourceCommand.ExecuteAsync(null);

            Assert.Contains("404 Project Not Found", vm.GameDataSourceStatus);
            Assert.False(vm.IsTestingGameDataSource);
        }

        [Fact]
        public async Task Test_InvalidUrl_ReportsValidationErrorWithoutNetworkCall()
        {
            bool called = false;
            var (vm, _, _) = MakeVm(_ => { called = true; return null; });
            vm.GameDataRepoUrl = "https://bitbucket.org/a/b";

            await vm.TestGameDataSourceCommand.ExecuteAsync(null);

            Assert.False(called);
            Assert.StartsWith("Only gitlab.com and github.com", vm.GameDataSourceStatus);
        }
    }
}
