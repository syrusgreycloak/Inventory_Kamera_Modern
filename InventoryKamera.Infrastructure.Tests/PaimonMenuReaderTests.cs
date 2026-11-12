using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class PaimonMenuReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;

        public PaimonMenuReaderTests(OcrEngineFixture fixture) => _fixture = fixture;

        // Test data keeps only the label region of a 1920x1080 screen (cropped clear of the UID,
        // name and values); paste it back into a full-size screen.
        private static Image<Rgba32> Screenshot(string file)
        {
            var canvas = new Image<Rgba32>(1920, 1080, new Rgba32(40, 40, 60, 255));
            using var labels = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "PaimonMenu", file));
            var at = PaimonMenuReader.LabelRegion1080;
            canvas.Mutate(c => c.DrawImage(labels, new Point(at.X, at.Y), 1f));
            return canvas;
        }

        [Theory]
        [InlineData("menu_labels.png", true)]   // Paimon menu open
        [InlineData("world.png", false)]        // open world: scenery in that spot
        public void IsOpen_SavedScreen(string file, bool expected)
        {
            using var screenshot = Screenshot(file);
            Assert.Equal(expected, PaimonMenuReader.IsOpen(screenshot, _fixture.Ocr));
        }

        [Theory]
        [InlineData("Adventure Rank\nAdventure EXP\nWorld Level\nBirthday", true)]
        [InlineData("World Level", true)]
        [InlineData("AdventureRank", true)]
        [InlineData("Weapons", false)]
        [InlineData("", false)]
        public void HasMenuLabels_Text(string text, bool expected)
        {
            Assert.Equal(expected, PaimonMenuReader.HasMenuLabels(text));
        }
    }
}
