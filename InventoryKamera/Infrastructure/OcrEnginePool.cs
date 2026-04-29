using System.Drawing;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using TesseractOCR;
using TesseractOCR.Enums;

namespace InventoryKamera.Infrastructure
{
    internal class OcrEnginePool : IOcrEngine
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const int NumEngines = 8;
        private readonly string _tessdataPath;
        private readonly string _tessdataLanguage;
        private ConcurrentBag<Engine> _engines;

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

        public string AnalyzeText(Bitmap bitmap, PageSegmentationMode mode = PageSegmentationMode.SingleLine, bool numbersOnly = false)
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
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
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
            }
        }
    }
}
