using System.Drawing;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using Tesseract;

namespace InventoryKamera.Infrastructure
{
    internal class OcrEnginePool : IOcrEngine
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const int NumEngines = 8;
        private readonly string _tessdataPath;
        private readonly string _tessdataLanguage;
        private ConcurrentBag<TesseractEngine> _engines;

        public OcrEnginePool()
        {
            _tessdataPath = Path.Combine(System.AppContext.BaseDirectory, "tessdata");
            _tessdataLanguage = "genshin_fast_09_04_21";
            _engines = new ConcurrentBag<TesseractEngine>();
            InitEngines();
        }

        private void InitEngines()
        {
            for (int i = 0; i < NumEngines; i++)
                _engines.Add(new TesseractEngine(_tessdataPath, _tessdataLanguage, EngineMode.LstmOnly));
        }

        public void RestartEngines()
        {
            lock (_engines)
            {
                while (!_engines.IsEmpty)
                    if (_engines.TryTake(out TesseractEngine e)) e.Dispose();
                InitEngines();
            }
        }

        public string AnalyzeText(Bitmap bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false)
        {
            var tessMode = (PageSegMode)(int)mode;
            TesseractEngine engine;
            while (!_engines.TryTake(out engine)) Thread.Sleep(10);

            if (numbersOnly) engine.SetVariable("tessedit_char_whitelist", "0123456789");

            byte[] pngBytes;
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                pngBytes = ms.ToArray();
            }

            string text = "";
            using (var pix = Pix.LoadFromMemory(pngBytes))
            using (var page = engine.Process(pix, tessMode))
            using (var iter = page.GetIterator())
            {
                iter.Begin();
                do { text += iter.GetText(PageIteratorLevel.TextLine); }
                while (iter.Next(PageIteratorLevel.TextLine));
            }

            _engines.Add(engine);
            return text;
        }
    }
}
