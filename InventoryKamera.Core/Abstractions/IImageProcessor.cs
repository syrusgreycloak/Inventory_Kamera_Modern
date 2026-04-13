using System.Drawing;

namespace InventoryKamera
{
    public interface IImageProcessor
    {
        Bitmap Crop(Bitmap source, Rectangle region);
        Color GetPixelColor(Bitmap image, int x, int y);
        Bitmap SetGrayscale(Bitmap bitmap);
        Bitmap SetInvert(Bitmap bitmap);
    }
}
