using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    public interface IScreenCapture
    {
        Image<Rgba32> CaptureWindow();
        Image<Rgba32> CaptureRegion(int x, int y, int width, int height);
        Image<Rgba32> CaptureRegion(RECT region);
        int GetWidth();
        int GetHeight();
        Size GetAspectRatio();
        bool IsNormal { get; }
    }
}
