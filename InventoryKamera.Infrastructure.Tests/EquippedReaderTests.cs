using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class EquippedReaderTests : IClassFixture<OcrEngineFixture>
    {
        private readonly OcrEngineFixture _fixture;
        private readonly GameDataService _gameData;

        public EquippedReaderTests(OcrEngineFixture fixture)
        {
            _fixture = fixture;

            // Real 7.2 character list (copied from reference_data), with the custom names the
            // crops below were scanned with, set the same way InventoryScanner does.
            _gameData = new GameDataService(new DatabaseManager());
            _gameData.UpdateCharacterName("traveler", "SyrusGreycloak");
            _gameData.UpdateCharacterName("wanderer", "Yoshida");
        }

        private static Image<Rgba32> Load(string file) =>
            Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Equipped", file));

        private string Read(Image<Rgba32> crop) =>
            EquippedReader.Read(crop, _fixture.ImageProcessor, _fixture.Ocr, n => _gameData.FindClosestCharacterName(n));

        /// <summary>
        /// Equipped-bar crops saved by a real 16:9 scan.
        /// - Xinyan: her icon covers the crop's top-left corner, which the old (5,5) colour check
        ///   sampled, so her gear came out unequipped.
        /// - Traveler/Wanderer: custom names, which the artifact path couldn't match because it used
        ///   a static copy of the game data that never received them.
        /// </summary>
        [Theory]
        [InlineData("xinyan_icon_over_corner.png", "Xinyan")]
        [InlineData("traveler_custom_name.png", "Traveler")]
        [InlineData("wanderer_custom_name.png", "Wanderer")]
        public void Read_EquippedCrop_ReturnsCharacterKey(string file, string expected)
        {
            using var crop = Load(file);
            Assert.True(EquippedReader.IsEquipped(crop, _fixture.ImageProcessor));
            Assert.Equal(expected, Read(crop));
        }

        [Theory]
        [InlineData("unequipped_plain.png")]
        [InlineData("unequipped_description_text.png")] // crop catches item description text
        public void Read_UnequippedCrop_ReturnsNull(string file)
        {
            using var crop = Load(file);
            Assert.False(EquippedReader.IsEquipped(crop, _fixture.ImageProcessor));
            Assert.Null(Read(crop));
        }

        [Theory]
        [InlineData("Equipped: SyrusGreycloak\n", "syrusgreycloak")]
        [InlineData("Equipped: Kaedehara Kazuha", "kaedeharakazuha")]
        [InlineData("Equipped: Hu Tao", "hutao")]
        [InlineData("equipped : Xinyan", "xinyan")]
        [InlineData("ay en. To him, it", null)]
        [InlineData("Equipped:", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void ParseName_OcrText_ReturnsCleanedName(string text, string expected)
        {
            Assert.Equal(expected, EquippedReader.ParseName(text));
        }
    }
}
