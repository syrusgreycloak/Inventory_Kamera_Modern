using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera.Avalonia.Services.PlatformStubs
{
    /// <summary>
    /// Non-Windows stub for <see cref="IOcrEngine"/>.
    /// Every method throws <see cref="PlatformNotSupportedException"/>.
    /// The ScanViewModel platform guard prevents these from being reached at runtime.
    /// </summary>
    public class StubOcrEngine : IOcrEngine
    {
        private static void Throw() =>
            throw new PlatformNotSupportedException("Scanning requires Windows.");

        public string AnalyzeText(Image<Rgba32> bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false)
        {
            Throw();
            return null;
        }

        public void RestartEngines() { Throw(); }
    }
}
