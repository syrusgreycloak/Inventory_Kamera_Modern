using NLog;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>
    /// Reads a weapon's refinement rank (1-5) from the small numbered badge to the left of
    /// "Refinement Rank N" on the weapon card. Kept separate from <see cref="WeaponScraper"/>
    /// so it can be tested against saved badge images without a scraper or a running game.
    /// </summary>
    public static class RefinementReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// OCRs the refinement badge. Returns 1-5, or -1 if no valid rank could be read.
        /// The caller keeps ownership of <paramref name="badge"/>.
        /// </summary>
        public static int Read(Image<Rgba32> badge, IImageProcessor imageProcessor, IOcrEngine ocrEngine)
        {
            // Process once at the original size: earlier scaling distorted the "2".
            string text;
            using (var gray = imageProcessor.SetGrayscale(badge))
            using (var inverted = imageProcessor.SetInvert(gray))
            {
                // Restrict OCR to digits: unconstrained, Tesseract read every R2 badge as "|?)" (the
                // badge's edges became "|" and ")" and the 2 became "?"). With the digit whitelist all
                // 155 saved refinement crops from a full scan (R1-R5) read correctly.
                text = ocrEngine.AnalyzeText(inverted, PageSegmentationMode.SingleLine, numbersOnly: true).Trim();
            }

            Logger.Debug("Refinement OCR raw output: '{0}'", text);
            int level = Parse(text);
            if (level == -1) Logger.Debug("Refinement OCR failed to parse valid level (1-5)");
            else Logger.Debug("Refinement OCR parsed successfully: {0}", level);
            return level;
        }

        /// <summary>
        /// Turns raw OCR text into a refinement rank: strips non-digits, keeps the last digit of an
        /// impossible multi-digit read (the badge edge can add a stray "1", e.g. "12" for R2), and
        /// returns 1-5, or -1 if the result isn't a valid rank.
        /// </summary>
        public static int Parse(string text)
        {
            string digits = Regex.Replace(text ?? string.Empty, @"[^\d]", string.Empty);

            if (digits.Length > 1 && int.TryParse(digits, out int multiDigit) && multiDigit > 5)
                digits = digits.Substring(digits.Length - 1);

            return int.TryParse(digits, out int level) && level >= 1 && level <= 5 ? level : -1;
        }
    }
}
