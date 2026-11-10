using System;
using System.Collections.Generic;
using Bitmap = System.Drawing.Bitmap;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using InventoryKamera.Infrastructure.Scraping; // GenshinProcesor
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SubStat = InventoryKamera.Artifact.SubStat;

namespace InventoryKamera
{
    /// <summary>
    /// Reads an artifact's substats from the substat region of its card, including whether the 4th
    /// substat is unactivated (GOOD v3). Kept separate from <see cref="ArtifactScraper"/> so it can be
    /// tested against saved crops without a scraper or a running game.
    /// </summary>
    public static class ArtifactSubstatReader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A text line whose darkest pixel is lighter than this is drawn in grey, i.e. unactivated.
        /// Measured on a full scan: active substat lines bottom out at luminance 82, unactivated ones
        /// at 156 (264 unactivated vs 96 four-active 5-star artifacts below +4, no overlap).
        /// </summary>
        public const double GreyLineMinLuminance = 120;

        /// <summary>Rows darker than this contain text.</summary>
        private const double InkLuminance = 200;

        /// <summary>
        /// OCRs the substat crop and flags the unactivated substat, either from the OCR text
        /// ("(unactivated)", often cut off for long stat names) or from the line being grey.
        /// Whether the flag is kept is decided by <see cref="Artifact"/> (5-star below +4 only).
        /// </summary>
        public static List<SubStat> Read(Image<Rgba32> crop)
        {
            string text;
            Bitmap bm = GenshinProcesor.ImageSharpToBitmap(crop);
            try
            {
                var oldBm = bm;
                GenshinProcesor.SetBrightness(-30, ref bm);
                if (!ReferenceEquals(oldBm, bm)) oldBm.Dispose();
                // SetContrast mutates in place (LockBits): no ref swap, no dispose guard needed.
                GenshinProcesor.SetContrast(85, ref bm);
                using (var gray = GenshinProcesor.ConvertToGrayscale(bm))
                {
                    text = GenshinProcesor.AnalyzeText(gray, TesseractOCR.Enums.PageSegMode.Auto);
                }
            }
            finally
            {
                bm.Dispose();
            }

            var substats = ParseText(text, name => GenshinProcesor.FindClosestStat(name, 80));

            // The unactivated substat is always the 4th line.
            if (substats.Count >= 4 && IsLineGrey(crop, 3))
            {
                var fourth = substats[3];
                fourth.unactivated = true;
                substats[3] = fourth;
            }

            return substats;
        }

        /// <summary>
        /// Parses OCR text into substats, one per line containing a digit, stopping at the set bonus
        /// text. Marks a substat unactivated if its line contains "unactivated".
        /// </summary>
        public static List<SubStat> ParseText(string ocrText, Func<string, string> findStat)
        {
            var substats = new List<SubStat>();
            if (string.IsNullOrWhiteSpace(ocrText)) return substats;

            var lines = ocrText.ToLowerInvariant().Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

            // Everything from the set bonus description onwards isn't a substat
            int setIndex = lines.FindIndex(l => l.Contains(":") || l.Contains("piece") || l.Contains("set") || l.Contains("2-"));
            if (setIndex >= 0) lines.RemoveRange(setIndex, lines.Count - setIndex);

            foreach (var rawLine in lines)
            {
                var line = Regex.Replace(rawLine, @"(?:^[^a-zA-Z]*)", string.Empty).Replace(" ", string.Empty);
                if (!line.Any(char.IsDigit)) continue;

                Logger.Debug("Parsing artifact substat: {0}", line);

                bool unactivated = line.Contains("unactiv");
                if (unactivated) line = Regex.Replace(line, @"\(?unactiv\w*\)?", string.Empty);

                var match = Regex.Match(line, @"^(.*?)(\d+.*)");
                var stat = Regex.Replace(match.Groups[1].Value, @"[^\w]", string.Empty);
                var value = Regex.Replace(match.Groups[2].Value, @"[^0-9]", string.Empty);

                var substat = new SubStat
                {
                    stat = findStat(line.Contains("%") ? stat + "%" : stat) ?? "",
                    unactivated = unactivated,
                };

                if (!decimal.TryParse(value, out substat.value))
                {
                    Logger.Debug("Failed to parse stat value from: {0}", line);
                    substat.value = -1;
                }

                // Percentage stats are read without the decimal point (e.g. "5.8%" -> 58)
                if (substat.value != -1 && substat.stat.Contains("_")) substat.value /= 10;

                if (string.IsNullOrWhiteSpace(substat.stat) || substat.value == -1)
                    Logger.Debug("Failed to parse stat from: {0}", line);

                substats.Add(substat);
            }

            return substats;
        }

        /// <summary>
        /// True if the given text line (0-based, top to bottom) in the crop is drawn in grey.
        /// Lines are found as runs of rows that contain text-coloured pixels.
        /// </summary>
        public static bool IsLineGrey(Image<Rgba32> crop, int lineIndex)
        {
            var lines = FindTextLines(crop);
            return lineIndex < lines.Count && lines[lineIndex] > GreyLineMinLuminance;
        }

        /// <summary>Darkest luminance of each text line, top to bottom.</summary>
        public static List<double> FindTextLines(Image<Rgba32> crop)
        {
            var lines = new List<double>();
            int start = -1;
            double lineMin = 255;

            for (int y = 0; y <= crop.Height; y++)
            {
                double rowMin = 255;
                if (y < crop.Height)
                {
                    for (int x = 0; x < crop.Width; x++)
                    {
                        var p = crop[x, y];
                        rowMin = Math.Min(rowMin, 0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                    }
                }

                bool ink = y < crop.Height && rowMin < InkLuminance;
                if (ink)
                {
                    if (start < 0) { start = y; lineMin = 255; }
                    lineMin = Math.Min(lineMin, rowMin);
                }
                else if (start >= 0)
                {
                    if (y - start >= 4) lines.Add(lineMin); // ignore specks thinner than a glyph
                    start = -1;
                }
            }

            return lines;
        }
    }
}
