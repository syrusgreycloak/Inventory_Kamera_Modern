using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class RefinementReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;

        public RefinementReaderTests(OcrEngineFixture fixture) => _fixture = fixture;

        /// <summary>
        /// Badge crops saved by a real 1920x1080 (16:9) scan, one per rank. R2 is the one that
        /// regressed: without the digit whitelist Tesseract read it as "|?)".
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Read_SavedBadge_ReturnsRank(int expected)
        {
            var path = Path.Combine(System.AppContext.BaseDirectory, "TestData", "Refinement", $"r{expected}.png");
            using var badge = Image.Load<Rgba32>(path);

            int actual = RefinementReader.Read(badge, _fixture.ImageProcessor, _fixture.Ocr);

            Assert.Equal(expected, actual);
        }

        /// <summary>Raw OCR strings actually seen in scan logs, plus edge cases.</summary>
        [Theory]
        [InlineData("1", 1)]
        [InlineData("5)", 5)]   // badge's right edge read as ")"
        [InlineData("3!", 3)]
        [InlineData("12", 2)]   // stray leading "1" from the badge edge
        [InlineData("13", 3)]
        [InlineData("|?)", -1)] // the old R2 failure: no digit at all
        [InlineData("", -1)]
        [InlineData(null, -1)]
        [InlineData("0", -1)]
        [InlineData("6", -1)]
        public void Parse_RawOcrText_ReturnsRankOrMinusOne(string raw, int expected)
        {
            Assert.Equal(expected, RefinementReader.Parse(raw));
        }
    }
}
