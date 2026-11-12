using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InventoryKamera
{
    /// <summary>
    /// Reads which inventory tab is open from its title in the top-left corner ("Weapons",
    /// "Artifacts", "Character Development Items", "Materials").
    /// </summary>
    /// <remarks>
    /// The scan switches tabs directly instead of going back to the Paimon menu between categories;
    /// this confirms the switch worked before a tab is scanned. The title is gold on a dark
    /// background; "Character Development Items" wraps onto a second line. The region stops short
    /// of the white "Q" key icon to the right.
    /// </remarks>
    public static class InventoryTabReader
    {
        /// <summary>The title area on a 1920x1080 (16:9) screen.</summary>
        public static readonly Rectangle TitleRegion1080 = new Rectangle(120, 20, 340, 58);

        /// <summary>The tab whose title is shown, or null if the title can't be read.</summary>
        public static InventoryPage? ReadTab(Image<Rgba32> screenshot, IOcrEngine ocrEngine)
        {
            double sx = screenshot.Width / 1920.0, sy = screenshot.Height / 1080.0;
            var r = TitleRegion1080;
            var region = new Rectangle((int)(r.X * sx), (int)(r.Y * sy), (int)(r.Width * sx), (int)(r.Height * sy));

            using (var crop = screenshot.Clone(c => c.Crop(region)))
            using (var mask = TitleMask(crop))
            {
                mask.Mutate(c => c.Resize(mask.Width * 2, mask.Height * 2));
                return Parse(ocrEngine.AnalyzeText(mask, PageSegmentationMode.SingleBlock));
            }
        }

        /// <summary>The tab named in OCR text, matched on a distinctive word, or null.</summary>
        public static InventoryPage? Parse(string text)
        {
            string t = (text ?? string.Empty).ToLowerInvariant();
            if (t.Contains("weapon")) return InventoryPage.Weapons;
            if (t.Contains("artifact")) return InventoryPage.Artifacts;
            if (t.Contains("development") || t.Contains("character")) return InventoryPage.CharacterDevelopmentItems;
            if (t.Contains("material")) return InventoryPage.Materials;
            return null;
        }

        /// <summary>Black where a pixel is the gold title text (light and warm), white elsewhere.</summary>
        public static Image<Rgba32> TitleMask(Image<Rgba32> source)
        {
            var black = new Rgba32(0, 0, 0, 255);
            var white = new Rgba32(255, 255, 255, 255);
            var mask = new Image<Rgba32>(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var p = source[x, y];
                    // Gold: red and green high, blue clearly lower (excludes the white key icons)
                    bool text = p.R >= 150 && p.G >= 130 && p.R - p.B >= 30;
                    mask[x, y] = text ? black : white;
                }
            }
            return mask;
        }
    }
}
