using System.Drawing;

namespace InventoryKamera.Infrastructure
{
    internal class WindowsScreenCapture : IScreenCapture
    {
        public Bitmap CaptureWindow()
        {
            return Navigation.CaptureWindow();
        }

        public Bitmap CaptureRegion(int x, int y, int width, int height)
        {
            return Navigation.CaptureRegion(x, y, width, height);
        }

        public int GetWidth()
        {
            return Navigation.GetWidth();
        }

        public int GetHeight()
        {
            return Navigation.GetHeight();
        }
    }
}
