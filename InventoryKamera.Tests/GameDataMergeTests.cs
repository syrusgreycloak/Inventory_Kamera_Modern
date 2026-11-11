using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// A forced "Update game data" rebuilds everything from the game data repository; these cases
    /// came from one that dropped a material, wrote "???" for a constellation name, and defaulted a
    /// constellation order it couldn't work out.
    /// </summary>
    public class GameDataMergeTests
    {
        [Fact]
        public void KeepMissing_EntryTheUpdateLacks_IsKept()
        {
            var updated = new Dictionary<string, string> { ["mora"] = "Mora" };
            var existing = new Dictionary<string, string> { ["mora"] = "Mora", ["vitalizeddragontooth"] = "VitalizedDragontooth" };

            var kept = GameDataMerge.KeepMissing(updated, existing);

            Assert.Equal(new[] { "vitalizeddragontooth" }, kept);
            Assert.Equal("VitalizedDragontooth", updated["vitalizeddragontooth"]);
        }

        [Fact]
        public void KeepMissing_UpdatedValue_Wins()
        {
            var updated = new Dictionary<string, string> { ["reliquaryoftruth"] = "ReliquaryOfTruth" };
            var existing = new Dictionary<string, string> { ["reliquaryoftruth"] = "ReliquaryofTruth" };

            Assert.Empty(GameDataMerge.KeepMissing(updated, existing));
            Assert.Equal("ReliquaryOfTruth", updated["reliquaryoftruth"]);
        }

        [Fact]
        public void MergeCharacters_PlaceholderName_KeepsExisting()
        {
            var updated = Characters(("columbina", "???", new JArray("burst", "skill")));
            var existing = Characters(("columbina", "Radiance Over Blossoms and Peaks", new JArray("burst", "skill")));

            var kept = GameDataMerge.MergeCharacters(updated, existing, new HashSet<string>());

            Assert.Equal("Radiance Over Blossoms and Peaks", (string)updated["columbina"]["ConstellationName"][0]);
            Assert.Contains("columbina constellation name", kept);
        }

        [Fact]
        public void MergeCharacters_UnresolvedOrder_KeepsExisting()
        {
            var updated = Characters(("aloy", "Aloy", new JArray("skill", "burst")));   // the default
            var existing = Characters(("aloy", "Aloy", new JArray("burst", "skill")));

            GameDataMerge.MergeCharacters(updated, existing, new HashSet<string> { "aloy" });

            Assert.Equal(new[] { "burst", "skill" }, updated["aloy"]["ConstellationOrder"].ToObject<string[]>());
        }

        [Fact]
        public void MergeCharacters_ResolvedOrder_UpdateWins()
        {
            // A corrected order from the game's own text must replace the old one
            var updated = Characters(("jahoda", "Jahoda", new JArray("burst", "skill")));
            var existing = Characters(("jahoda", "Jahoda", new JArray("skill", "burst")));

            GameDataMerge.MergeCharacters(updated, existing, new HashSet<string>());

            Assert.Equal(new[] { "burst", "skill" }, updated["jahoda"]["ConstellationOrder"].ToObject<string[]>());
        }

        [Fact]
        public void MergeCharacters_TravelerElementMissing_KeepsExistingElement()
        {
            var updated = new Dictionary<string, JObject>
            {
                ["traveler"] = new JObject { ["ConstellationOrder"] = new JObject { ["anemo"] = new JArray("burst", "skill") } },
            };
            var existing = new Dictionary<string, JObject>
            {
                ["traveler"] = new JObject
                {
                    ["ConstellationOrder"] = new JObject
                    {
                        ["anemo"] = new JArray("burst", "skill"),
                        ["cryo"] = new JArray("skill", "burst"),
                    },
                },
            };

            GameDataMerge.MergeCharacters(updated, existing, new HashSet<string>());

            Assert.Equal(new[] { "skill", "burst" }, updated["traveler"]["ConstellationOrder"]["cryo"].ToObject<string[]>());
        }

        [Theory]
        [InlineData("???", true)]
        [InlineData(" ?? ", true)]
        [InlineData("", true)]
        [InlineData("Princeps Cretaceus", false)]
        public void IsPlaceholder_DetectsPlaceholderText(string text, bool expected)
        {
            Assert.Equal(expected, GameDataMerge.IsPlaceholder(new JArray(text)));
        }

        private static Dictionary<string, JObject> Characters(params (string key, string constellation, JArray order)[] entries)
        {
            var result = new Dictionary<string, JObject>();
            foreach (var (key, constellation, order) in entries)
                result[key] = new JObject { ["ConstellationName"] = new JArray(constellation), ["ConstellationOrder"] = order };
            return result;
        }
    }
}
