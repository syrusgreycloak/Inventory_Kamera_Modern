using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using InventoryKamera;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Tests
{
    // ---------------------------------------------------------------------------
    // Avalonia headless session — shared fixture for all tests in the collection.
    //
    // HeadlessUnitTestSession starts a dedicated dispatcher thread. Every test
    // must run inside session.Dispatch() so they execute on the UI thread where
    // Dispatcher.UIThread.Post can flush synchronously.
    // ---------------------------------------------------------------------------

    public class AvaloniaHeadlessFixture : IDisposable
    {
        public readonly HeadlessUnitTestSession Session;

        public AvaloniaHeadlessFixture()
        {
            // HeadlessUnitTestSession.StartNew takes the entry-point type.
            // Passing Application (the Avalonia base) creates a minimal app
            // with no custom services — enough to get Dispatcher.UIThread working.
            Session = HeadlessUnitTestSession.StartNew(typeof(global::Avalonia.Application));
        }

        public void Dispose() => Session.Dispose();
    }

    [CollectionDefinition("AvaloniaHeadless")]
    public class AvaloniaHeadlessCollection : ICollectionFixture<AvaloniaHeadlessFixture> { }

    [Collection("AvaloniaHeadless")]
    public class AvaloniaUserInterfaceTests
    {
        private readonly HeadlessUnitTestSession _session;

        public AvaloniaUserInterfaceTests(AvaloniaHeadlessFixture fixture)
        {
            _session = fixture.Session;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static (ScanViewModel vm, AvaloniaUserInterface ui) Make()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            var vm = new ScanViewModel(settings.Object, runner.Object, dialog.Object);
            var ui = new AvaloniaUserInterface(vm);
            return (vm, ui);
        }

        private static Image<Rgba32> MakeImage(int w = 4, int h = 4)
            => new Image<Rgba32>(w, h);

        // Run action on UI thread and flush any pending posts before returning.
        private void Run(Action action)
        {
            _session.Dispatch(() =>
            {
                action();
                // Flush all pending UIThread.Post calls (Background priority or higher).
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None).GetAwaiter().GetResult();
        }

        // ------------------------------------------------------------------
        // Test 1 — SetWeapon_Max sets WeaponsTotal
        // ------------------------------------------------------------------

        [Fact]
        public void SetWeapon_Max_SetsWeaponsTotal()
        {
            ScanViewModel vm = null;
            AvaloniaUserInterface ui = null;

            Run(() =>
            {
                (vm, ui) = Make();
                ui.SetWeapon_Max(100);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(100, vm.WeaponsTotal);
        }

        // ------------------------------------------------------------------
        // Test 2 — SetArtifact_Max sets ArtifactsTotal
        // ------------------------------------------------------------------

        [Fact]
        public void SetArtifact_Max_SetsArtifactsTotal()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.SetArtifact_Max(250);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(250, vm.ArtifactsTotal);
        }

        // ------------------------------------------------------------------
        // Test 3 — IncrementWeaponCount accumulates correctly
        // ------------------------------------------------------------------

        [Fact]
        public void IncrementWeaponCount_AccumulatesAcrossMultipleCalls()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.IncrementWeaponCount();
                ui.IncrementWeaponCount();
                ui.IncrementWeaponCount();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(3, vm.WeaponsScanned);
        }

        // ------------------------------------------------------------------
        // Test 4 — IncrementArtifactCount accumulates
        // ------------------------------------------------------------------

        [Fact]
        public void IncrementArtifactCount_AccumulatesAcrossMultipleCalls()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.IncrementArtifactCount();
                ui.IncrementArtifactCount();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(2, vm.ArtifactsScanned);
        }

        // ------------------------------------------------------------------
        // Test 5 — IncrementCharacterCount accumulates
        // ------------------------------------------------------------------

        [Fact]
        public void IncrementCharacterCount_AccumulatesAcrossMultipleCalls()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.IncrementCharacterCount();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(1, vm.CharactersScanned);
        }

        // ------------------------------------------------------------------
        // Test 6 — SetProgramStatus sets both StatusMessage and StatusIsOk (ok=true)
        // ------------------------------------------------------------------

        [Fact]
        public void SetProgramStatus_SetsBothProperties_OkTrue()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.SetProgramStatus("Done", true);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("Done", vm.StatusMessage);
            Assert.True(vm.StatusIsOk);
        }

        // ------------------------------------------------------------------
        // Test 7 — SetProgramStatus sets ok=false
        // ------------------------------------------------------------------

        [Fact]
        public void SetProgramStatus_False_SetsIsOkFalse()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.SetProgramStatus("Error occurred", false);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("Error occurred", vm.StatusMessage);
            Assert.False(vm.StatusIsOk);
        }

        // ------------------------------------------------------------------
        // Test 8 — SetMainCharacterName sets MainCharacterName
        // ------------------------------------------------------------------

        [Fact]
        public void SetMainCharacterName_SetsProperty()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.SetMainCharacterName("Lumine");
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("Lumine", vm.MainCharacterName);
        }

        // ------------------------------------------------------------------
        // Test 9 — SetGearTextBox splits by newline into CurrentItemStats
        // ------------------------------------------------------------------

        [Fact]
        public void SetGearTextBox_SplitsTextIntoCurrentItemStats()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                ui.SetGearTextBox("ATK: 608\nCRIT Rate: 33%");
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(2, vm.CurrentItemStats.Length);
            Assert.Equal("ATK: 608", vm.CurrentItemStats[0]);
            Assert.Equal("CRIT Rate: 33%", vm.CurrentItemStats[1]);
        }

        // ------------------------------------------------------------------
        // Test 10 — SetCharacter_NameAndElement sets screenshot + label
        // ------------------------------------------------------------------

        [Fact]
        public void SetCharacter_NameAndElement_SetsScreenshotAndName()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                ui.SetCharacter_NameAndElement(img, "Lumine", "Anemo");
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("Lumine (Anemo)", vm.CurrentItemName);
            Assert.NotNull(vm.CurrentScreenshot);
            Assert.NotEmpty(vm.CurrentScreenshot);
        }

        // ------------------------------------------------------------------
        // Test 11 — SetCharacter_Level sets stats and screenshot
        // ------------------------------------------------------------------

        [Fact]
        public void SetCharacter_Level_SetsStatsAndScreenshot()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                ui.SetCharacter_Level(img, 80, 90);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.NotNull(vm.CurrentItemStats);
            Assert.Contains("Level 80 / 90", vm.CurrentItemStats[0]);
            Assert.NotNull(vm.CurrentScreenshot);
        }

        // ------------------------------------------------------------------
        // Test 12 — ResetCounters zeros all eight counters
        // ------------------------------------------------------------------

        [Fact]
        public void ResetCounters_ZerosAllCounters()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                vm.WeaponsScanned    = 10;
                vm.WeaponsTotal      = 20;
                vm.ArtifactsScanned  = 30;
                vm.ArtifactsTotal    = 40;
                vm.CharactersScanned = 5;
                vm.CharactersTotal   = 8;
                vm.MaterialsScanned  = 100;
                vm.MaterialsTotal    = 200;
                ui.ResetCounters();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(0, vm.WeaponsScanned);
            Assert.Equal(0, vm.WeaponsTotal);
            Assert.Equal(0, vm.ArtifactsScanned);
            Assert.Equal(0, vm.ArtifactsTotal);
            Assert.Equal(0, vm.CharactersScanned);
            Assert.Equal(0, vm.CharactersTotal);
            Assert.Equal(0, vm.MaterialsScanned);
            Assert.Equal(0, vm.MaterialsTotal);
        }

        // ------------------------------------------------------------------
        // Test 13 — ResetGearDisplay clears item display properties
        // ------------------------------------------------------------------

        [Fact]
        public void ResetGearDisplay_ClearsDisplayProperties()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                vm.CurrentItemName    = "Thundering Pulse";
                vm.CurrentItemStats   = new[] { "ATK: 608" };
                vm.IsCurrentItemValid = true;
                vm.CurrentScreenshot  = new byte[] { 1, 2, 3 };
                ui.ResetGearDisplay();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Null(vm.CurrentItemName);
            Assert.Null(vm.CurrentItemStats);
            Assert.Null(vm.IsCurrentItemValid);
            Assert.Null(vm.CurrentScreenshot);
        }

        // ------------------------------------------------------------------
        // Test 14 — Throttle: two rapid SetGearPictureBox calls within the
        //           100 ms window produce at most one screenshot update.
        // ------------------------------------------------------------------

        [Fact]
        public void SetGearPictureBox_Throttle_DropsSecondCallWithinWindow()
        {
            ScanViewModel vm = null;
            int snapshotCount = 0;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;

                // Subscribe before both calls
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ScanViewModel.CurrentScreenshot))
                        Interlocked.Increment(ref snapshotCount);
                };

                using var img1 = MakeImage();
                using var img2 = MakeImage();

                // Both calls are back-to-back — well inside the 100 ms throttle window.
                ui.SetGearPictureBox(img1);  // first: gate passes
                ui.SetGearPictureBox(img2);  // second: gate blocks (< 100 ms elapsed)

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            // Exactly one property-changed event for CurrentScreenshot was posted.
            Assert.Equal(1, snapshotCount);
            Assert.NotNull(vm.CurrentScreenshot);
        }

        // ------------------------------------------------------------------
        // Test 15 — SetNavigation_Image respects throttle (same as above)
        // ------------------------------------------------------------------

        [Fact]
        public void SetNavigation_Image_Throttle_DropsRapidUpdates()
        {
            int snapshotCount = 0;

            Run(() =>
            {
                var (vm, ui) = Make();

                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ScanViewModel.CurrentScreenshot))
                        Interlocked.Increment(ref snapshotCount);
                };

                using var img = MakeImage();
                ui.SetNavigation_Image(img);
                ui.SetNavigation_Image(img);  // throttled

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(1, snapshotCount);
        }

        // ------------------------------------------------------------------
        // Test 16 — ResetAll calls through to all sub-resets
        // ------------------------------------------------------------------

        [Fact]
        public void ResetAll_ClearsCountersAndDisplay()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                vm.WeaponsScanned = 5;
                vm.CurrentItemName = "Some Weapon";
                ui.ResetAll();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(0, vm.WeaponsScanned);
            Assert.Null(vm.CurrentItemName);
        }

        // ------------------------------------------------------------------
        // Test 17 — PropertyChanged fires for StatusMessage and StatusIsOk
        //           on SetProgramStatus.
        //
        //           StatusIsOk defaults to true, so we set ok=false first so
        //           a subsequent true→false change always fires.
        // ------------------------------------------------------------------

        [Fact]
        public void SetProgramStatus_FiresPropertyChanged()
        {
            var changedProps = new List<string>();

            Run(() =>
            {
                var (vm, ui) = Make();

                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName != null)
                        changedProps.Add(e.PropertyName);
                };

                // Force a status transition: true → false so both properties fire.
                ui.SetProgramStatus("Error", false);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Contains("StatusMessage", changedProps);
            Assert.Contains("StatusIsOk", changedProps);
        }

        // ------------------------------------------------------------------
        // Chunk-10 additions — the 8 IUserInterface methods not yet covered
        // ------------------------------------------------------------------

        // Test 18 — SetGear(weapon) sets screenshot, name, validity, stats
        [Fact]
        public void SetGear_Weapon_SetsDisplayProperties()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                // Weapon(name, level, ascended, refinementLevel, locked, equippedCharacter, id, _rarity)
                var weapon = new Weapon("AquilaFavonia", 90, true, 1, false, null, 0, 5);
                ui.SetGear(img, weapon);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("AquilaFavonia", vm.CurrentItemName);
            Assert.NotNull(vm.CurrentScreenshot);
            Assert.NotEmpty(vm.CurrentScreenshot);
        }

        // Test 19 — SetGear(artifact) sets screenshot and SetName
        [Fact]
        public void SetGear_Artifact_SetsDisplayProperties()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                // Use full constructor: (setName, rarity, level, gearSlot, mainStat, subStats)
                var artifact = new Artifact(
                    "GladiatorsFinale",
                    5,
                    20,
                    "flower",
                    "hp",
                    new System.Collections.Generic.List<Artifact.SubStat>());
                ui.SetGear(img, artifact);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal("GladiatorsFinale", vm.CurrentItemName);
            Assert.NotNull(vm.CurrentScreenshot);
        }

        // Test 20 — SetCharacter_Constellation logs (no VM change but no exception)
        [Fact]
        public void SetCharacter_Constellation_DoesNotThrow()
        {
            Run(() =>
            {
                var (_, ui) = Make();
                var ex = Record.Exception(() => ui.SetCharacter_Constellation(4));
                Assert.Null(ex);
            });
        }

        // Test 21 — SetCharacter_Talent respects throttle for screenshot
        [Fact]
        public void SetCharacter_Talent_SetsScreenshotOnFirstCall()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                ui.SetCharacter_Talent(img, "Normal Attack: Favonius Bladework", 1);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            // Throttle gate: first call should pass and set screenshot.
            Assert.NotNull(vm.CurrentScreenshot);
        }

        // Test 22 — SetMora logs and may set screenshot (first call passes throttle)
        [Fact]
        public void SetMora_SetsScreenshot()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                using var img = MakeImage();
                ui.SetMora(img, 12_000_000);
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.NotNull(vm.CurrentScreenshot);
        }

        // Test 23 — AddError does not throw and does not crash the VM
        [Fact]
        public void AddError_DoesNotThrow()
        {
            Run(() =>
            {
                var (_, ui) = Make();
                var ex = Record.Exception(() => ui.AddError("something went wrong"));
                Assert.Null(ex);
            });
        }

        // Test 24 — ResetCharacterDisplay clears item display (same as ResetGearDisplay)
        [Fact]
        public void ResetCharacterDisplay_ClearsDisplayProperties()
        {
            ScanViewModel vm = null;

            Run(() =>
            {
                var (v, ui) = Make();
                vm = v;
                vm.CurrentItemName    = "Hu Tao";
                vm.CurrentItemStats   = new[] { "Level 90 / 90" };
                vm.IsCurrentItemValid = true;
                vm.CurrentScreenshot  = new byte[] { 1, 2, 3 };
                ui.ResetCharacterDisplay();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Null(vm.CurrentItemName);
            Assert.Null(vm.CurrentItemStats);
            Assert.Null(vm.IsCurrentItemValid);
            Assert.Null(vm.CurrentScreenshot);
        }

        // Test 25 — ResetErrors is a no-op but must not throw
        [Fact]
        public void ResetErrors_DoesNotThrow()
        {
            Run(() =>
            {
                var (_, ui) = Make();
                var ex = Record.Exception(() => ui.ResetErrors());
                Assert.Null(ex);
            });
        }

        // Test 26 — Caller's Image<Rgba32> is NOT disposed by the adapter;
        //           it must still be usable after SetGearPictureBox returns.
        [Fact]
        public void SetGearPictureBox_DoesNotDispose_CallerImage()
        {
            bool imageStillUsable = false;

            Run(() =>
            {
                var (_, ui) = Make();
                var img = MakeImage(8, 8);

                ui.SetGearPictureBox(img);

                // Trying to access Width on a disposed ImageSharp image throws ObjectDisposedException.
                try
                {
                    var _ = img.Width; // This succeeds only if image is not disposed.
                    imageStillUsable = true;
                }
                catch (ObjectDisposedException)
                {
                    imageStillUsable = false;
                }
                finally
                {
                    img.Dispose();
                }
            });

            Assert.True(imageStillUsable, "Caller's Image<Rgba32> should not be disposed by AvaloniaUserInterface");
        }

        // Test 27 — 11 rapid SetGearPictureBox calls within 100 ms produce
        //           only ONE screenshot update (throttle gate).
        [Fact]
        public void SetGearPictureBox_Throttle_ManyRapidCalls_ProduceOneUpdate()
        {
            int snapshotCount = 0;

            Run(() =>
            {
                var (vm, ui) = Make();

                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ScanViewModel.CurrentScreenshot))
                        Interlocked.Increment(ref snapshotCount);
                };

                // 11 back-to-back calls — all within well under 100 ms.
                for (int i = 0; i < 11; i++)
                {
                    using var img = MakeImage();
                    ui.SetGearPictureBox(img);
                }

                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            });

            Assert.Equal(1, snapshotCount);
        }
    }
}
