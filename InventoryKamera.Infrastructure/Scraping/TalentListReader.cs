using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InventoryKamera
{
    /// <summary>
    /// Reads the three combat talent levels from one screenshot of the Talents tab, instead of
    /// clicking each talent and reading its detail page (~2 s per character).
    /// </summary>
    /// <remarks>
    /// The tab lists each talent with "Lv. N" right-aligned under its name; levels raised by a
    /// constellation are cyan, the rest white. The level lines sit at fixed heights (long names wrap
    /// upwards), and passives have no level line, so the "Lv." lines in order are auto, skill and
    /// burst, also for Mona and Ayaka, whose sprint talent sits between skill and burst.
    /// Measured on 15 screenshots (every element, with and without cyan levels): each of the three
    /// readings below got all 15 right, but neighbouring settings got 14, because background
    /// sparkles (and Cryo's snowflakes) can land on a digit. Hence three readings that must agree.
    /// </remarks>
    public static class TalentListReader
    {
        /// <summary>The whole talent list on a 1920x1080 (16:9) screen.</summary>
        public static readonly Rectangle WideRegion1080 = new Rectangle(1380, 140, 330, 360);

        /// <summary>Just the right-aligned "Lv." column on a 1920x1080 (16:9) screen.</summary>
        public static readonly Rectangle NarrowRegion1080 = new Rectangle(1580, 140, 130, 360);

        private static readonly Regex LevelPattern =
            new Regex(@"[Ll1I|][vVy][\.:,]?\s*([0-9IJl|]{1,2})\b", RegexOptions.Compiled);

        /// <summary>
        /// Talent levels as shown (auto, skill, burst), including constellation bonuses, or null if
        /// the screenshot can't be trusted: the readings disagree, or don't give exactly three levels
        /// from 1 to 15. Callers should fall back to clicking through the talents.
        /// </summary>
        public static int[] ReadLevels(Image<Rgba32> screenshot, IOcrEngine ocrEngine)
        {
            var readings = new[]
            {
                Read(screenshot, ocrEngine, NarrowRegion1080, minGreenBlue: 200, scale: 2),
                Read(screenshot, ocrEngine, WideRegion1080,   minGreenBlue: 200, scale: 2),
                Read(screenshot, ocrEngine, NarrowRegion1080, minGreenBlue: 190, scale: 3),
            };

            var first = readings[0];
            if (first == null || first.Length != 3 || first.Any(l => l < 1 || l > 15)) return null;
            return readings.All(r => r != null && r.SequenceEqual(first)) ? first : null;
        }

        /// <summary>One reading: crop, keep white and cyan text, enlarge, OCR, parse the "Lv." lines.</summary>
        public static int[] Read(Image<Rgba32> screenshot, IOcrEngine ocrEngine, Rectangle region1080, int minGreenBlue, int scale)
        {
            var region = Scale(region1080, screenshot.Width, screenshot.Height);
            using (var crop = screenshot.Clone(c => c.Crop(region)))
            using (var mask = TextMask(crop, minGreenBlue))
            {
                mask.Mutate(c => c.Resize(mask.Width * scale, mask.Height * scale));
                string text = ocrEngine.AnalyzeText(mask, PageSegmentationMode.SingleBlock);
                return Parse(text);
            }
        }

        /// <summary>The numbers after each "Lv." in the OCR text. I, J, l and | in the number are a misread 1.</summary>
        public static int[] Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<int>();

            var levels = new List<int>();
            foreach (Match m in LevelPattern.Matches(text))
            {
                string digits = Regex.Replace(m.Groups[1].Value, "[IJl|]", "1");
                if (int.TryParse(digits, out int level)) levels.Add(level);
            }
            return levels.ToArray();
        }

        /// <summary>
        /// Black where a pixel is white or cyan text (green and blue at least
        /// <paramref name="minGreenBlue"/>, and red as high or clearly lower), white elsewhere.
        /// Excludes the red/orange of Pyro backgrounds and dim sparkles.
        /// </summary>
        public static Image<Rgba32> TextMask(Image<Rgba32> source, int minGreenBlue)
        {
            var black = new Rgba32(0, 0, 0, 255);
            var white = new Rgba32(255, 255, 255, 255);
            var mask = new Image<Rgba32>(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var p = source[x, y];
                    bool text = Math.Min(p.G, p.B) >= minGreenBlue && (p.R >= minGreenBlue || p.G - p.R >= 60);
                    mask[x, y] = text ? black : white;
                }
            }
            return mask;
        }

        private static Rectangle Scale(Rectangle r, int width, int height)
        {
            double sx = width / 1920.0, sy = height / 1080.0;
            return new Rectangle((int)(r.X * sx), (int)(r.Y * sy), (int)(r.Width * sx), (int)(r.Height * sy));
        }
    }
}
