using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class InventoryTabReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;

        public InventoryTabReaderTests(OcrEngineFixture fixture) => _fixture = fixture;

        // Test data keeps only the title region of a 1920x1080 inventory screenshot; paste it back
        // into a dark screen of full size.
        private static Image<Rgba32> Screenshot(string file)
        {
            var canvas = new Image<Rgba32>(1920, 1080, new Rgba32(30, 33, 45, 255));
            using var title = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "InventoryTabs", file));
            var at = InventoryTabReader.TitleRegion1080;
            canvas.Mutate(c => c.DrawImage(title, new Point(at.X, at.Y), 1f));
            return canvas;
        }

        [Theory]
        [InlineData("weapons.png", InventoryPage.Weapons)]
        [InlineData("artifacts.png", InventoryPage.Artifacts)]
        [InlineData("characterdevelopmentitems.png", InventoryPage.CharacterDevelopmentItems)]   // two lines
        [InlineData("materials.png", InventoryPage.Materials)]
        public void ReadTab_SavedScreen_ReturnsTab(string file, InventoryPage expected)
        {
            using var screenshot = Screenshot(file);
            Assert.Equal(expected, InventoryTabReader.ReadTab(screenshot, _fixture.Ocr));
        }

        [Fact]
        public void ReadTab_NoTitle_ReturnsNull()
        {
            // E.g. still on the Paimon menu or in the open world
            using var screenshot = new Image<Rgba32>(1920, 1080, new Rgba32(30, 33, 45, 255));
            Assert.Null(InventoryTabReader.ReadTab(screenshot, _fixture.Ocr));
        }

        [Theory]
        [InlineData("Weapons", InventoryPage.Weapons)]
        [InlineData("Character Development\nItems", InventoryPage.CharacterDevelopmentItems)]
        [InlineData("Materials", InventoryPage.Materials)]
        [InlineData("Artifacts", InventoryPage.Artifacts)]
        [InlineData("", null)]
        [InlineData("Food", null)]
        public void Parse_Title_ReturnsTab(string text, InventoryPage? expected)
        {
            Assert.Equal(expected, InventoryTabReader.Parse(text));
        }
    }
}
