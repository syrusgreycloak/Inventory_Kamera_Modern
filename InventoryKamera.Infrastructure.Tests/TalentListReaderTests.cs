using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class TalentListReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;

        public TalentListReaderTests(OcrEngineFixture fixture) => _fixture = fixture;

        // Test data keeps only the talent list of a 1920x1080 screenshot (x 1380-1710, y 140-500);
        // paste it back at its original position to rebuild a screenshot-sized image.
        private static Image<Rgba32> Screenshot(string file)
        {
            var canvas = new Image<Rgba32>(1920, 1080, new Rgba32(0, 0, 0, 255));
            using var list = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Talents", file));
            canvas.Mutate(c => c.DrawImage(list, new Point(1380, 140), 1f));
            return canvas;
        }

        /// <summary>
        /// Talents tabs from the game, one per element with and without constellation-boosted (cyan)
        /// levels. Expected levels are as shown, i.e. the export's levels plus 3 where boosted.
        /// </summary>
        [Theory]
        [InlineData("anemo_no_cyan.png", 1, 4, 4)]           // Vesna
        [InlineData("anemo_with_cyan.png", 1, 9, 8)]         // Jahoda C6
        [InlineData("cryo_no_cyan.png", 1, 5, 1)]            // Odette
        [InlineData("cryo_with_cyan.png", 5, 11, 11)]        // Qiqi C6, two-line burst name
        [InlineData("dendro_no_cyan.png", 6, 10, 10)]        // Nahida
        [InlineData("dendro_with_cyan.png", 1, 1, 4)]        // Tighnari C4
        [InlineData("electro_no_cyan.png", 7, 10, 10)]       // Raiden Shogun
        [InlineData("electro_with_cyan.png", 4, 10, 7)]      // Keqing C5
        [InlineData("geo_no_cyan.png", 8, 10, 10)]           // Zhongli
        [InlineData("geo_with_cyan.png", 6, 11, 10)]         // Noelle C6
        [InlineData("hydro_no_cyan.png", 1, 9, 6)]           // Columbina
        [InlineData("hydro_with_cyan.png", 5, 11, 10)]       // Xingqiu C6
        [InlineData("pyro_no_cyan.png", 6, 10, 10)]          // Mavuika
        [InlineData("pyro_with_cyan_dark.png", 7, 10, 10)]   // Diluc C6, dark background
        [InlineData("pyro_with_cyan_light.png", 6, 9, 10)]   // Bennett C6, light clouds behind the text
        // From a full scan, where a third (since removed) reading made these fall back to clicking
        [InlineData("scan_cryo_shenhe.png", 1, 1, 1)]
        [InlineData("scan_cryo_freminet.png", 4, 4, 1)]      // C6: auto and skill boosted
        [InlineData("scan_cryo_sandrone.png", 8, 7, 6)]
        [InlineData("scan_electro_dori.png", 1, 4, 4)]
        [InlineData("scan_pyro_arlecchino.png", 10, 10, 6)]
        public void ReadLevels_SavedScreen_ReturnsShownLevels(string file, int auto, int skill, int burst)
        {
            using var screenshot = Screenshot(file);
            Assert.Equal(new[] { auto, skill, burst }, TalentListReader.ReadLevels(screenshot, _fixture.Ocr));
        }

        /// <summary>
        /// Screens from a full scan where one level can't be read (a sparkle on Kazuha's skill
        /// "Lv.", a light patch of background behind Xinyan's auto level): must fall back, not guess.
        /// </summary>
        [Theory]
        [InlineData("scan_anemo_kazuha_sparkle.png")]
        [InlineData("scan_pyro_xinyan_light_patch.png")]
        public void ReadLevels_LevelObscured_IsInconclusive(string file)
        {
            using var screenshot = Screenshot(file);
            Assert.Null(TalentListReader.ReadLevels(screenshot, _fixture.Ocr));
        }

        [Fact]
        public void ReadLevels_LevelHidden_IsInconclusive()
        {
            // Burst level painted over: only two levels can be read, so fall back to clicking
            using var screenshot = Screenshot("geo_no_cyan.png");
            for (int y = 345; y < 385; y++)
                for (int x = 1580; x < 1710; x++)
                    screenshot[x, y] = new Rgba32(30, 30, 40, 255);

            Assert.Null(TalentListReader.ReadLevels(screenshot, _fixture.Ocr));
        }

        [Theory]
        [InlineData("Lv. 1 | Lv. 4 | Lv. 4", new[] { 1, 4, 4 })]
        [InlineData("Lv. 1 | Lv. 5 | Lv. J", new[] { 1, 5, 1 })]            // 1 read as J
        [InlineData("- Lv. I | $ - | Lv. 9 | Lv. 6.", new[] { 1, 9, 6 })]   // 1 read as I, noise lines
        [InlineData("Lv. 7 | Lv: 10 | Lv. 10", new[] { 7, 10, 10 })]       // "Lv:"
        [InlineData("Hidden Aces", new int[0])]
        public void Parse_OcrText_ReturnsLevels(string text, int[] expected)
        {
            Assert.Equal(expected, TalentListReader.Parse(text.Replace(" | ", "\n")));
        }
    }
}
