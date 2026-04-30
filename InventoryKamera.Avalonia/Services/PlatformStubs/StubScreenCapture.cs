using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera.Avalonia.Services.PlatformStubs
{
    /// <summary>
    /// Non-Windows stub for <see cref="IScreenCapture"/>.
    /// Every method throws <see cref="PlatformNotSupportedException"/>.
    /// The ScanViewModel platform guard prevents these from being reached at runtime.
    /// </summary>
    public class StubScreenCapture : IScreenCapture
    {
        private static void Throw() =>
            throw new PlatformNotSupportedException("Scanning requires Windows.");

        public Image<Rgba32> CaptureWindow()                                   { Throw(); return null; }
        public Image<Rgba32> CaptureRegion(int x, int y, int width, int height) { Throw(); return null; }
        public Image<Rgba32> CaptureRegion(RECT region)                        { Throw(); return null; }
        public int GetWidth()                                                   { Throw(); return 0; }
        public int GetHeight()                                                  { Throw(); return 0; }
        public Size GetAspectRatio()                                            { Throw(); return default; }
        public bool IsNormal                                                    { get { Throw(); return false; } }
    }
}
