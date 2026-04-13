using System.Collections.Generic;
using System.Drawing;

namespace InventoryKamera
{
    public interface IImageProcessor
    {
        // Existing
        Bitmap Crop(Bitmap source, Rectangle region);
        Color GetPixelColor(Bitmap image, int x, int y);
        Bitmap SetGrayscale(Bitmap bitmap);
        Bitmap SetInvert(Bitmap bitmap);

        // New — all return new Bitmap; callers are responsible for disposing
        Bitmap SetContrast(Bitmap bitmap, double contrast);
        Bitmap SetGamma(Bitmap bitmap, double red, double green, double blue);
        Bitmap SetThreshold(Bitmap bitmap, int threshold);
        Bitmap FilterColors(Bitmap bitmap, IntRange red, IntRange green, IntRange blue);
        Bitmap ResizeImage(Bitmap bitmap, int width, int height);
        Bitmap ScaleImage(Bitmap bitmap, double factor);
        Bitmap KirschEdgeDetect(Bitmap bitmap);
        Bitmap PreProcessImage(Bitmap bitmap);
        Color GetAverageColor(Bitmap bitmap);
        Color ClosestColor(IList<Color> colors, Color target);
        bool CompareColors(Color a, Color b);
        bool CompareBitmapsFast(Bitmap bmp1, Bitmap bmp2);
    }
}
