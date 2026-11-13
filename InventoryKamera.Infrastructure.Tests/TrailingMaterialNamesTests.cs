using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    /// <summary>
    /// The materials read after the last weapon or artifact are matched against the material
    /// list, so a name missing from materials.json can never be read (Sanctifying Essence and
    /// Unction weren't in it: the updater skipped their material type).
    /// </summary>
    public class TrailingMaterialNamesTests
    {
        private readonly GameDataService _gameData = new GameDataService(new DatabaseManager());

        public static TheoryData<string> Names()
        {
            var data = new TheoryData<string>();
            foreach (var n in WeaponScraper.EnhancementOres) data.Add(n);
            foreach (var n in ArtifactScraper.ArtifactExpItems) data.Add(n);
            return data;
        }

        [Theory]
        [MemberData(nameof(Names))]
        public void Name_IsInMaterialList(string name)
        {
            // ScanItemName gives lower-case text without spaces
            Assert.Equal(name, _gameData.FindClosestMaterialName(name.ToLowerInvariant(), minConfidence: 95));
        }
    }
}
