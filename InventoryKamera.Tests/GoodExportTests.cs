using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace InventoryKamera.Tests
{
    public class GoodExportTests
    {
        private static Inventory BuildMinimalInventory()
        {
            var inv = new Inventory();

            // Weapon(name, level, ascended, refinement, locked, equippedCharacter, id, rarity)
            // ModelValidator defaults return true so IsValidWeapon("Absolution") passes even without wiring.
            inv.Add(new Weapon("Absolution", 1, false, 1, false, "", 0, 5));

            var materials = new HashSet<Material> { new Material("Mora", 100000) };
            inv.AddMaterials(ref materials);

            return inv;
        }

        [Fact]
        public void GOOD_Constructor_ProducesGoodFormat()
        {
            var inventory = BuildMinimalInventory();
            var characters = new List<Character>();

            var good = new GOOD(characters, inventory, equipWeapons: true, equipArtifacts: true);
            string json = good.ToString();

            Assert.False(string.IsNullOrWhiteSpace(json));
            var obj = JObject.Parse(json);
            Assert.Equal("GOOD", (string)obj["format"]);
            Assert.Equal(2, (int)obj["version"]);
            Assert.Equal("Inventory_Kamera", (string)obj["source"]);
        }

        [Fact]
        public void GOOD_Constructor_IncludesWeapons()
        {
            var inventory = BuildMinimalInventory();
            var characters = new List<Character>();

            var good = new GOOD(characters, inventory, equipWeapons: true, equipArtifacts: true);
            string json = good.ToString();

            var obj = JObject.Parse(json);
            var weapons = obj["weapons"] as JArray;

            // Weapons array should be present and contain our weapon
            Assert.NotNull(weapons);
            Assert.NotEmpty(weapons);
            Assert.Equal("Absolution", (string)weapons[0]["key"]);
        }

        [Fact]
        public void GOOD_Constructor_IncludesMaterials()
        {
            var inventory = BuildMinimalInventory();
            var characters = new List<Character>();

            var good = new GOOD(characters, inventory, equipWeapons: true, equipArtifacts: true);
            string json = good.ToString();

            var obj = JObject.Parse(json);
            var materials = obj["materials"] as JObject;

            Assert.NotNull(materials);
            Assert.Equal(100000, (int)materials["Mora"]);
        }

        [Fact]
        public void GOOD_Constructor_EmptyInventory_ProducesValidJson()
        {
            var inv = new Inventory();
            var characters = new List<Character>();

            var good = new GOOD(characters, inv, equipWeapons: false, equipArtifacts: false);
            string json = good.ToString();

            Assert.False(string.IsNullOrWhiteSpace(json));
            var obj = JObject.Parse(json);
            Assert.Equal("GOOD", (string)obj["format"]);
        }

        [Fact]
        public void GOOD_Constructor_EquipWeaponsFalse_ClearsWeaponLocation()
        {
            var weaponWithLocation = new Weapon("AquilaFavonia", 20, false, 1, false, "Albedo", 1, 5);
            Assert.Equal("Albedo", weaponWithLocation.EquippedCharacter); // sanity: starts with location

            var inventory = BuildMinimalInventory();
            inventory.Add(weaponWithLocation);
            var characters = new List<Character>();

            var good = new GOOD(characters, inventory, equipWeapons: false, equipArtifacts: false);

            // GOOD with equipWeapons=false mutates the weapon objects directly (shallow copy),
            // so we can assert on the object itself.
            Assert.Equal("", weaponWithLocation.EquippedCharacter);

            // Also verify through JSON output that the specific weapon has empty location.
            string json = good.ToString();
            var obj = JObject.Parse(json);
            var weapons = obj["weapons"] as JArray;

            Assert.NotNull(weapons);
            var aquila = weapons.FirstOrDefault(w => (string)w["key"] == "AquilaFavonia");
            Assert.NotNull(aquila); // weapon must be present in output
            Assert.Equal("", (string)aquila["location"]); // its location must be cleared
        }

        [Fact]
        public void GOOD_DefaultConstructor_ProducesEmptyFormat()
        {
            var good = new GOOD();
            string json = good.ToString();
            var obj = JObject.Parse(json);
            Assert.Equal("EMPTY", (string)obj["format"]);
        }
    }
}
