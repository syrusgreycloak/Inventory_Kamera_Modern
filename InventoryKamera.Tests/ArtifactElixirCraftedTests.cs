using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;
using SubStat = InventoryKamera.Artifact.SubStat;

namespace InventoryKamera.Tests
{
    /// <summary>GOOD v3 "elixirCrafted": artifacts crafted with a Sanctifying Elixir.</summary>
    public class ArtifactElixirCraftedTests
    {
        private static Artifact Goblet() => new Artifact("SilkenMoonsSerenade", 5, 20, "goblet", "eleMas",
            new List<SubStat> { new SubStat { stat = "critRate_", value = 6.2m } });

        [Fact]
        public void Json_ElixirCrafted_IsTrue()
        {
            var a = Goblet();
            a.ElixirCrafted = true;

            Assert.True((bool)JObject.FromObject(a)["elixirCrafted"]);
        }

        [Fact]
        public void Json_Regular_IsFalse()
        {
            Assert.False((bool)JObject.FromObject(Goblet())["elixirCrafted"]);
        }
    }
}
