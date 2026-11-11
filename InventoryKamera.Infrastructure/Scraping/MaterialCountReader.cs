using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text.RegularExpressions;
using InventoryKamera.Infrastructure.Scraping; // GenshinProcesor
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>
    /// Reads the count on a material card: finds the card's light label strip, then OCRs it.
    /// </summary>
    public static class MaterialCountReader
    {
        /// <summary>Height of the label strip on a 1080p screen (measured: 28 px).</summary>
        public const int LabelHeight1080 = 28;

        /// <summary>
        /// Finds the card's label strip (the light beige band holding the count) in a strip captured
        /// around the bottom of the detected card. Returns the first and last row, or null if there
        /// is no band at least <paramref name="minRows"/> tall.
        /// </summary>
        /// <remarks>
        /// The count used to be cropped at a fixed offset from the bottom of the detected card box.
        /// The box isn't always the same shape: on normal pages it ends inside the label strip, but
        /// on the scrolled-to-the-end last page it covered the whole card, so the crop started 5 px
        /// below the top of the digits and the bottom row of fish misread (9 as 4, 3 as 9, ...).
        /// Finding the strip itself doesn't depend on the box.
        /// The label is beige (about 233, 229, 220). A selected card has a pure white frame just
        /// below it, which isn't label: counted as label, it lengthened the band, trimming to
        /// <paramref name="maxRows"/> then cut the top off the digits (259 read as 9).
        /// </remarks>
        public static (int Top, int Bottom)? FindLabelBand(Image<Rgba32> strip, int minRows, int maxRows)
        {
            int x0 = strip.Width / 10, x1 = strip.Width - strip.Width / 10;
            if (x1 <= x0) return null;

            // Rows that are mostly label background; digits cover well under half of a row
            var isLabel = new bool[strip.Height];
            for (int y = 0; y < strip.Height; y++)
            {
                int beige = 0;
                for (int x = x0; x < x1; x++)
                {
                    var p = strip[x, y];
                    int max = Math.Max(p.R, Math.Max(p.G, p.B)), min = Math.Min(p.R, Math.Min(p.G, p.B));
                    // Light and greyish, but not the near-white selection frame
                    if (min >= 190 && min < 245 && max - min <= 35) beige++;
                }
                isLabel[y] = beige * 2 >= x1 - x0;
            }

            // Longest run of label rows, bridging gaps of up to 2 rows
            int bestTop = -1, bestBottom = -1, top = -1, lastLabel = -1;
            for (int y = 0; y < strip.Height; y++)
            {
                if (!isLabel[y]) continue;
                if (top < 0 || y - lastLabel > 3) top = y;
                lastLabel = y;
                if (lastLabel - top > bestBottom - bestTop) { bestTop = top; bestBottom = lastLabel; }
            }

            if (bestTop < 0 || bestBottom - bestTop + 1 < minRows) return null;

            // A light icon can run into the label from above; the label is at the bottom of the card
            if (bestBottom - bestTop + 1 > maxRows) bestTop = bestBottom - maxRows + 1;

            return (bestTop, bestBottom);
        }

        /// <summary>
        /// OCRs a count crop at several scales and votes. This is the reading that used to live in
        /// <c>MaterialScraper.ScanMaterialCount</c>, except that it starts at 2x.
        /// </summary>
        /// <remarks>
        /// It used to start at 1x. At 1x and 1.5x a 1080p digit is about 10 px tall, and Tesseract
        /// read a "2" as "F4" and then "4"; two matching readings settle a count, so it returned 4.
        /// From 2x it read "2" every time. Starting at 2x read all 371 counts of a full Character
        /// Development Items scan correctly (1x: 2 wrong). The digits-only whitelist did worse
        /// (2 and 3 wrong), so it isn't used.
        /// </remarks>
        public static int ReadCount(Bitmap bm, NLog.Logger logger = null) => ReadCount(bm, out _, logger);

        /// <summary>
        /// As <see cref="ReadCount(Bitmap, NLog.Logger)"/>; <paramref name="confident"/> is false
        /// when no reading was trustworthy (no two scales agreed and no clean multi-digit reading)
        /// and the count is the vote's best guess, which can be far off: unreadable "&lt;0&quot;"
        /// and "&lt;9&quot;" once gave 9 for 259. Callers should flag such counts.
        /// </summary>
        public static int ReadCount(Bitmap bm, out bool confident, NLog.Logger logger = null)
        {
            confident = true;
            Dictionary<int, int> counts = new Dictionary<int, int>();
            var rRange = new IntRange(0, 150);
            var bRange = new IntRange(0, 150);
            var gRange = new IntRange(0, 150);

            for (var scale = 2.0; scale <= 3; scale += 0.5)
            {
                using (Bitmap rescaled = GenshinProcesor.ResizeImage(bm, (int)(bm.Width * scale), (int)(bm.Height * scale)))
                {
                    Bitmap copy = (Bitmap)rescaled.Clone();
                    GenshinProcesor.FilterColors(ref copy, rRange, gRange, bRange);

                    Bitmap n = GenshinProcesor.ConvertToGrayscale(copy);
                    GenshinProcesor.SetInvert(ref n);

                    string original = GenshinProcesor.AnalyzeText(n).Trim();

                    n.Dispose();
                    copy.Dispose();

                    // Parse: clean integer first; otherwise strip junk chars and retry.
                    bool cleanParse = int.TryParse(original, out int val);
                    var cleaned = original;
                    if (!cleanParse)
                    {
                        cleaned = cleaned.Replace("M", "111");
                        cleaned = Regex.Replace(cleaned, @"[^0-9]", string.Empty);
                        int.TryParse(cleaned, out val);
                    }

                    logger?.Debug($"Scanned: {original} -> Regex: {cleaned} -> Parsed: {val}");

                    // Vote: track every parse; short-circuit on confidence.
                    counts[val] = counts.TryGetValue(val, out var existing) ? existing + 1 : 1;

                    // Confidence rules — return early only when sure:
                    // (a) two matching parses across scales, OR
                    // (b) one clean multi-digit parse (single-digit values like "1" need a
                    //     confirming scale because Tesseract often collapses "11" -> "1").
                    if (val != 0 && (counts[val] >= 2 || (cleanParse && val >= 10)))
                        return val;
                }
            }

            // No reading settled it: a best guess from here on
            confident = false;
            var nullableMode = SafeExtractMaxCounter(counts);
            if (nullableMode == null)
                return 1;

            var mode = nullableMode.Value;
            if (mode.Key == 0 && counts.Count >= 5)
                return 1;

            counts.Remove(mode.Key);
            return SafeExtractMaxCounter(counts)?.Key ?? 1;
        }

        private static KeyValuePair<int, int>? SafeExtractMaxCounter(Dictionary<int, int> counts)
        {
            // Ties go to the later entry, as before
            return counts.Count == 0 ? null : (KeyValuePair<int, int>?)counts.Aggregate((l, r) => l.Value > r.Value ? l : r);
        }
    }
}
