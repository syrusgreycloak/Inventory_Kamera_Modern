using System.Drawing;

namespace InventoryKamera
{
    /// <summary>Core-defined page segmentation mode, independent of any specific OCR library.</summary>
    public enum PageSegmentationMode
    {
        SingleLine = 7,    // Treat the image as a single text line (Tesseract PSM 7)
        SingleBlock = 6,   // Treat the image as a single uniform block of text (Tesseract PSM 6)
        Auto = 3,          // Fully automatic page segmentation, no OSD (Tesseract PSM 3)
        SingleWord = 8,    // Treat image as a single word (Tesseract PSM 8)
    }

    public interface IOcrEngine
    {
        string AnalyzeText(Bitmap bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false);
    }
}
