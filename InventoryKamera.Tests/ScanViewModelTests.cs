using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using InventoryKamera;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ScanViewModel"/>.
    ///
    /// NOTE: Tests that require Avalonia.Headless (e.g., Dispatcher.UIThread.RunJobs)
    /// are skipped here and documented as chunk-10 work.
    /// </summary>
    public class ScanViewModelTests
    {
        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        /// <summary>Creates a ScanViewModel with all services mocked.</summary>
        private static (ScanViewModel vm, Mock<IScanRunner> runner, Mock<ISettingsService> settings)
            MakeVm(Func<ScanSettings, CancellationToken, Task> scanBody = null)
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings
            {
                OutputPath     = "",
                EquipWeapons   = true,
                EquipArtifacts = true
            });

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            if (scanBody != null)
            {
                runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                      .Returns<ScanSettings, CancellationToken>((s, t) => scanBody(s, t));
            }
            else
            {
                runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);
            }

            var vm = new ScanViewModel(settings.Object, runner.Object);
            return (vm, runner, settings);
        }

        // ---------------------------------------------------------------
        // Test 1: IsScanning transitions
        // ---------------------------------------------------------------

        [Fact]
        public async Task StartScan_Sets_IsScanning_True_During_Scan()
        {
            var tcs = new TaskCompletionSource<bool>();

            var (vm, _, _) = MakeVm(async (_, token) =>
            {
                tcs.SetResult(true); // signal that scan has started
                await Task.Delay(500, token); // hold the scan open
            });

            Assert.False(vm.IsScanning, "IsScanning should be false before scan starts");
            Assert.True(vm.StartScanCommand.CanExecute(null), "Start should be executable before scan");

            var scanTask = vm.StartScanCommand.ExecuteAsync(null);

            // Wait until scan body signals it started
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(vm.IsScanning, "IsScanning should be true while scan is running");
            Assert.False(vm.StartScanCommand.CanExecute(null), "Start should be disabled while scanning");
            Assert.True(vm.StopScanCommand.CanExecute(null), "Stop should be enabled while scanning");

            // Stop it so the task can complete
            vm.StopScanCommand.Execute(null);
            await scanTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task StartScan_Resets_IsScanning_On_Completion()
        {
            var (vm, _, _) = MakeVm(); // instant scan

            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.False(vm.IsScanning, "IsScanning should be false after scan completes");
            Assert.True(vm.StartScanCommand.CanExecute(null), "Start should re-enable after scan completes");
        }

        // ---------------------------------------------------------------
        // Test 2: Cancellation
        // ---------------------------------------------------------------

        [Fact]
        public async Task Cancel_Sets_IsScanning_False_When_Token_Triggered()
        {
            var started = new ManualResetEventSlim(false);

            var (vm, _, _) = MakeVm(async (_, token) =>
            {
                started.Set();
                // Run until cancelled
                while (!token.IsCancellationRequested)
                    await Task.Delay(10);
                token.ThrowIfCancellationRequested();
            });

            var scanTask = vm.StartScanCommand.ExecuteAsync(null);
            started.Wait(TimeSpan.FromSeconds(5));

            vm.StopScanCommand.Execute(null);
            await scanTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(vm.IsScanning, "IsScanning should be false after cancellation");
        }

        // ---------------------------------------------------------------
        // Test 3: Counter observability
        // ---------------------------------------------------------------

        [Fact]
        public void Counter_Increments_Are_Observable()
        {
            var (vm, _, _) = MakeVm();

            var events = new List<string>();
            vm.PropertyChanged += (_, e) => events.Add(e.PropertyName);

            vm.WeaponsScanned = 5;
            vm.ArtifactsScanned = 10;
            vm.WeaponsTotal = 50;

            Assert.Equal(5, vm.WeaponsScanned);
            Assert.Equal(10, vm.ArtifactsScanned);
            Assert.Equal(50, vm.WeaponsTotal);

            Assert.Contains("WeaponsScanned", events);
            Assert.Contains("ArtifactsScanned", events);
            Assert.Contains("WeaponsTotal", events);
            Assert.Contains("WeaponsProgressText", events);
        }

        // ---------------------------------------------------------------
        // Test 4: Console log target
        // ---------------------------------------------------------------

        [Fact(Skip = "Requires Avalonia.Headless (Dispatcher.UIThread); scheduled for chunk 10")]
        public void Console_Grows_On_Log_Events()
        {
            // This test needs Avalonia.Headless to pump the UI thread dispatcher.
            // Marked Skip — implement in chunk 10 after Avalonia.Headless is configured.
        }

        // ---------------------------------------------------------------
        // Test 5: OCR preview properties
        // ---------------------------------------------------------------

        [Fact]
        public void OCR_Preview_Properties_Update_And_Fire_PropertyChanged()
        {
            var (vm, _, _) = MakeVm();

            var events = new List<string>();
            vm.PropertyChanged += (_, e) => events.Add(e.PropertyName);

            vm.CurrentItemName   = "Thundering Pulse";
            vm.CurrentItemStats  = new[] { "ATK: 608", "CRIT Rate: 33%" };
            vm.IsCurrentItemValid = true;
            vm.ValidationMessage  = "OK";
            vm.CurrentScreenshot  = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

            Assert.Equal("Thundering Pulse", vm.CurrentItemName);
            Assert.Equal(2, vm.CurrentItemStats.Length);
            Assert.True(vm.IsCurrentItemValid);
            Assert.Equal("OK", vm.ValidationMessage);
            Assert.NotEmpty(vm.CurrentScreenshot);

            Assert.Contains("CurrentItemName",    events);
            Assert.Contains("CurrentItemStats",   events);
            Assert.Contains("IsCurrentItemValid", events);
            Assert.Contains("ValidationMessage",  events);
            Assert.Contains("CurrentScreenshot",  events);
        }

        // ---------------------------------------------------------------
        // Test 6: ExportLast CanExecute tracks LastScan
        // ---------------------------------------------------------------

        [Fact]
        public void Export_CanExecute_False_When_LastScan_Null()
        {
            var (vm, _, _) = MakeVm();

            Assert.Null(vm.LastScan);
            Assert.False(vm.ExportLastCommand.CanExecute(null),
                "ExportLast should not be executable when LastScan is null");
        }

        [Fact]
        public async Task Export_CanExecute_True_When_LastScan_HasData()
        {
            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            runner.SetupGet(r => r.HasData).Returns(true);
            runner.SetupGet(r => r.Characters).Returns(new List<Character>());
            runner.SetupGet(r => r.Inventory).Returns(new Inventory());

            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings
            {
                OutputPath     = "C:/temp/output",
                EquipWeapons   = true,
                EquipArtifacts = true
            });

            var vm = new ScanViewModel(settings.Object, runner.Object);

            // Run scan so LastScan gets set
            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.NotNull(vm.LastScan);
            Assert.True(vm.ExportLastCommand.CanExecute(null),
                "ExportLast should be executable after scan with data");
        }

        // ---------------------------------------------------------------
        // Test 7: Progress text computed properties
        // ---------------------------------------------------------------

        [Fact]
        public void WeaponsProgressText_Reflects_Counters()
        {
            var (vm, _, _) = MakeVm();

            vm.WeaponsScanned = 7;
            vm.WeaponsTotal   = 42;

            Assert.Equal("7 / 42", vm.WeaponsProgressText);
        }

        [Fact]
        public void ArtifactsProgressText_Reflects_Counters()
        {
            var (vm, _, _) = MakeVm();

            vm.ArtifactsScanned = 100;
            vm.ArtifactsTotal   = 500;

            Assert.Equal("100 / 500", vm.ArtifactsProgressText);
        }

        // ---------------------------------------------------------------
        // Test 8: StopScan CanExecute when not scanning
        // ---------------------------------------------------------------

        [Fact]
        public void StopScan_CanExecute_False_When_Not_Scanning()
        {
            var (vm, _, _) = MakeVm();

            Assert.False(vm.StopScanCommand.CanExecute(null),
                "Stop should not be executable when not scanning");
        }

        // ---------------------------------------------------------------
        // Test 9: Console is initially empty
        // ---------------------------------------------------------------

        [Fact]
        public void Console_Is_Empty_On_Construction()
        {
            var (vm, _, _) = MakeVm();
            Assert.Empty(vm.Console);
        }

        // ---------------------------------------------------------------
        // Test 10: Reset counters on new scan
        // ---------------------------------------------------------------

        [Fact]
        public async Task Counters_Reset_At_Scan_Start()
        {
            var (vm, _, _) = MakeVm();

            // Manually set some counters
            vm.WeaponsScanned  = 99;
            vm.ArtifactsScanned = 55;

            // Run a quick scan
            await vm.StartScanCommand.ExecuteAsync(null);

            // After scan, counters should have been reset (to 0) at the start
            // We can't observe mid-scan state in a synchronous test, but we verify
            // they were set by the end (should still be 0 since mock doesn't increment).
            Assert.Equal(0, vm.WeaponsScanned);
            Assert.Equal(0, vm.ArtifactsScanned);
        }
    }
}
