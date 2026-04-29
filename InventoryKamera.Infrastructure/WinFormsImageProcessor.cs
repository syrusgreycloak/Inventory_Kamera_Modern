using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using InventoryKamera;
using InventoryKamera.Infrastructure.Scraping; // GenshinProcesor

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// ImageSharp-typed public API that delegates to the GDI+ GenshinProcesor helpers via the
    /// ImageSharp bridge methods in GenshinProcesor. All conversion is encapsulated there.
    /// </summary>
    public class WinFormsImageProcessor : IImageProcessor
    {
        public Image<Rgba32> Crop(Image<Rgba32> source, Rectangle region) =>
            GenshinProcesor.CopyBitmap(source, region);

        public Rgba32 GetPixelColor(Image<Rgba32> image, int x, int y) => image[x, y];

        public Image<Rgba32> SetGrayscale(Image<Rgba32> bitmap) =>
            GenshinProcesor.ConvertToGrayscale(bitmap);

        public Image<Rgba32> SetInvert(Image<Rgba32> bitmap)
        {
            GenshinProcesor.SetInvert(ref bitmap);
            return bitmap;
        }

        public Image<Rgba32> SetContrast(Image<Rgba32> bitmap, double contrast)
        {
            GenshinProcesor.SetContrast(contrast, ref bitmap);
            return bitmap;
        }

        public Image<Rgba32> SetGamma(Image<Rgba32> bitmap, double red, double green, double blue)
        {
            GenshinProcesor.SetGamma(red, green, blue, ref bitmap);
            return bitmap;
        }

        public Image<Rgba32> SetThreshold(Image<Rgba32> bitmap, int threshold)
        {
            GenshinProcesor.SetThreshold(threshold, ref bitmap);
            return bitmap;
        }

        public Image<Rgba32> FilterColors(Image<Rgba32> bitmap, IntRange red, IntRange green, IntRange blue)
        {
            GenshinProcesor.FilterColors(ref bitmap, red, green, blue);
            return bitmap;
        }

        public Image<Rgba32> ResizeImage(Image<Rgba32> bitmap, int width, int height) =>
            GenshinProcesor.ResizeImage(bitmap, width, height);

        public Image<Rgba32> ScaleImage(Image<Rgba32> bitmap, double factor) =>
            GenshinProcesor.ScaleImage(bitmap, factor);

        public Image<Rgba32> KirschEdgeDetect(Image<Rgba32> bitmap) =>
            GenshinProcesor.KirschEdgeDetect(bitmap);

        public Image<Rgba32> PreProcessImage(Image<Rgba32> bitmap) =>
            GenshinProcesor.PreProcessImage(bitmap);

        public Rgba32 GetAverageColor(Image<Rgba32> bitmap) =>
            GenshinProcesor.GetAverageColor(bitmap);

        public Rgba32 ClosestColor(IList<Rgba32> colors, Rgba32 target) =>
            GenshinProcesor.ClosestColor(new List<Rgba32>(colors), target);

        public bool CompareColors(Rgba32 a, Rgba32 b) =>
            GenshinProcesor.CompareColors(a, b);

        public bool CompareBitmapsFast(Image<Rgba32> bmp1, Image<Rgba32> bmp2) =>
            GenshinProcesor.CompareBitmapsFast(bmp1, bmp2);
    }
}
