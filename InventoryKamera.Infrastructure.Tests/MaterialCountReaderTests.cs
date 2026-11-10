using System.IO;
using InventoryKamera;
using InventoryKamera.Infrastructure.Scraping;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class MaterialCountReaderTests
    {
        /// <summary>
        /// Strips captured the way <c>ScanMaterialCount</c> captures them (45 px above to 30 px below
        /// the bottom of the detected card box, 1080p) from the scrolled-to-the-end last page of the
        /// Materials tab, where the detected boxes cover the whole card. The old fixed crop read 6 of
        /// these 32 correctly (the fish row came out as 1, 1, 11, 14, 1, 1, 1, 1).
        /// </summary>
        [Theory]
        [InlineData(0, 0, 36)] [InlineData(0, 1, 190)] [InlineData(0, 2, 14)] [InlineData(0, 3, 202)]
        [InlineData(0, 4, 99)] [InlineData(0, 5, 176)] [InlineData(0, 6, 1058)] [InlineData(0, 7, 122)]
        [InlineData(1, 0, 10)] [InlineData(1, 1, 18)] [InlineData(1, 2, 44)] [InlineData(1, 3, 21)]
        [InlineData(1, 4, 20)] [InlineData(1, 5, 17)] [InlineData(1, 6, 12)] [InlineData(1, 7, 6)]
        [InlineData(2, 0, 15)] [InlineData(2, 1, 11)] [InlineData(2, 2, 33)] [InlineData(2, 3, 10)]
        [InlineData(2, 4, 6)] [InlineData(2, 5, 7)] [InlineData(2, 6, 16)] [InlineData(2, 7, 2)]
        // The fish row: Pufferfish, Bitter Pufferfish, rays, anglers, axe marlins
        [InlineData(3, 0, 9)] [InlineData(3, 1, 5)] [InlineData(3, 2, 11)] [InlineData(3, 3, 14)]
        [InlineData(3, 4, 6)] [InlineData(3, 5, 3)] [InlineData(3, 6, 9)] [InlineData(3, 7, 8)]
        public void LabelBand_LastPageCard_ReadsCount(int row, int col, int expected)
        {
            using var strip = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "MaterialCounts", $"strip_r{row}c{col}.png"));

            var band = MaterialCountReader.FindLabelBand(strip, minRows: 18, maxRows: MaterialCountReader.LabelHeight1080);
            Assert.NotNull(band);

            var (top, bottom) = band.Value;
            using var label = strip.Clone(c => c.Crop(new Rectangle(0, top, strip.Width, bottom - top + 1)));
            using var bitmap = GenshinProcesor.ImageSharpToBitmap(label);
            Assert.Equal(expected, MaterialCountReader.ReadCount(bitmap));
        }

        /// <summary>
        /// Enhancement ores at the end of the weapon page (scrolled to the end, 1080p), read the
        /// same way. The old fixed crop read the first as 209.
        /// </summary>
        [Theory]
        [InlineData("ore_mystic.png", 259)]
        [InlineData("ore_fine.png", 131)]
        [InlineData("ore_enhancement.png", 2)]
        public void LabelBand_EnhancementOre_ReadsCount(string file, int expected)
        {
            using var strip = Load(file);

            var band = MaterialCountReader.FindLabelBand(strip, minRows: 18, maxRows: MaterialCountReader.LabelHeight1080);
            Assert.NotNull(band);

            var (top, bottom) = band.Value;
            using var label = strip.Clone(c => c.Crop(new Rectangle(0, top, strip.Width, bottom - top + 1)));
            using var bitmap = GenshinProcesor.ImageSharpToBitmap(label);
            Assert.Equal(expected, MaterialCountReader.ReadCount(bitmap));
        }

        /// <summary>
        /// The item grid is a full grid, so the last row can end in empty cells. The ore scan skips
        /// cells without a label strip; a weapon's card has one.
        /// </summary>
        [Theory]
        [InlineData("empty_cell.png", false)]
        [InlineData("weapon_cell.png", true)]
        public void FindLabelBand_WeaponPageCell_FindsLabelOnlyOnCards(string file, bool hasCard)
        {
            using var strip = Load(file);
            Assert.Equal(hasCard, MaterialCountReader.FindLabelBand(strip, minRows: 18, maxRows: MaterialCountReader.LabelHeight1080) != null);
        }

        /// <summary>
        /// A label crop showing "2" that Tesseract read as "F4" and "4" at 1x and 1.5x, which used
        /// to settle the vote on 4.
        /// </summary>
        [Fact]
        public void ReadCount_SmallTwo_IsNotReadAsFour()
        {
            using var label = Load("label_2_small.png");
            using var bitmap = GenshinProcesor.ImageSharpToBitmap(label);
            Assert.Equal(2, MaterialCountReader.ReadCount(bitmap));
        }

        private static Image<Rgba32> Load(string file) =>
            Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "MaterialCounts", file));

        [Fact]
        public void FindLabelBand_NoLabel_ReturnsNull()
        {
            // Dark inventory background only: fall back to the fixed crop
            using var strip = new Image<Rgba32>(126, 75, new Rgba32(110, 115, 143, 255));
            Assert.Null(MaterialCountReader.FindLabelBand(strip, minRows: 18, maxRows: 28));
        }

        [Fact]
        public void FindLabelBand_LightIconAboveLabel_KeepsBottomOfRun()
        {
            // 50 light rows (icon running into the label): only the bottom 28 are the label
            using var strip = new Image<Rgba32>(126, 75, new Rgba32(110, 115, 143, 255));
            for (int y = 10; y < 60; y++)
                for (int x = 0; x < 126; x++)
                    strip[x, y] = new Rgba32(233, 229, 220, 255);

            Assert.Equal((32, 59), MaterialCountReader.FindLabelBand(strip, minRows: 18, maxRows: 28));
        }
    }
}
