using System;
using System.IO;
using System.Threading;
using InventoryKamera.Configuration;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for ISettingsService / JsonSettingsService.
    /// Each test gets a unique temp directory so they are fully isolated.
    /// </summary>
    public class SettingsServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public SettingsServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "InventoryKameraTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        private string SettingsFilePath => Path.Combine(_tempDir, "settings.json");

        private JsonSettingsService CreateService() =>
            new JsonSettingsService(() => SettingsFilePath);

        // -----------------------------------------------------------------------
        // Test 1: Defaults when file does not exist
        // -----------------------------------------------------------------------
        [Fact]
        public void Defaults_When_File_Does_Not_Exist()
        {
            using var svc = CreateService();

            var s = svc.Current;

            Assert.NotNull(s);
            Assert.True(s.ScanWeapons);
            Assert.True(s.ScanArtifacts);
            Assert.True(s.ScanCharacters);
            Assert.True(s.ScanMaterials);
            Assert.True(s.ScanCharDevItems);
            Assert.Equal(3, s.MinimumWeaponRarity);
            Assert.Equal(1, s.MinimumWeaponLevel);
            Assert.Equal(4, s.MinimumArtifactRarity);
            Assert.Equal(0, s.MinimumArtifactLevel);
            Assert.True(s.EquipWeapons);
            Assert.True(s.EquipArtifacts);
            Assert.Equal(0, s.ScannerDelay);
            Assert.Equal(string.Empty, s.OutputPath);
            Assert.Equal(66, s.InventoryKey);
            Assert.Equal(67, s.CharacterKey);
            Assert.Equal(1, s.Slot1Key);
            Assert.True(s.UpgradeNeeded);
            Assert.False(s.LogScreenshots);
        }

        // -----------------------------------------------------------------------
        // Test 2: Round-trip save then load
        // -----------------------------------------------------------------------
        [Fact]
        public void RoundTrip_Save_Then_Load()
        {
            const string expectedPath = @"C:\TestOutput\good_data.json";

            using (var svc = CreateService())
            {
                svc.Current.OutputPath = expectedPath;
                svc.Current.ScanWeapons = false;
                svc.Current.MinimumArtifactRarity = 5;
                svc.Save();
                // Dispose flushes pending debounced write synchronously.
            }

            // Load fresh service from the same path
            using var svc2 = CreateService();
            Assert.Equal(expectedPath, svc2.Current.OutputPath);
            Assert.False(svc2.Current.ScanWeapons);
            Assert.Equal(5, svc2.Current.MinimumArtifactRarity);
        }

        // -----------------------------------------------------------------------
        // Test 3: Debounce — rapid saves coalesce to a single file write
        // -----------------------------------------------------------------------
        [Fact]
        public void Save_Coalesces_Rapid_Calls()
        {
            // The SettingsChanged event fires exactly once per physical disk write.
            // By counting it we can verify that 3 rapid Save() calls produce only 1 write.
            using var svc = CreateService();

            int writeCount = 0;
            svc.SettingsChanged += (_, _) => Interlocked.Increment(ref writeCount);

            // Fire three saves in rapid succession (well within the 500ms debounce window).
            svc.Current.OutputPath = "first";
            svc.Save();
            svc.Current.OutputPath = "second";
            svc.Save();
            svc.Current.OutputPath = "third";
            svc.Save();

            // Dispose forces synchronous flush, waits for timer and event delivery.
            svc.Dispose();

            // Brief settle time for the event to propagate (it fires on the timer thread).
            Thread.Sleep(50);

            // Coalescing assertion: 3 rapid Save() calls → exactly 1 disk write.
            Assert.Equal(1, writeCount);

            // The file should exist with the last-set value.
            Assert.True(File.Exists(SettingsFilePath), "settings.json should have been created");

            using var svc2 = CreateService();
            Assert.Equal("third", svc2.Current.OutputPath);
        }

        // -----------------------------------------------------------------------
        // Test 4: SettingsChanged fires exactly once after save+dispose
        // -----------------------------------------------------------------------
        [Fact]
        public void SettingsChanged_Fires_On_Write()
        {
            using var svc = CreateService();

            int eventCount = 0;
            svc.SettingsChanged += (_, _) => Interlocked.Increment(ref eventCount);

            svc.Current.OutputPath = "changed_value";
            svc.Save();
            // Dispose flushes the debounce timer synchronously and ensures the event fires.

            svc.Dispose();

            // Give a brief moment for any async event delivery
            Thread.Sleep(50);

            Assert.Equal(1, eventCount);
        }

        // -----------------------------------------------------------------------
        // Test 5: Corrupt file falls back to defaults and renames original
        // -----------------------------------------------------------------------
        [Fact]
        public void Corrupt_File_Falls_Back_To_Defaults_And_Renames_Original()
        {
            // Write garbage JSON to the settings file location before constructing service
            File.WriteAllText(SettingsFilePath, "{ this is definitely not valid JSON !!! }}}");

            using var svc = CreateService();

            // Should fall back to defaults without throwing
            Assert.NotNull(svc.Current);
            Assert.True(svc.Current.ScanWeapons); // default value
            Assert.Equal(string.Empty, svc.Current.OutputPath); // default value

            // Original corrupt file should be renamed to settings.json.corrupt-<timestamp>
            var files = Directory.GetFiles(_tempDir, "settings.json.corrupt-*");
            Assert.True(files.Length >= 1,
                "Corrupt file should be renamed to settings.json.corrupt-<timestamp>");

            // The main settings.json should NOT exist yet (service hasn't saved new defaults)
            // or if it does, it should be valid JSON with defaults
            if (File.Exists(SettingsFilePath))
            {
                var content = File.ReadAllText(SettingsFilePath);
                // Should be valid JSON
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<ScanSettings>(content);
                Assert.NotNull(parsed);
            }
        }

        // -----------------------------------------------------------------------
        // Test 6: Reload re-reads from disk
        // -----------------------------------------------------------------------
        [Fact]
        public void Reload_Picks_Up_External_File_Change()
        {
            // Create service and save initial value
            using var svc = CreateService();
            svc.Current.OutputPath = "original";
            svc.Save();
            svc.FlushPendingSave(); // force synchronous write

            // Externally overwrite the file
            var newSettings = new ScanSettings { OutputPath = "external_change" };
            File.WriteAllText(SettingsFilePath,
                Newtonsoft.Json.JsonConvert.SerializeObject(newSettings, Newtonsoft.Json.Formatting.Indented));

            // Reload should pick up the external change
            svc.Reload();
            Assert.Equal("external_change", svc.Current.OutputPath);
        }

        // -----------------------------------------------------------------------
        // Test 7: DefaultPath returns well-formed AppData path
        // -----------------------------------------------------------------------
        [Fact]
        public void DefaultPath_Returns_AppData_InventoryKamera_SettingsJson()
        {
            var path = JsonSettingsService.DefaultPath();

            Assert.EndsWith("settings.json", path, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("InventoryKamera", path, StringComparison.OrdinalIgnoreCase);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Assert.StartsWith(appData, path, StringComparison.OrdinalIgnoreCase);
        }
    }
}
