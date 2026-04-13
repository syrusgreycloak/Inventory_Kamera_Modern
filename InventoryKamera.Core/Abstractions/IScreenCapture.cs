using System.Drawing;

namespace InventoryKamera
{
    public interface IScreenCapture
    {
        Bitmap CaptureWindow();
        Bitmap CaptureRegion(int x, int y, int width, int height);
        Bitmap CaptureRegion(RECT region);
        int GetWidth();
        int GetHeight();
        Size GetAspectRatio();
        bool IsNormal { get; }
    }
}
