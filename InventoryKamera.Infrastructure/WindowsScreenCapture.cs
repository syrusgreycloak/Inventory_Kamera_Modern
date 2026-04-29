using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using InventoryKamera; // RECT
using InventoryKamera.Infrastructure.Windows; // Navigation

namespace InventoryKamera.Infrastructure
{
    public class WindowsScreenCapture : IScreenCapture
    {
        public Image<Rgba32> CaptureWindow() => BitmapToImageSharp(Navigation.CaptureWindow());
        public Image<Rgba32> CaptureRegion(int x, int y, int width, int height) => BitmapToImageSharp(Navigation.CaptureRegion(x, y, width, height));
        public Image<Rgba32> CaptureRegion(RECT region) => BitmapToImageSharp(Navigation.CaptureRegion(region));
        public int GetWidth() => Navigation.GetWidth();
        public int GetHeight() => Navigation.GetHeight();
        public SixLabors.ImageSharp.Size GetAspectRatio()
        {
            var s = Navigation.GetAspectRatio();
            return new SixLabors.ImageSharp.Size(s.Width, s.Height);
        }
        public bool IsNormal => Navigation.IsNormal;

        /// <summary>
        /// Converts a GDI+ <see cref="Bitmap"/> to an ImageSharp <see cref="Image{Rgba32}"/>.
        /// The source bitmap is disposed after conversion.
        /// </summary>
        internal static Image<Rgba32> BitmapToImageSharp(Bitmap bmp)
        {
            using (bmp)
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
            }
        }
    }
}
