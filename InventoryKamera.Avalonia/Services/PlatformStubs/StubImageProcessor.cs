using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera.Avalonia.Services.PlatformStubs
{
    /// <summary>
    /// Non-Windows stub for <see cref="IImageProcessor"/>.
    /// Every method throws <see cref="PlatformNotSupportedException"/>.
    /// The ScanViewModel platform guard prevents these from being reached at runtime.
    /// </summary>
    public class StubImageProcessor : IImageProcessor
    {
        private static void Throw() =>
            throw new PlatformNotSupportedException("Scanning requires Windows.");

        public Image<Rgba32> Crop(Image<Rgba32> source, Rectangle region)                  { Throw(); return null; }
        public Rgba32 GetPixelColor(Image<Rgba32> image, int x, int y)                     { Throw(); return default; }
        public Image<Rgba32> SetGrayscale(Image<Rgba32> bitmap)                            { Throw(); return null; }
        public Image<Rgba32> SetInvert(Image<Rgba32> bitmap)                               { Throw(); return null; }
        public Image<Rgba32> SetContrast(Image<Rgba32> bitmap, double contrast)            { Throw(); return null; }
        public Image<Rgba32> SetGamma(Image<Rgba32> bitmap, double red, double green, double blue) { Throw(); return null; }
        public Image<Rgba32> SetThreshold(Image<Rgba32> bitmap, int threshold)             { Throw(); return null; }
        public Image<Rgba32> FilterColors(Image<Rgba32> bitmap, IntRange red, IntRange green, IntRange blue) { Throw(); return null; }
        public Image<Rgba32> ResizeImage(Image<Rgba32> bitmap, int width, int height)      { Throw(); return null; }
        public Image<Rgba32> ScaleImage(Image<Rgba32> bitmap, double factor)               { Throw(); return null; }
        public Image<Rgba32> KirschEdgeDetect(Image<Rgba32> bitmap)                        { Throw(); return null; }
        public Image<Rgba32> PreProcessImage(Image<Rgba32> bitmap)                         { Throw(); return null; }
        public Rgba32 GetAverageColor(Image<Rgba32> bitmap)                                { Throw(); return default; }
        public Rgba32 ClosestColor(IList<Rgba32> colors, Rgba32 target)                    { Throw(); return default; }
        public bool CompareColors(Rgba32 a, Rgba32 b)                                      { Throw(); return false; }
        public bool CompareBitmapsFast(Image<Rgba32> bmp1, Image<Rgba32> bmp2)             { Throw(); return false; }
    }
}
