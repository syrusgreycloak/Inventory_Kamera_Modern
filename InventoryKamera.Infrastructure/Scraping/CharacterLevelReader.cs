using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace InventoryKamera
{
    /// <summary>
    /// Parses the OCR text of the character level line ("Level 60 / 70") into the level and
    /// whether the character has ascended past it.
    /// </summary>
    public static class CharacterLevelReader
    {
        /// <summary>Every max level the level line can show: the ascension caps, then 95 and 100.</summary>
        public static readonly int[] ValidMaxLevels = { 20, 40, 50, 60, 70, 80, 90, 95, 100 };

        /// <summary>
        /// Parses <paramref name="text"/>. Returns false if it doesn't contain a usable
        /// "level / max level" pair, in which case the caller should capture and try again.
        /// </summary>
        /// <remarks>
        /// Tesseract misreads the max level in both directions. It can read the trailing 0 as a
        /// bracket ("Level 60 / 7()", "Level 20 / 4(") or drop it ("90 / 9"), and it can add a
        /// stray bracket after a correct 0 ("Level 40 / 40)"). So the max level is read several
        /// ways and the first reading that is a real level cap, at or above the level, wins.
        /// </remarks>
        public static bool TryParse(string text, out int level, out int maxLevel, out bool ascended)
        {
            level = 0;
            maxLevel = 0;
            ascended = false;
            if (string.IsNullOrEmpty(text)) return false;

            int slash = text.LastIndexOf('/');
            if (slash < 0) return false;

            string levelText = Regex.Replace(text.Substring(0, slash), "[^0-9]", string.Empty);
            if (!int.TryParse(levelText, out level) || level < 1) return false;

            int lvl = level;
            int? max = MaxLevelReadings(text.Substring(slash + 1))
                .Cast<int?>()
                .FirstOrDefault(m => ValidMaxLevels.Contains(m.Value) && m.Value >= lvl);
            if (max == null) return false;

            maxLevel = max.Value;
            ascended = 20 <= level && level < maxLevel;
            return true;
        }

        /// <summary>Candidate readings of the max-level text, most literal first.</summary>
        private static System.Collections.Generic.IEnumerable<int> MaxLevelReadings(string maxText)
        {
            // Digits only: brackets and other noise dropped ("40)" -> 40)
            string digits = Regex.Replace(maxText, "[^0-9]", string.Empty);
            if (int.TryParse(digits, out int plain))
            {
                yield return plain;
                // Trailing 0 lost entirely ("9" -> 90)
                yield return plain * 10;
            }

            // Brackets and the letter O read as the 0 they replaced ("7()" -> 70, "4(" -> 40)
            string asZero = Regex.Replace(maxText.Replace("()", "0"), "[()Oo]", "0");
            asZero = Regex.Replace(asZero, "[^0-9]", string.Empty);
            if (int.TryParse(asZero, out int bracketed))
                yield return bracketed;
        }
    }
}
