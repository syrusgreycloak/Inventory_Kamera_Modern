using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="SettingsViewModel"/>.
    ///
    /// All services are mocked so no Avalonia.Headless or file I/O is required.
    /// </summary>
    public class SettingsViewModelTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static (SettingsViewModel vm, Mock<ISettingsService> settings, Mock<IFolderPickerService> picker)
            MakeVm(ScanSettings initialSettings = null)
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(initialSettings ?? new ScanSettings());

            var picker = new Mock<IFolderPickerService>(MockBehavior.Loose);
            picker.Setup(p => p.PickFolderAsync(It.IsAny<string>()))
                  .ReturnsAsync((string)null);

            var vm = new SettingsViewModel(settings.Object, picker.Object);
            return (vm, settings, picker);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 1: Constructor null-guards
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_Throws_When_Settings_Null()
        {
            var picker = new Mock<IFolderPickerService>();
            Assert.Throws<ArgumentNullException>(() =>
                new SettingsViewModel(null, picker.Object));
        }

        [Fact]
        public void Constructor_Throws_When_FolderPicker_Null()
        {
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(s => s.Current).Returns(new ScanSettings());
            Assert.Throws<ArgumentNullException>(() =>
                new SettingsViewModel(settings.Object, null));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 2: OutputPath — read, write-through, save, PropertyChanged
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void OutputPath_Reads_From_Settings()
        {
            var initial = new ScanSettings { OutputPath = @"C:\MyOutput" };
            var (vm, _, _) = MakeVm(initial);

            Assert.Equal(@"C:\MyOutput", vm.OutputPath);
        }

        [Fact]
        public void OutputPath_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { OutputPath = "" };
            var (vm, settings, _) = MakeVm(s);

            vm.OutputPath = @"C:\NewOutput";

            Assert.Equal(@"C:\NewOutput", s.OutputPath);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void OutputPath_NoSave_When_Unchanged()
        {
            var s = new ScanSettings { OutputPath = @"C:\Same" };
            var (vm, settings, _) = MakeVm(s);

            vm.OutputPath = @"C:\Same"; // same value

            settings.Verify(x => x.Save(), Times.Never);
        }

        [Fact]
        public void OutputPath_Fires_PropertyChanged()
        {
            var (vm, _, _) = MakeVm(new ScanSettings { OutputPath = "" });
            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.OutputPath = @"C:\Changed";

            Assert.Contains("OutputPath", changed);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 3: MinimumWeaponRarity
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void MinimumWeaponRarity_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { MinimumWeaponRarity = 3 };
            var (vm, settings, _) = MakeVm(s);

            vm.MinimumWeaponRarity = 5;

            Assert.Equal(5, s.MinimumWeaponRarity);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 4: MinimumArtifactRarity
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void MinimumArtifactRarity_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { MinimumArtifactRarity = 4 };
            var (vm, settings, _) = MakeVm(s);

            vm.MinimumArtifactRarity = 5;

            Assert.Equal(5, s.MinimumArtifactRarity);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 5: EquipWeapons / EquipArtifacts toggles
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void EquipWeapons_WritesThrough_And_Fires_PropertyChanged()
        {
            var s = new ScanSettings { EquipWeapons = true };
            var (vm, settings, _) = MakeVm(s);

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.EquipWeapons = false;

            Assert.False(s.EquipWeapons);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
            Assert.Contains("EquipWeapons", changed);
        }

        [Fact]
        public void EquipArtifacts_WritesThrough_And_Fires_PropertyChanged()
        {
            var s = new ScanSettings { EquipArtifacts = true };
            var (vm, settings, _) = MakeVm(s);

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.EquipArtifacts = false;

            Assert.False(s.EquipArtifacts);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
            Assert.Contains("EquipArtifacts", changed);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 6: Key binding properties
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void InventoryKey_Stores_As_Int_And_Saves()
        {
            var s = new ScanSettings { InventoryKey = 66 };
            var (vm, settings, _) = MakeVm(s);

            vm.InventoryKey = 42;

            Assert.Equal(42, s.InventoryKey);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void CharacterKey_Stores_As_Int_And_Saves()
        {
            var s = new ScanSettings { CharacterKey = 67 };
            var (vm, settings, _) = MakeVm(s);

            vm.CharacterKey = 99;

            Assert.Equal(99, s.CharacterKey);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void Slot1Key_Stores_As_Int_And_Saves()
        {
            var s = new ScanSettings { Slot1Key = 1 };
            var (vm, settings, _) = MakeVm(s);

            vm.Slot1Key = 200;

            Assert.Equal(200, s.Slot1Key);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 7: ScannerDelay and TravelerName
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ScannerDelay_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { ScannerDelay = 0 };
            var (vm, settings, _) = MakeVm(s);

            vm.ScannerDelay = 500;

            Assert.Equal(500, s.ScannerDelay);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void TravelerName_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { TravelerName = "" };
            var (vm, settings, _) = MakeVm(s);

            vm.TravelerName = "Lumine";

            Assert.Equal("Lumine", s.TravelerName);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 8: LastUpdateCheck is read-only display property
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void LastUpdateCheck_Reflects_Settings_Value()
        {
            var expected = new DateTime(2025, 6, 15, 10, 30, 0);
            var s = new ScanSettings { LastUpdateCheck = expected };
            var (vm, _, _) = MakeVm(s);

            Assert.Equal(expected, vm.LastUpdateCheck);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 9: BrowseOutputPath command
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task BrowseOutputPath_Sets_OutputPath_When_Folder_Picked()
        {
            var s = new ScanSettings { OutputPath = "" };
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(x => x.Current).Returns(s);

            var picker = new Mock<IFolderPickerService>(MockBehavior.Loose);
            picker.Setup(p => p.PickFolderAsync(It.IsAny<string>()))
                  .ReturnsAsync(@"C:\PickedFolder");

            var vm = new SettingsViewModel(settings.Object, picker.Object);

            await vm.BrowseOutputPathCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\PickedFolder", vm.OutputPath);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task BrowseOutputPath_DoesNotChange_OutputPath_When_Cancelled()
        {
            var s = new ScanSettings { OutputPath = @"C:\Original" };
            var settings = new Mock<ISettingsService>(MockBehavior.Loose);
            settings.Setup(x => x.Current).Returns(s);

            var picker = new Mock<IFolderPickerService>(MockBehavior.Loose);
            picker.Setup(p => p.PickFolderAsync(It.IsAny<string>()))
                  .ReturnsAsync((string)null); // user cancelled

            var vm = new SettingsViewModel(settings.Object, picker.Object);

            await vm.BrowseOutputPathCommand.ExecuteAsync(null);

            Assert.Equal(@"C:\Original", vm.OutputPath);
            settings.Verify(x => x.Save(), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 10: MinimumWeaponLevel and MinimumArtifactLevel
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void MinimumWeaponLevel_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { MinimumWeaponLevel = 1 };
            var (vm, settings, _) = MakeVm(s);

            vm.MinimumWeaponLevel = 40;

            Assert.Equal(40, s.MinimumWeaponLevel);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        [Fact]
        public void MinimumArtifactLevel_WritesThrough_And_Saves()
        {
            var s = new ScanSettings { MinimumArtifactLevel = 0 };
            var (vm, settings, _) = MakeVm(s);

            vm.MinimumArtifactLevel = 12;

            Assert.Equal(12, s.MinimumArtifactLevel);
            settings.Verify(x => x.Save(), Times.AtLeastOnce);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 11: All properties are observable (fire PropertyChanged)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void AllSettingsProperties_Fire_PropertyChanged()
        {
            var s = new ScanSettings
            {
                MinimumWeaponRarity   = 3,
                MinimumWeaponLevel    = 1,
                MinimumArtifactRarity = 4,
                MinimumArtifactLevel  = 0,
                EquipWeapons          = true,
                EquipArtifacts        = true,
                InventoryKey          = 0,
                CharacterKey          = 0,
                Slot1Key              = 0,
                ScannerDelay          = 0,
                TravelerName          = "",
                OutputPath            = ""
            };
            var (vm, _, _) = MakeVm(s);

            var changed = new HashSet<string>();
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.MinimumWeaponRarity   = 5;
            vm.MinimumWeaponLevel    = 50;
            vm.MinimumArtifactRarity = 5;
            vm.MinimumArtifactLevel  = 10;
            vm.EquipWeapons          = false;
            vm.EquipArtifacts        = false;
            vm.InventoryKey          = 10;
            vm.CharacterKey          = 11;
            vm.Slot1Key              = 12;
            vm.ScannerDelay          = 200;
            vm.TravelerName          = "Aether";
            vm.OutputPath            = @"C:\out";

            Assert.Contains("MinimumWeaponRarity",   changed);
            Assert.Contains("MinimumWeaponLevel",    changed);
            Assert.Contains("MinimumArtifactRarity", changed);
            Assert.Contains("MinimumArtifactLevel",  changed);
            Assert.Contains("EquipWeapons",          changed);
            Assert.Contains("EquipArtifacts",        changed);
            Assert.Contains("InventoryKey",          changed);
            Assert.Contains("CharacterKey",          changed);
            Assert.Contains("Slot1Key",              changed);
            Assert.Contains("ScannerDelay",          changed);
            Assert.Contains("TravelerName",          changed);
            Assert.Contains("OutputPath",            changed);
        }

        // ---------------------------------------------------------------
        // Mannequin names and game executables
        // ---------------------------------------------------------------

        [Fact]
        public void MannequinName_OldPlaceholder_ShowsBlank()
        {
            var (vm, _, _) = MakeVm(new ScanSettings { Manequin1Name = "Manequin1", Manequin2Name = "Manekin Prime" });
            Assert.Equal("", vm.Mannequin1Name);
            Assert.Equal("Manekin Prime", vm.Mannequin2Name);
        }

        [Fact]
        public void MannequinName_Set_SavesSetting()
        {
            var current = new ScanSettings();
            var (vm, settings, _) = MakeVm(current);

            vm.Mannequin1Name = "Doll Face";

            Assert.Equal("Doll Face", current.Manequin1Name);
            settings.Verify(s => s.Save(), Times.Once);
        }

        [Theory]
        [InlineData("GenshinImpact, YuanShen", new[] { "GenshinImpact", "YuanShen" })]
        [InlineData("GenshinImpact.exe;Genshin Impact Cloud Game.exe", new[] { "GenshinImpact", "Genshin Impact Cloud Game" })]
        [InlineData(" GenshinImpact ,, genshinimpact ", new[] { "GenshinImpact" })]
        [InlineData("", new[] { "GenshinImpact", "YuanShen" })]   // blank restores the defaults
        public void ParseExecutables_SplitsAndCleans(string text, string[] expected)
        {
            Assert.Equal(expected, SettingsViewModel.ParseExecutables(text));
        }

        [Fact]
        public void Executables_Set_SavesList()
        {
            var current = new ScanSettings();
            var (vm, settings, _) = MakeVm(current);

            vm.Executables = "GenshinImpact, MyGenshin.exe";

            Assert.Equal(new[] { "GenshinImpact", "MyGenshin" }, current.Executables);
            Assert.Equal("GenshinImpact, MyGenshin", vm.Executables);
            settings.Verify(s => s.Save(), Times.Once);
        }
    }
}
