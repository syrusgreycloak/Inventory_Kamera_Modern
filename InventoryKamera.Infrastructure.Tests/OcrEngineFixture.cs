using System;
using InventoryKamera;
using InventoryKamera.Infrastructure;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    /// <summary>
    /// Shares one OCR engine pool across a test class: creating the pool loads the trained data
    /// into 8 Tesseract engines, which is too slow to repeat per test.
    /// </summary>
    public sealed class OcrEngineFixture : IDisposable
    {
        public IOcrEngine Ocr { get; } = new OcrEnginePool();
        public IImageProcessor ImageProcessor { get; } = new WinFormsImageProcessor();

        public void Dispose() => (Ocr as IDisposable)?.Dispose();
    }
}
