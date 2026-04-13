using System;
using System.Collections.Concurrent;

using TesseractOCR;
using TesseractOCR.Enums;

namespace TesseractEval
{
    class Program
    {
        private const int NumEngines = 8;
        private const string TessDataPath = @".\tessdata";
        private const string TessLanguage = "genshin_fast_09_04_21";

        static int Main(string[] args)
        {
            Console.WriteLine("=== TesseractOCR 5.5.2 Evaluation ===");

            // Test 1: Engine initialization
            Console.WriteLine("\n[Test 1] Initializing 8 engines...");
            var engines = new ConcurrentBag<Engine>();
            try
            {
                for (int i = 0; i < NumEngines; i++)
                {
                    // TesseractOCR 5.5.2 API: Engine(dataPath, language, engineMode, ...)
                    // Language is passed as a string (custom traineddata filename without extension)
                    var engine = new Engine(TessDataPath, TessLanguage, EngineMode.Default);
                    engines.Add(engine);
                }
                Console.WriteLine($"  PASS: {engines.Count} engines initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL: {ex.Message}");
                Console.WriteLine("\nEvaluation complete. Document result in QUICK_WINS.md.");
                // Dispose any that were created
                foreach (var e in engines)
                    e.Dispose();
                return 1;
            }

            Console.WriteLine("\n[Test 2] Disposing engines...");
            try
            {
                foreach (var engine in engines)
                    engine.Dispose();
                Console.WriteLine("  PASS: All engines disposed cleanly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL during dispose: {ex.Message}");
                return 1;
            }

            Console.WriteLine("\nAll tests passed. TesseractOCR 5.5.2 is viable.");
            return 0;
        }
    }
}
