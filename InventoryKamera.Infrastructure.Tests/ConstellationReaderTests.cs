using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class ConstellationReaderTests
    {
        // Test data keeps only the node strip of a 1920x1080 screenshot (x 1440-1660, y 220-890);
        // paste it back at its original position to rebuild a screenshot-sized image.
        private static Image<Rgba32> Screenshot(string file)
        {
            var canvas = new Image<Rgba32>(1920, 1080, new Rgba32(0, 0, 0, 255));
            using var strip = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Constellations", file));
            canvas.Mutate(c => c.DrawImage(strip, new Point(1440, 220), 1f));
            return canvas;
        }

        /// <summary>
        /// Constellation screens saved by a real 16:9 scan, one per count, verified against the old
        /// click-each-constellation method (which agreed on all 94 characters of that scan).
        /// </summary>
        [Theory]
        [InlineData("c0_chiori.png", 0)]
        [InlineData("c1_cyno.png", 1)]
        [InlineData("c2_amber.png", 2)]
        [InlineData("c3_kaveh.png", 3)]
        [InlineData("c4_tighnari.png", 4)]
        [InlineData("c5_keqing.png", 5)]
        [InlineData("c6_bennett.png", 6)]
        // A bright background star right behind the locked 6th node: the ring's average reached
        // 130 (inconclusive), its median stays at 84
        [InlineData("c5_keqing_star_glow.png", 5)]
        public void CountActivated_SavedScreen_ReturnsConstellation(string file, int expected)
        {
            using var screenshot = Screenshot(file);
            Assert.Equal(expected, ConstellationReader.CountActivated(screenshot));
        }

        [Fact]
        public void CountActivated_LockedBeforeActivated_IsInconclusive()
        {
            // C6 screen with node 2 painted dark: an impossible pattern, so fall back to clicking
            using var screenshot = Screenshot("c6_bennett.png");
            Paint(screenshot, ConstellationReader.NodeCentres16x9[1], new Rgba32(20, 20, 25, 255));

            Assert.Null(ConstellationReader.CountActivated(screenshot));
        }

        [Fact]
        public void CountActivated_BorderlineBrightness_IsInconclusive()
        {
            using var screenshot = Screenshot("c6_bennett.png");
            Paint(screenshot, ConstellationReader.NodeCentres16x9[0], new Rgba32(138, 138, 138, 255));

            Assert.Null(ConstellationReader.CountActivated(screenshot));
        }

        private static void Paint(Image<Rgba32> img, (double X, double Y) centre, Rgba32 colour)
        {
            int cx = (int)(centre.X * img.Width), cy = (int)(centre.Y * img.Height);
            for (int y = cy - 45; y <= cy + 45; y++)
                for (int x = cx - 45; x <= cx + 45; x++)
                    img[x, y] = colour;
        }
    }
}
