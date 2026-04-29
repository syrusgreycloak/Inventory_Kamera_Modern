using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Verifies that the PNG round-trip conversion used at Infrastructure boundaries
    /// preserves pixel data faithfully (within the lossy constraints of PNG encoding).
    /// These tests exercise the same code path as the
    /// WindowsScreenCapture.BitmapToImageSharp / WinFormsUserInterface.ImageSharpToBitmap bridges.
    /// </summary>
    public class ImageConversionTests
    {
        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static Image<Rgba32> CreateTestImage(int width, int height)
        {
            var image = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    image[x, y] = new Rgba32((byte)(x % 256), (byte)(y % 256), (byte)((x + y) % 256), 255);
            return image;
        }

        private static Image<Rgba32> PngRoundTrip(Image<Rgba32> source)
        {
            using (var ms = new MemoryStream())
            {
                source.Save(ms, new PngEncoder());
                ms.Position = 0;
                return Image.Load<Rgba32>(ms);
            }
        }

        // -----------------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------------

        [Fact]
        public void PngRoundTrip_PreservesDimensions()
        {
            using var original = CreateTestImage(64, 48);
            using var roundTripped = PngRoundTrip(original);

            Assert.Equal(original.Width, roundTripped.Width);
            Assert.Equal(original.Height, roundTripped.Height);
        }

        [Fact]
        public void PngRoundTrip_PreservesPixelValues()
        {
            using var original = CreateTestImage(32, 32);
            using var roundTripped = PngRoundTrip(original);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    var orig = original[x, y];
                    var copy = roundTripped[x, y];
                    Assert.Equal(orig.R, copy.R);
                    Assert.Equal(orig.G, copy.G);
                    Assert.Equal(orig.B, copy.B);
                    Assert.Equal(orig.A, copy.A);
                }
            }
        }

        [Fact]
        public void PngRoundTrip_SinglePixelImage_PreservesColor()
        {
            using var original = new Image<Rgba32>(1, 1);
            original[0, 0] = new Rgba32(200, 100, 50, 255);

            using var roundTripped = PngRoundTrip(original);

            var pixel = roundTripped[0, 0];
            Assert.Equal(200, pixel.R);
            Assert.Equal(100, pixel.G);
            Assert.Equal(50, pixel.B);
            Assert.Equal(255, pixel.A);
        }

        [Fact]
        public void PngRoundTrip_LargerImage_IsLossless()
        {
            const int w = 128;
            const int h = 128;
            using var original = CreateTestImage(w, h);
            using var roundTripped = PngRoundTrip(original);

            int mismatch = 0;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (original[x, y] != roundTripped[x, y]) mismatch++;

            Assert.Equal(0, mismatch);
        }
    }
}
