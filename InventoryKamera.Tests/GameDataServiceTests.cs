using System.Collections.Generic;
using Xunit;

namespace InventoryKamera.Tests
{
    public class GameDataServiceTests
    {
        private readonly GameDataService _svc;

        public GameDataServiceTests()
        {
            // DatabaseManager reads from AppContext.BaseDirectory/inventorylists/
            // The .csproj copies reference_data/*.json to inventorylists/ in the output,
            // so this works in test runs.
            var db = new DatabaseManager();
            _svc = new GameDataService(db);
        }

        [Fact]
        public void FindClosestWeapon_ExactNormalizedKey_ReturnsGoodValue()
        {
            // "absolution" is a key in weapons.json -> value "Absolution"
            string result = _svc.FindClosestWeapon("absolution");
            Assert.Equal("Absolution", result);
        }

        [Fact]
        public void IsValidWeapon_KnownWeaponGoodKey_ReturnsTrue()
        {
            // Weapons dict contains value "Absolution" (GOOD format)
            Assert.True(_svc.IsValidWeapon("Absolution"));
        }

        [Fact]
        public void IsValidWeapon_UnknownName_ReturnsFalse()
        {
            Assert.False(_svc.IsValidWeapon("NotARealWeaponXYZ123"));
        }

        [Fact]
        public void IsValidCharacter_Traveler_ReturnsTrue()
        {
            // "TravelerAnemo" contains "Traveler" so always valid
            Assert.True(_svc.IsValidCharacter("TravelerAnemo"));
        }

        [Fact]
        public void IsValidCharacter_KnownCharacter_ReturnsTrue()
        {
            // "albedo" is a key in characters.json
            Assert.True(_svc.IsValidCharacter("albedo"));
        }

        [Fact]
        public void IsValidCharacter_UnknownCharacter_ReturnsFalse()
        {
            Assert.False(_svc.IsValidCharacter("NotACharacterXYZ999"));
        }

        [Fact]
        public void IsValidStat_CritRateValue_ReturnsTrue()
        {
            // Stats dict has value "critRate_"
            Assert.True(_svc.IsValidStat("critRate_"));
        }

        [Fact]
        public void IsValidStat_UnknownStat_ReturnsFalse()
        {
            Assert.False(_svc.IsValidStat("notAStat_XYZ"));
        }

        [Fact]
        public void IsValidSlot_Flower_ReturnsTrue()
        {
            Assert.True(_svc.IsValidSlot("flower"));
        }

        [Fact]
        public void IsValidSlot_AllSlots_ReturnTrue()
        {
            foreach (var slot in new[] { "flower", "plume", "sands", "goblet", "circlet" })
            {
                Assert.True(_svc.IsValidSlot(slot), $"Expected slot '{slot}' to be valid");
            }
        }

        [Fact]
        public void IsValidSlot_Unknown_ReturnsFalse()
        {
            Assert.False(_svc.IsValidSlot("helmet"));
        }

        [Fact]
        public void FindClosestGearSlot_FlowerInput_ReturnsFlower()
        {
            string result = _svc.FindClosestGearSlot("flower");
            Assert.Equal("flower", result);
        }

        [Fact]
        public void FindClosestGearSlot_PlumeInput_ReturnsPlume()
        {
            string result = _svc.FindClosestGearSlot("plume");
            Assert.Equal("plume", result);
        }

        [Fact]
        public void ReloadData_DoesNotThrow()
        {
            var ex = Record.Exception(() => _svc.ReloadData());
            Assert.Null(ex);
        }

        [Fact]
        public void GetCharactersElements_EmptyName_ReturnsEmptyList()
        {
            var result = _svc.GetCharactersElements("");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetCharactersElements_UnknownCharacter_ReturnsEmptyList()
        {
            var result = _svc.GetCharactersElements("notacharacterxyz");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetCharactersElements_Albedo_ReturnsGeoElement()
        {
            // albedo is geo element in characters.json
            var result = _svc.GetCharactersElements("albedo");
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains("geo", result);
        }

        [Fact]
        public void GearSlots_ContainsExpectedSlots()
        {
            Assert.Contains("flower", _svc.GearSlots);
            Assert.Contains("plume", _svc.GearSlots);
            Assert.Contains("sands", _svc.GearSlots);
            Assert.Contains("goblet", _svc.GearSlots);
            Assert.Contains("circlet", _svc.GearSlots);
        }
    }
}
