using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;
using SubStat = InventoryKamera.Artifact.SubStat;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// GOOD v3 "unactivatedSubstats": a 5-star artifact below +4 can show a greyed-out 4th substat
    /// that gives no stats until +4. The scraper flags it; Artifact decides whether the flag stands.
    /// </summary>
    public class ArtifactUnactivatedSubstatTests
    {
        private static List<SubStat> Subs(bool fourthUnactivated) => new List<SubStat>
        {
            new SubStat { stat = "enerRech_", value = 5.8m },
            new SubStat { stat = "eleMas", value = 16m },
            new SubStat { stat = "hp", value = 209m },
            new SubStat { stat = "def", value = 21m, unactivated = fourthUnactivated },
        };

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public void FiveStarBelowPlus4_FlaggedSubstat_IsUnactivated(int level)
        {
            var a = new Artifact("GladiatorsFinale", 5, level, "plume", "atk", Subs(true));

            Assert.Equal(new[] { "enerRech_", "eleMas", "hp" }, a.SubStats.Select(s => s.stat));
            var un = Assert.Single(a.UnactivatedSubStats);
            Assert.Equal("def", un.stat);
            Assert.Equal(21m, un.value);
        }

        [Theory]
        [InlineData(5, 4)]  // activates at +4
        [InlineData(5, 20)]
        [InlineData(4, 0)]  // only 5-star artifacts have unactivated substats
        public void FlagOutsideGameRule_IsIgnored_StatStaysActive(int rarity, int level)
        {
            var a = new Artifact("GladiatorsFinale", rarity, level, "plume", "atk", Subs(true));

            Assert.Equal(4, a.SubStats.Count);
            Assert.Null(a.UnactivatedSubStats);
        }

        [Fact]
        public void NoFlag_AllSubstatsActive()
        {
            var a = new Artifact("GladiatorsFinale", 5, 0, "plume", "atk", Subs(false));

            Assert.Equal(4, a.SubStats.Count);
            Assert.Null(a.UnactivatedSubStats);
        }

        [Fact]
        public void Json_UnactivatedSubstat_SerializesAsGoodV3()
        {
            var a = JObject.FromObject(new Artifact("GladiatorsFinale", 5, 2, "plume", "atk", Subs(true)));

            Assert.Equal(3, ((JArray)a["substats"]).Count);
            var un = Assert.Single((JArray)a["unactivatedSubstats"]);
            Assert.Equal("def", (string)un["key"]);
            Assert.Equal(21m, (decimal)un["value"]);
            Assert.Null(un["unactivated"]); // internal flag is not exported
        }

        [Fact]
        public void Json_NoUnactivatedSubstat_OmitsProperty()
        {
            var a = JObject.FromObject(new Artifact("GladiatorsFinale", 5, 20, "plume", "atk", Subs(false)));

            Assert.Null(a["unactivatedSubstats"]);
        }
    }
}
