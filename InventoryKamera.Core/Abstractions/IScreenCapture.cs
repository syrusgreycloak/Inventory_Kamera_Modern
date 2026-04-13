using System.Drawing;

namespace InventoryKamera
{
    public interface IScreenCapture
    {
        Bitmap CaptureWindow();
        Bitmap CaptureRegion(int x, int y, int width, int height);
        int GetWidth();
        int GetHeight();
    }
}
