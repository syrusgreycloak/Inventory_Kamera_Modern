using System.Drawing;
using Tesseract;

namespace InventoryKamera
{
    public interface IOcrEngine
    {
        string AnalyzeText(Bitmap bitmap, PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false);
    }
}
