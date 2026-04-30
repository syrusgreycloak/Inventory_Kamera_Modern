using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryKamera;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ExportViewModel"/>.
    /// All services are mocked — no Avalonia.Headless or file I/O required.
    /// </summary>
    public class ExportViewModelTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Factory helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Mock<IScanRunner> MakeRunner(bool hasData = false)
        {
            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.HasData).Returns(hasData);
            runner.Setup(r => r.Characters).Returns(new List<Character>());
            runner.Setup(r => r.Inventory).Returns(new Inventory());
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<System.Threading.CancellationToken>()))
                  .Returns(Task.CompletedTask);
            return runner;
        }

        private static (
            ExportViewModel   vm,
            ScanViewModel     scanVm,
            Mock<ISettingsService>  settings,
            Mock<IClipboardService> clipboard,
            Mock<IUrlLauncher>      url,
            Mock<IDialogService>    dialog)
            MakeVm(bool hasScanData = false, string outputPath = @"C:\Output")
        {
            var scanSettings = new ScanSettings
            {
                OutputPath     = outputPath,
                EquipWeapons   = true,
                EquipArtifacts = true
            };

            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = MakeRunner(hasData: false);
            var scanVm = new ScanViewModel(settings.Object, runner.Object);

            var clipboard = new Mock<IClipboardService>(MockBehavior.Loose);
            clipboard.Setup(c => c.SetTextAsync(It.IsAny<string>()))
                     .Returns(Task.CompletedTask);

            var url = new Mock<IUrlLauncher>(MockBehavior.Loose);

            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            dialog.Setup(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);
            dialog.Setup(d => d.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);
            dialog.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .ReturnsAsync(false);

            var vm = new ExportViewModel(scanVm, settings.Object, clipboard.Object, url.Object, dialog.Object);

            // Simulate last-scan state by setting LastScan on the ScanViewModel.
            // [ObservableProperty] generates a public setter so no reflection needed.
            if (hasScanData)
            {
                var completedRunner = MakeRunner(hasData: true);
                scanVm.LastScan = completedRunner.Object;
            }

            return (vm, scanVm, settings, clipboard, url, dialog);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 1: Constructor null-guards
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_Throws_When_ScanVm_Null()
        {
            var settings  = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var clipboard = new Mock<IClipboardService>();
            var url       = new Mock<IUrlLauncher>();
            var dialog    = new Mock<IDialogService>();

            Assert.Throws<ArgumentNullException>(() =>
                new ExportViewModel(null, settings.Object, clipboard.Object, url.Object, dialog.Object));
        }

        [Fact]
        public void Constructor_Throws_When_Settings_Null()
        {
            var settings  = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var runner    = MakeRunner();
            var scanVm    = new ScanViewModel(settings.Object, runner.Object);
            var clipboard = new Mock<IClipboardService>();
            var url       = new Mock<IUrlLauncher>();
            var dialog    = new Mock<IDialogService>();

            Assert.Throws<ArgumentNullException>(() =>
                new ExportViewModel(scanVm, null, clipboard.Object, url.Object, dialog.Object));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 2: HasScanData / ScanStatusText initial state
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void HasScanData_False_When_No_Last_Scan()
        {
            var (vm, _, _, _, _, _) = MakeVm(hasScanData: false);
            Assert.False(vm.HasScanData);
        }

        [Fact]
        public void ScanStatusText_Shows_No_Data_When_Not_Scanned()
        {
            var (vm, _, _, _, _, _) = MakeVm(hasScanData: false);
            Assert.Contains("No data", vm.ScanStatusText);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 3: ExportGoodJson CanExecute reflects HasScanData
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ExportGoodJsonCommand_CannotExecute_When_No_Data()
        {
            var (vm, _, _, _, _, _) = MakeVm(hasScanData: false);
            Assert.False(vm.ExportGoodJsonCommand.CanExecute(null));
        }

        [Fact]
        public void UploadToOptimizerCommand_CannotExecute_When_No_Data()
        {
            var (vm, _, _, _, _, _) = MakeVm(hasScanData: false);
            Assert.False(vm.UploadToOptimizerCommand.CanExecute(null));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 4: OutputPath mirrors settings
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void OutputPath_Mirrors_Settings_Current()
        {
            var (vm, _, _, _, _, _) = MakeVm(outputPath: @"C:\MyFolder");
            Assert.Equal(@"C:\MyFolder", vm.OutputPath);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 5: OpenOutputFolder calls IUrlLauncher
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void OpenOutputFolder_Calls_UrlLauncher_With_OutputPath()
        {
            var (vm, _, _, _, url, _) = MakeVm(outputPath: @"C:\MyOutput");

            vm.OpenOutputFolderCommand.Execute(null);

            url.Verify(u => u.Open(@"C:\MyOutput"), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 6: HasScanData updates when ScanViewModel.LastScan changes
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void HasScanData_Updates_When_ScanVm_LastScan_Changes()
        {
            var (vm, scanVm, _, _, _, _) = MakeVm(hasScanData: false);

            Assert.False(vm.HasScanData);

            var propertyChanges = new List<string>();
            vm.PropertyChanged += (_, e) => propertyChanges.Add(e.PropertyName);

            // Simulate scan completing by setting LastScan on the ScanViewModel.
            var completedRunner = MakeRunner(hasData: true);
            scanVm.LastScan = completedRunner.Object;

            Assert.True(vm.HasScanData);
            Assert.Contains(nameof(ExportViewModel.HasScanData), propertyChanges);
        }
    }
}
