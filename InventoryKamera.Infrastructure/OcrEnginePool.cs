using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using TesseractOCR;
using TesseractOCR.Enums;
using InventoryKamera; // IOcrEngine, PageSegmentationMode

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// A pool of Tesseract engines shared by the scraper threads. Dispose it on shutdown: Tesseract
    /// otherwise reports its loaded dictionaries as leaked ("ObjectCache ... WARNING! LEAK!") when the
    /// process exits.
    /// </summary>
    public class OcrEnginePool : IOcrEngine, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const int NumEngines = 8;
        private readonly string _tessdataPath;
        private readonly string _tessdataLanguage;
        private ConcurrentBag<Engine> _engines;
        private volatile bool _disposed;

        public OcrEnginePool()
        {
            _tessdataPath = Path.Combine(System.AppContext.BaseDirectory, "tessdata");
            _tessdataLanguage = "genshin_best_eng";
            _engines = new ConcurrentBag<Engine>();
            InitEngines();
        }

        private void InitEngines()
        {
            for (int i = 0; i < NumEngines; i++)
                _engines.Add(new Engine(_tessdataPath, _tessdataLanguage, EngineMode.LstmOnly));
        }

        public void RestartEngines()
        {
            lock (_engines)
            {
                while (!_engines.IsEmpty)
                    if (_engines.TryTake(out Engine e)) e.Dispose();
                InitEngines();
            }
        }

        public string AnalyzeText(Image<Rgba32> bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false)
        {
            var tessMode = (PageSegMode)(int)mode;
            Engine engine;
            while (!_engines.TryTake(out engine)) Thread.Sleep(10);

            try
            {
                if (numbersOnly) engine.SetVariable("tessedit_char_whitelist", "0123456789");

                byte[] pngBytes;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, new PngEncoder());
                    pngBytes = ms.ToArray();
                }

                using (var pix = TesseractOCR.Pix.Image.LoadFromMemory(pngBytes))
                using (var page = engine.Process(pix, tessMode))
                {
                    return page.Text;
                }
            }
            finally
            {
                engine.SetVariable("tessedit_char_whitelist", ""); // always clear whitelist before returning
                _engines.Add(engine);
                if (_disposed) DisposeIdleEngines(); // returned after Dispose: don't leave it behind
            }
        }

        /// <summary>Disposes the engines. One still in use is disposed when it is returned.</summary>
        public void Dispose()
        {
            _disposed = true;
            DisposeIdleEngines();
        }

        private void DisposeIdleEngines()
        {
            lock (_engines)
            {
                while (_engines.TryTake(out Engine e)) e.Dispose();
            }
        }
    }
}
