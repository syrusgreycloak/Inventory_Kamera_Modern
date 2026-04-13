using System.Drawing;
using Tesseract;

namespace InventoryKamera.Infrastructure
{
    internal class OcrEnginePool : IOcrEngine
    {
        public string AnalyzeText(Bitmap bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false)
        {
            // Map Core enum to Tesseract PageSegMode by integer value
            var tessMode = (PageSegMode)(int)mode;
            return GenshinProcesor.AnalyzeText(bitmap, tessMode, numbersOnly);
        }
    }
}
