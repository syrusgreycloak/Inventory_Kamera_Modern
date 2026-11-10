using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class CharacterNameReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;

        public CharacterNameReaderTests(OcrEngineFixture fixture) => _fixture = fixture;

        private static Image<Rgba32> Load(string file) =>
            Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "CharacterNames", file));

        private static string Clean(string ocr) => Regex.Replace(ocr.ToLowerInvariant(), "[^a-z0-9]", "");

        /// <summary>
        /// Name crops saved by real scans. The first four failed every retry with the old grayscale
        /// threshold (it read nothing): coloured backgrounds became as bright as the white text.
        /// Vesna failing 75 times crashed the character scan.
        /// </summary>
        [Theory]
        [InlineData("vesna_teal_background.png", "vesna")]
        [InlineData("xilonen_gold_background.png", "xilonen")]
        [InlineData("zibai.png", "zibai")]
        [InlineData("lauma.png", "lauma")]
        [InlineData("chiori_reads_fine.png", "chiori")]
        [InlineData("traveler_custom_name.png", "syrusgreycloak")]
        public void ReadWithWhiteMask_SavedCrop_ReadsName(string file, string expected)
        {
            using var crop = Load(file);
            Assert.Equal(expected, Clean(CharacterNameReader.ReadWithWhiteMask(crop, _fixture.ImageProcessor, _fixture.Ocr)));
        }

        [Fact]
        public void ReadCandidates_WhiteMaskFirst_FallbackSecond()
        {
            using var crop = Load("vesna_teal_background.png");
            var readings = CharacterNameReader.ReadCandidates(crop, _fixture.ImageProcessor, _fixture.Ocr).Take(2).ToList();

            Assert.Equal(2, readings.Count);
            Assert.Equal("vesna", Clean(readings[0]));
        }

        [Fact]
        public void WhiteTextMask_OnlyNearWhitePixelsBecomeText()
        {
            using var img = new Image<Rgba32>(3, 1);
            img[0, 0] = new Rgba32(250, 250, 245, 255); // white text
            img[1, 0] = new Rgba32(120, 230, 230, 255); // bright teal background
            img[2, 0] = new Rgba32(240, 220, 120, 255); // bright gold background

            using var mask = CharacterNameReader.WhiteTextMask(img, CharacterNameReader.WhiteTextMinChannel);

            Assert.Equal(0, mask[0, 0].R);   // text -> black
            Assert.Equal(255, mask[1, 0].R); // background -> white
            Assert.Equal(255, mask[2, 0].R);
        }
    }
}
