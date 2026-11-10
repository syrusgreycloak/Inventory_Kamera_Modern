using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using InventoryKamera.Infrastructure.Scraping;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class BitmapConversionTests
    {
        /// <summary>
        /// Screen captures are converted with a direct pixel copy instead of a PNG encode/decode
        /// round trip (~25x faster). They must produce identical pixels for the 24-bit format the
        /// scanner captures in, including the red/blue channel order.
        /// </summary>
        [Fact]
        public void BitmapToImageSharp_MatchesPngRoundTrip()
        {
            using var bmp = new Bitmap(7, 5, PixelFormat.Format24bppRgb);
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(255, x * 36, y * 60, (x * y * 13) % 256));
            bmp.SetPixel(0, 0, Color.FromArgb(255, 255, 0, 0)); // pure red: catches a channel swap

            using var direct = GenshinProcesor.BitmapToImageSharp(bmp);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            using var viaPng = SixLabors.ImageSharp.Image.Load<Rgba32>(ms);

            Assert.Equal(new Rgba32(255, 0, 0, 255), direct[0, 0]);
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    Assert.Equal(viaPng[x, y], direct[x, y]);
        }
    }
}
