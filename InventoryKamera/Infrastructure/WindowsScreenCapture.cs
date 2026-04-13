using System.Drawing;

namespace InventoryKamera.Infrastructure
{
    internal class WindowsScreenCapture : IScreenCapture
    {
        public Bitmap CaptureWindow() => Navigation.CaptureWindow();
        public Bitmap CaptureRegion(int x, int y, int width, int height) => Navigation.CaptureRegion(x, y, width, height);
        public Bitmap CaptureRegion(RECT region) => Navigation.CaptureRegion(region);
        public int GetWidth() => Navigation.GetWidth();
        public int GetHeight() => Navigation.GetHeight();
        public Size GetAspectRatio() => Navigation.GetAspectRatio();
        public bool IsNormal => Navigation.IsNormal;
    }
}
