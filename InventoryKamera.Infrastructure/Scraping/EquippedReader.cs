using System;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>
    /// Reads the "Equipped: &lt;name&gt;" bar at the bottom of a weapon or artifact card.
    /// Shared by <see cref="WeaponScraper"/> and <see cref="ArtifactScraper"/>, and kept separate
    /// so it can be tested against saved crops without a scraper or a running game.
    /// </summary>
    public static class EquippedReader
    {
        /// <summary>Background colour of the "Equipped" bar.</summary>
        public static readonly Rgba32 EquippedBarColor = new Rgba32(255, 231, 187, 255);

        /// <summary>
        /// True if the crop shows the Equipped bar. Samples the top-right corner: the top-left can be
        /// covered by the equipped character's icon (e.g. Xinyan's), which made the old (5,5) sample
        /// miss 5 of 524 equipped items in a full scan. The top-right sample missed none.
        /// </summary>
        public static bool IsEquipped(Image<Rgba32> crop, IImageProcessor imageProcessor)
        {
            if (crop.Width < 12 || crop.Height < 6) return false;
            return imageProcessor.CompareColors(EquippedBarColor, crop[crop.Width - 6, 5]);
        }

        /// <summary>
        /// Returns the equipped character's GOOD key, or null if the item isn't equipped or the
        /// name can't be read. <paramref name="matchCharacterName"/> must know the user's custom
        /// Traveler/Wanderer names (use the injected IGameDataService).
        /// </summary>
        public static string Read(Image<Rgba32> crop, IImageProcessor imageProcessor, IOcrEngine ocrEngine,
            Func<string, string> matchCharacterName)
        {
            if (!IsEquipped(crop, imageProcessor)) return null;

            string text;
            using (var gray = imageProcessor.SetGrayscale(crop))
            using (var contrasted = imageProcessor.SetContrast(gray, 60.0))
            {
                text = ocrEngine.AnalyzeText(contrasted);
            }

            string name = ParseName(text);
            if (name == null) return null;

            string match = matchCharacterName(name);
            return string.IsNullOrWhiteSpace(match) ? null : match;
        }

        /// <summary>
        /// Extracts the lower-cased name from OCR text such as "Equipped: SyrusGreycloak", stripping
        /// spaces and punctuation. Returns null if the text isn't an "Equipped:" line.
        /// </summary>
        public static string ParseName(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText)) return null;

            int colon = ocrText.IndexOf(':');
            if (colon < 0 || ocrText.IndexOf("equipped", StringComparison.OrdinalIgnoreCase) < 0) return null;

            string name = Regex.Replace(ocrText.Substring(colon + 1), @"[\W_]", string.Empty).ToLowerInvariant();
            return name.Length == 0 ? null : name;
        }
    }
}
