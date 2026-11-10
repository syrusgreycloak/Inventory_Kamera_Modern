using System.IO;
using System.Linq;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class ArtifactSubstatReaderTests
    {
        private static string Fmt(Artifact.SubStat s) =>
            s.stat + "=" + s.value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

        private static Image<Rgba32> Load(string file) =>
            Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Substats", file));

        /// <summary>
        /// Substat crops saved by a real 16:9 scan, with values from the standard exporter's GOOD v3
        /// export of the same artifacts. The first crop cuts off the "(unactivated)" text, so it
        /// can only be detected from the grey line colour.
        /// </summary>
        [Theory]
        [InlineData("unactivated_marker_cut_off.png", 5, 0, "critRate_=3.1;def_=6.6;atk=19", "enerRech_=5.2")]
        [InlineData("unactivated_level2_with_marker.png", 5, 2, "enerRech_=5.8;eleMas=16;hp=209", "def=21")]
        [InlineData("four_active_level0.png", 5, 0, "critDMG_=7;hp_=4.1;critRate_=3.5;atk_=4.7", "")]
        [InlineData("four_active_level20.png", 5, 20, "def_=12.4;hp=687;atk_=9.9;enerRech_=5.2", "")]
        public void Read_SavedCrop_SplitsActiveAndUnactivated(string file, int rarity, int level, string active, string unactivated)
        {
            using var crop = Load(file);

            var artifact = new Artifact("x", rarity, level, "flower", "hp", ArtifactSubstatReader.Read(crop));

            Assert.Equal(active, string.Join(";", artifact.SubStats.Select(s => Fmt(s))));
            Assert.Equal(unactivated, string.Join(";", (artifact.UnactivatedSubStats ?? new()).Select(s => Fmt(s))));
        }

        [Theory]
        [InlineData("unactivated_marker_cut_off.png", true)]
        [InlineData("unactivated_level2_with_marker.png", true)]
        [InlineData("four_active_level0.png", false)]
        [InlineData("four_active_level20.png", false)]
        public void IsLineGrey_FourthLine_MatchesUnactivated(string file, bool expected)
        {
            using var crop = Load(file);
            Assert.Equal(expected, ArtifactSubstatReader.IsLineGrey(crop, 3));
        }

        [Fact]
        public void ParseText_UnactivatedMarker_FlagsSubstatAndKeepsValue()
        {
            var subs = ArtifactSubstatReader.ParseText("· HP+209\n· DEF+21 (unactivated)\n", s => s);

            Assert.Equal(2, subs.Count);
            Assert.False(subs[0].unactivated);
            Assert.True(subs[1].unactivated);
            Assert.Equal("def", subs[1].stat);
            Assert.Equal(21m, subs[1].value);
        }

        [Fact]
        public void ParseText_StopsAtSetBonus_AndSkipsLinesWithoutDigits()
        {
            var subs = ArtifactSubstatReader.ParseText("garbage line\nATK+19\nSilken Moon's Serenade:\n2-Piece Set: +20%", s => s);

            var only = Assert.Single(subs);
            Assert.Equal("atk", only.stat);
            Assert.Equal(19m, only.value);
        }
    }
}
