using System.Drawing;

namespace InventoryKamera.Infrastructure
{
    internal class WinFormsImageProcessor : IImageProcessor
    {
        public Bitmap Crop(Bitmap source, Rectangle region)
        {
            return GenshinProcesor.CopyBitmap(source, region);
        }

        public System.Drawing.Color GetPixelColor(Bitmap image, int x, int y)
        {
            return image.GetPixel(x, y);
        }

        public Bitmap SetGrayscale(Bitmap bitmap)
        {
            return GenshinProcesor.ConvertToGrayscale(bitmap);
        }

        public Bitmap SetInvert(Bitmap bitmap)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.SetInvert(ref bmp);
            return bmp;
        }
    }
}
