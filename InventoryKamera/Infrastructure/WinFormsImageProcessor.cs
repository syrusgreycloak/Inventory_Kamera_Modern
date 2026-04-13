using System.Collections.Generic;
using System.Drawing;

namespace InventoryKamera.Infrastructure
{
    internal class WinFormsImageProcessor : IImageProcessor
    {
        public Bitmap Crop(Bitmap source, Rectangle region) =>
            GenshinProcesor.CopyBitmap(source, region);

        public Color GetPixelColor(Bitmap image, int x, int y) =>
            image.GetPixel(x, y);

        public Bitmap SetGrayscale(Bitmap bitmap) =>
            GenshinProcesor.ConvertToGrayscale(bitmap);

        public Bitmap SetInvert(Bitmap bitmap)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.SetInvert(ref bmp);
            return bmp;
        }

        public Bitmap SetContrast(Bitmap bitmap, double contrast)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.SetContrast(contrast, ref bmp);
            return bmp;
        }

        public Bitmap SetGamma(Bitmap bitmap, double red, double green, double blue)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.SetGamma(red, green, blue, ref bmp);
            return bmp;
        }

        public Bitmap SetThreshold(Bitmap bitmap, int threshold)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.SetThreshold(threshold, ref bmp);
            return bmp;
        }

        public Bitmap FilterColors(Bitmap bitmap, IntRange red, IntRange green, IntRange blue)
        {
            var bmp = (Bitmap)bitmap.Clone();
            GenshinProcesor.FilterColors(ref bmp, red, green, blue);
            return bmp;
        }

        public Bitmap ResizeImage(Bitmap bitmap, int width, int height) =>
            GenshinProcesor.ResizeImage(bitmap, width, height);

        public Bitmap ScaleImage(Bitmap bitmap, double factor) =>
            GenshinProcesor.ScaleImage(bitmap, factor);

        public Bitmap KirschEdgeDetect(Bitmap bitmap) =>
            GenshinProcesor.KirschEdgeDetect(bitmap);

        public Bitmap PreProcessImage(Bitmap bitmap) =>
            GenshinProcesor.PreProcessImage(bitmap);

        public Color GetAverageColor(Bitmap bitmap) =>
            GenshinProcesor.GetAverageColor(bitmap);

        public Color ClosestColor(IList<Color> colors, Color target) =>
            GenshinProcesor.ClosestColor(new List<Color>(colors), target);

        public bool CompareColors(Color a, Color b) =>
            GenshinProcesor.CompareColors(a, b);

        public bool CompareBitmapsFast(Bitmap bmp1, Bitmap bmp2) =>
            GenshinProcesor.CompareBitmapsFast(bmp1, bmp2);
    }
}
