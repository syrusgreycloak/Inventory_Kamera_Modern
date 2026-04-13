using System.Drawing;
using Tesseract;

namespace InventoryKamera.Infrastructure
{
    internal class OcrEnginePool : IOcrEngine
    {
        public string AnalyzeText(Bitmap bitmap, PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false)
        {
            return GenshinProcesor.AnalyzeText(bitmap, pageMode, numbersOnly);
        }
    }
}
