using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Threading;
using InventoryKamera;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using NLog;
using NLog.Config;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="ScanViewModel"/>.
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

            var dialog = new Mock<IDialogService>(MockBehavior.Loose);

            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);
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
        // NOTE: This test runs in ScanViewModelHeadlessTests (see below)
        // which uses the shared AvaloniaHeadless collection fixture to avoid
        // Avalonia initialisation conflicts.
        // (Placeholder kept here for method-count parity with the plan.)
        // ---------------------------------------------------------------

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

            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

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

        // ---------------------------------------------------------------
        // Tests 11-17: Settings-bound observable properties
        // ---------------------------------------------------------------

        [Fact]
        public void ScanWeapons_Reads_From_Settings()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings { ScanWeapons = false };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            Assert.False(vm.ScanWeapons);
        }

        [Fact]
        public void ScanWeapons_Writes_Through_To_Settings_And_Saves()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings { ScanWeapons = true };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            vm.ScanWeapons = false;

            Assert.False(scanSettings.ScanWeapons);
            settings.Verify(s => s.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void ScanWeapons_No_Save_When_Value_Unchanged()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings { ScanWeapons = true };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            vm.ScanWeapons = true; // same value — should not save

            settings.Verify(s => s.Save(), Times.Never);
        }

        [Fact]
        public void ScanWeapons_Fires_PropertyChanged()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings { ScanWeapons = true };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            var changedProperties = new List<string>();
            vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

            vm.ScanWeapons = false;

            Assert.Contains("ScanWeapons", changedProperties);
        }

        [Fact]
        public void ScanSpeedIndex_Maps_To_ScannerDelay()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings { ScannerDelay = 0 };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            // ScannerDelay = 0 → Fast (index 0)
            Assert.Equal(0, vm.ScanSpeedIndex);

            // Set index 2 (Slow) → delay should be 500
            vm.ScanSpeedIndex = 2;
            Assert.Equal(500, scanSettings.ScannerDelay);
            settings.Verify(s => s.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void AllScanToggles_WriteThrough_And_FirePropertyChanged()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            var scanSettings = new ScanSettings
            {
                ScanArtifacts   = true,
                ScanCharacters  = true,
                ScanCharDevItems = true,
                ScanMaterials   = true
            };
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.ScanArtifacts    = false;
            vm.ScanCharacters   = false;
            vm.ScanCharDevItems = false;
            vm.ScanMaterials    = false;

            Assert.False(scanSettings.ScanArtifacts);
            Assert.False(scanSettings.ScanCharacters);
            Assert.False(scanSettings.ScanCharDevItems);
            Assert.False(scanSettings.ScanMaterials);

            Assert.Contains("ScanArtifacts",    changed);
            Assert.Contains("ScanCharacters",   changed);
            Assert.Contains("ScanCharDevItems", changed);
            Assert.Contains("ScanMaterials",    changed);
        }

        // ---------------------------------------------------------------
        // Tests 18-21 (chunk 10): new coverage gaps
        // ---------------------------------------------------------------

        /// <summary>
        /// CharactersProgressText and MaterialsProgressText are computed from
        /// their respective Scanned / Total counter pairs.
        /// </summary>
        [Fact]
        public void CharactersProgressText_Reflects_Counters()
        {
            var (vm, _, _) = MakeVm();

            vm.CharactersScanned = 3;
            vm.CharactersTotal   = 9;

            Assert.Equal("3 / 9", vm.CharactersProgressText);
        }

        [Fact]
        public void MaterialsProgressText_Reflects_Counters()
        {
            var (vm, _, _) = MakeVm();

            vm.MaterialsScanned = 50;
            vm.MaterialsTotal   = 200;

            Assert.Equal("50 / 200", vm.MaterialsProgressText);
        }

        [Fact]
        public void ProgressText_Shows_Zero_Zero_Initially()
        {
            var (vm, _, _) = MakeVm();

            Assert.Equal("0 / 0", vm.WeaponsProgressText);
            Assert.Equal("0 / 0", vm.ArtifactsProgressText);
            Assert.Equal("0", vm.CharactersProgressText);   // total unknown until a character scan completes
            Assert.Equal("0 / 0", vm.MaterialsProgressText);
        }

        /// <summary>
        /// Mutating ISettingsService.Current externally (e.g., from another
        /// ViewModel or thread) does NOT cause the ScanViewModel's hand-rolled
        /// property getters to fire PropertyChanged.  The VM does NOT subscribe
        /// to SettingsChanged — it reads Current on each access.
        /// </summary>
        [Fact]
        public void ExternalSettings_Mutation_Does_NOT_Fire_PropertyChanged()
        {
            var scanSettings = new ScanSettings { ScanWeapons = true };
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(scanSettings);

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            var changes = new List<string>();
            vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            // Mutate directly — bypassing the VM setter.
            scanSettings.ScanWeapons = false;

            // VM getter still reflects the new value because it reads Current each time...
            Assert.False(vm.ScanWeapons);
            // ...but no PropertyChanged was fired (no subscription to SettingsChanged).
            Assert.DoesNotContain("ScanWeapons", changes);
        }

        /// <summary>
        /// Two consecutive scans do not carry over LastScan from the first scan
        /// if the second scan runner reports no data.
        /// </summary>
        [Fact]
        public async Task Consecutive_Scans_Reset_LastScan_At_Start()
        {
            // First runner: has data.
            var runner1 = new Mock<IScanRunner>(MockBehavior.Loose);
            runner1.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
            runner1.SetupGet(r => r.HasData).Returns(true);
            runner1.SetupGet(r => r.Characters).Returns(new List<Character>());
            runner1.SetupGet(r => r.Inventory).Returns(new Inventory());

            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings
            {
                OutputPath     = "",   // no write attempted
                EquipWeapons   = true,
                EquipArtifacts = true
            });
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);

            var vm = new ScanViewModel(settings.Object, runner1.Object, dialog.Object);

            // First scan sets LastScan.
            await vm.StartScanCommand.ExecuteAsync(null);
            Assert.NotNull(vm.LastScan);

            // Second runner: no data.
            var runner2 = new Mock<IScanRunner>(MockBehavior.Loose);
            runner2.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);
            runner2.SetupGet(r => r.HasData).Returns(false);

            // Swap the runner by creating a new VM backed by runner2.
            var vm2 = new ScanViewModel(settings.Object, runner2.Object, dialog.Object);
            await vm2.StartScanCommand.ExecuteAsync(null);

            // runner2.HasData == false → LastScan should be null.
            Assert.Null(vm2.LastScan);
        }

        /// <summary>
        /// Calling StopScanCommand while the runner is executing signals the
        /// CancellationToken passed to RunAsync.
        /// </summary>
        [Fact]
        public async Task StopScan_Signals_CancellationToken()
        {
            var tokenWasCancelled = false;
            var runnerStarted = new TaskCompletionSource<bool>();

            var (vm, _, _) = MakeVm(async (_, token) =>
            {
                runnerStarted.SetResult(true);
                try
                {
                    await Task.Delay(10_000, token);
                }
                catch (OperationCanceledException)
                {
                    tokenWasCancelled = true;
                    throw;
                }
            });

            var scanTask = vm.StartScanCommand.ExecuteAsync(null);

            // Wait until the runner body has started.
            await runnerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Cancel from the outside.
            vm.StopScanCommand.Execute(null);
            await scanTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.True(tokenWasCancelled, "CancellationToken should have been cancelled");
            Assert.False(vm.IsScanning, "IsScanning should be false after cancellation");
        }

        // ---------------------------------------------------------------
        // Tests 23-27: coverage gap-fillers
        // ---------------------------------------------------------------

        /// <summary>
        /// Reading ScanArtifacts / ScanCharacters / ScanCharDevItems / ScanMaterials
        /// returns the value from the underlying ScanSettings.
        /// </summary>
        [Fact]
        public void ScanToggle_Getters_Return_Settings_Values()
        {
            var s = new ScanSettings
            {
                ScanArtifacts    = true,
                ScanCharacters   = false,
                ScanCharDevItems = true,
                ScanMaterials    = false
            };
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(x => x.Current).Returns(s);
            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            Assert.True(vm.ScanArtifacts);
            Assert.False(vm.ScanCharacters);
            Assert.True(vm.ScanCharDevItems);
            Assert.False(vm.ScanMaterials);
        }

        /// <summary>
        /// ScanSpeedIndex with a delay between 1 and 499 ms returns index 1 (Normal).
        /// </summary>
        [Fact]
        public void ScanSpeedIndex_Normal_Branch_Delay_100()
        {
            var s = new ScanSettings { ScannerDelay = 100 };
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(x => x.Current).Returns(s);
            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            Assert.Equal(1, vm.ScanSpeedIndex);
        }

        /// <summary>
        /// ExportLastCommand writes the GOOD JSON to the output path.
        /// </summary>
        [Fact]
        public void ExportLast_Writes_GOOD_JSON_To_OutputPath()
        {
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
            System.IO.Directory.CreateDirectory(tempDir);

            try
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
                    OutputPath     = tempDir,
                    EquipWeapons   = true,
                    EquipArtifacts = true
                });
                var dialog = new Mock<IDialogService>(MockBehavior.Loose);
                var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);
                vm.LastScan = runner.Object;

                vm.ExportLastCommand.Execute(null);

                var files = System.IO.Directory.GetFiles(tempDir, "genshinData_GOOD_*.json");
                Assert.NotEmpty(files);
            }
            finally
            {
                System.IO.Directory.Delete(tempDir, recursive: true);
            }
        }

        // ---------------------------------------------------------------
        // Test 26 (chunk 9): Platform warning shown on non-Windows
        // ---------------------------------------------------------------

        /// <summary>
        /// On non-Windows, StartScan should show the platform warning dialog
        /// and return without calling IScanRunner.RunAsync.
        /// This test is only meaningful when run on Linux/macOS; on Windows it
        /// verifies the happy-path (no dialog, scan proceeds).
        /// </summary>
        [Fact]
        public async Task StartScan_Shows_PlatformWarning_On_NonWindows()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());

            var runner = new Mock<IScanRunner>(MockBehavior.Strict); // Strict — RunAsync must NOT be called on non-Windows
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            dialog.Setup(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

            bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

            if (!isWindows)
            {
                // On non-Windows: dialog shown, runner NOT called, IsScanning stays false
                await vm.StartScanCommand.ExecuteAsync(null);

                dialog.Verify(
                    d => d.ShowInfoAsync("Scanning Unavailable", It.IsAny<string>()),
                    Times.Once);
                runner.VerifyNoOtherCalls();
                Assert.False(vm.IsScanning);
            }
            else
            {
                // On Windows: runner IS called (set it up for success), no dialog
                runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

                await vm.StartScanCommand.ExecuteAsync(null);

                dialog.Verify(
                    d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Never);
                runner.Verify(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        // ---------------------------------------------------------------
        // Character progress: the game doesn't show a total, so the last
        // completed scan's count is remembered in settings
        // ---------------------------------------------------------------

        [Fact]
        public void CharactersTotal_StartsAtLastCharacterCount()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings { LastCharacterCount = 94 });

            var vm = new ScanViewModel(settings.Object, new Mock<IScanRunner>().Object, new Mock<IDialogService>().Object);

            Assert.Equal("0 / 94", vm.CharactersProgressText);
        }

        [Fact]
        public async Task StartScan_UsesLastCharacterCountAsTotal()
        {
            int totalDuringScan = -1;
            ScanViewModel vmRef = null;
            var (vm, _, settings) = MakeVm((_, _) =>
            {
                totalDuringScan = vmRef.CharactersTotal;
                return Task.CompletedTask;
            });
            vmRef = vm;
            settings.Object.Current.LastCharacterCount = 94;

            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.Equal(94, totalDuringScan);
        }

        [Fact]
        public async Task CompletedScan_RemembersCharacterCount()
        {
            var (vm, runner, settings) = MakeVm();
            runner.Setup(r => r.Characters).Returns(new List<Character> { new Character(), new Character(), new Character() });

            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.Equal(3, settings.Object.Current.LastCharacterCount);
            Assert.Equal(3, vm.CharactersTotal);
            settings.Verify(s => s.Save(), Times.AtLeastOnce());
        }

        [Fact]
        public async Task CancelledScan_KeepsCharacterCount()
        {
            var (vm, runner, settings) = MakeVm((_, _) => throw new OperationCanceledException());
            runner.Setup(r => r.Characters).Returns(new List<Character> { new Character() });
            settings.Object.Current.LastCharacterCount = 94;

            await vm.StartScanCommand.ExecuteAsync(null);

            Assert.Equal(94, settings.Object.Current.LastCharacterCount);
        }

        [Fact]
        public void CharactersProgressText_UnknownTotal_ShowsCountOnly()
        {
            var (vm, _, _) = MakeVm();

            vm.CharactersScanned = 5;

            Assert.Equal("5", vm.CharactersProgressText);
        }

        [Fact]
        public void CharactersScanned_PastTotal_GrowsTotal()
        {
            // A character obtained since the last scan
            var (vm, _, _) = MakeVm();
            vm.CharactersTotal = 94;

            vm.CharactersScanned = 95;

            Assert.Equal(95, vm.CharactersTotal);
            Assert.Equal("95 / 95", vm.CharactersProgressText);
        }
    }

    // -----------------------------------------------------------------------
    // Headless tests for ScanViewModel that require Avalonia.UIThread.
    // Placed in their own class so they share the AvaloniaHeadless collection
    // fixture without conflicting with the non-headless ScanViewModelTests.
    // -----------------------------------------------------------------------

    [Collection("AvaloniaHeadless")]
    public class ScanViewModelHeadlessTests
    {
        private readonly HeadlessUnitTestSession _session;

        public ScanViewModelHeadlessTests(AvaloniaHeadlessFixture fixture)
        {
            _session = fixture.Session;
        }

        private void Run(Action action)
        {
            _session.Dispatch(() =>
            {
                action();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Verifies that the ObservableCollectionLogTarget wires correctly to
        /// ScanViewModel.Console: entries added via NLog logger show up in Console.
        ///
        /// The approach here builds an isolated NLog configuration with a dedicated
        /// rule, so the test is immune to global LogManager state from other tests.
        /// </summary>
        [Fact]
        public void Console_Grows_On_Log_Events()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var settings = new Mock<ISettingsService>(MockBehavior.Loose);
                settings.Setup(s => s.Current).Returns(new ScanSettings());
                var runner = new Mock<IScanRunner>(MockBehavior.Loose);
                var dialog = new Mock<IDialogService>(MockBehavior.Loose);
                vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);

                // Build an isolated configuration with a rule targeting ONLY the
                // VM's Console collection, then swap it in.
                var cfg = new NLog.Config.LoggingConfiguration();
                var target = new InventoryKamera.Avalonia.Services.ObservableCollectionLogTarget(vm.Console)
                {
                    Name = "IsolatedConsoleTarget_" + System.Guid.NewGuid()
                };
                cfg.AddTarget(target);
                cfg.AddRuleForAllLevels(target, "HeadlessTestLogger.*");
                LogManager.Configuration = cfg;
                LogManager.ReconfigExistingLoggers();

                // Obtain a logger that matches the rule pattern.
                var logger = LogManager.GetLogger("HeadlessTestLogger.Entry");
                logger.Info("Test console entry");

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.NotEmpty(vm.Console);
        }
    }
}
