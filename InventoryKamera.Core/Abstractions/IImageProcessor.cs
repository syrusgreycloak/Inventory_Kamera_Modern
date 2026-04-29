using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    public interface IImageProcessor
    {
        // Existing
        Image<Rgba32> Crop(Image<Rgba32> source, Rectangle region);
        Rgba32 GetPixelColor(Image<Rgba32> image, int x, int y);
        Image<Rgba32> SetGrayscale(Image<Rgba32> bitmap);
        Image<Rgba32> SetInvert(Image<Rgba32> bitmap);

        // New — all return new Image<Rgba32>; callers are responsible for disposing
        Image<Rgba32> SetContrast(Image<Rgba32> bitmap, double contrast);
        Image<Rgba32> SetGamma(Image<Rgba32> bitmap, double red, double green, double blue);
        Image<Rgba32> SetThreshold(Image<Rgba32> bitmap, int threshold);
        Image<Rgba32> FilterColors(Image<Rgba32> bitmap, IntRange red, IntRange green, IntRange blue);
        Image<Rgba32> ResizeImage(Image<Rgba32> bitmap, int width, int height);
        Image<Rgba32> ScaleImage(Image<Rgba32> bitmap, double factor);
        Image<Rgba32> KirschEdgeDetect(Image<Rgba32> bitmap);
        Image<Rgba32> PreProcessImage(Image<Rgba32> bitmap);
        Rgba32 GetAverageColor(Image<Rgba32> bitmap);
        Rgba32 ClosestColor(IList<Rgba32> colors, Rgba32 target);
        bool CompareColors(Rgba32 a, Rgba32 b);
        bool CompareBitmapsFast(Image<Rgba32> bmp1, Image<Rgba32> bmp2);
    }
}
