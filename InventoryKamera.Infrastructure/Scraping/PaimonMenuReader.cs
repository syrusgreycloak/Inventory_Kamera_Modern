using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InventoryKamera
{
    /// <summary>Tells whether the Paimon menu is open.</summary>
    /// <remarks>
    /// The scan navigates from the Paimon menu (and reads the Traveler's name there when it isn't
    /// set). Instead of requiring the player to open it, the scan checks for it at the start and
    /// presses Escape once if it isn't open. The check reads the fixed labels in the menu's profile
    /// card ("Adventure Rank", "World Level"): white text that's always there in English, while the
    /// open world shows scenery in that spot. The region avoids the UID, the player name and the
    /// values.
    /// </remarks>
    public static class PaimonMenuReader
    {
        /// <summary>The profile card's labels on a 1920x1080 (16:9) screen.</summary>
        public static readonly Rectangle LabelRegion1080 = new Rectangle(315, 170, 225, 160);

        /// <summary>True if the Paimon menu's profile labels are showing.</summary>
        public static bool IsOpen(Image<Rgba32> screenshot, IOcrEngine ocrEngine)
        {
            string text = ReadLabels(screenshot, ocrEngine, out var crop);
            crop.Dispose();
            return HasMenuLabels(text);
        }

        /// <summary>
        /// The OCR text of the label region, and the region itself (<paramref name="crop"/>, owned by
        /// the caller) for saving when the check fails. The crop contains no UID or name.
        /// </summary>
        public static string ReadLabels(Image<Rgba32> screenshot, IOcrEngine ocrEngine, out Image<Rgba32> crop)
        {
            double sx = screenshot.Width / 1920.0, sy = screenshot.Height / 1080.0;
            var r = LabelRegion1080;
            var region = new Rectangle((int)(r.X * sx), (int)(r.Y * sy), (int)(r.Width * sx), (int)(r.Height * sy));

            crop = screenshot.Clone(c => c.Crop(region));
            using (var mask = CharacterNameReader.WhiteTextMask(crop, 200))
            {
                mask.Mutate(c => c.Resize(mask.Width * 2, mask.Height * 2));
                return ocrEngine.AnalyzeText(mask, PageSegmentationMode.SingleBlock) ?? string.Empty;
            }
        }

        /// <summary>True if OCR text contains one of the profile card's fixed labels.</summary>
        public static bool HasMenuLabels(string text)
        {
            string t = (text ?? string.Empty).ToLowerInvariant().Replace(" ", string.Empty);
            return t.Contains("adventurerank") || t.Contains("worldlevel");
        }
    }
}
