using System;
using System.Text.RegularExpressions;

namespace InventoryKamera
{
    /// <summary>
    /// Parses the OCR text of the character level line ("Level 60 / 70") into the level and
    /// whether the character has ascended past it.
    /// </summary>
    public static class CharacterLevelReader
    {
        /// <summary>
        /// Parses <paramref name="text"/>. Returns false if it doesn't contain a usable
        /// "level / max level" pair, in which case the caller should capture and try again.
        /// </summary>
        /// <remarks>
        /// Tesseract sometimes reads the trailing 0 of the max level as a bracket: "Level 60 / 7()"
        /// and "Level 20 / 4(" came from a real scan. Dropping those characters left a max level of
        /// 7 or 4, so Jahoda at 60/70 and Kaveh at 20/40 were exported one ascension too low.
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

            // Brackets and the letter O on the max-level side are a misread 0
            string maxText = text.Substring(slash + 1).Replace("()", "0");
            maxText = Regex.Replace(maxText, "[()Oo]", "0");
            maxText = Regex.Replace(maxText, "[^0-9]", string.Empty);

            if (!int.TryParse(levelText, out level) || !int.TryParse(maxText, out maxLevel))
                return false;

            // Max levels are multiples of ten (95 rounds up, as before). A single digit means the
            // trailing 0 was lost entirely ("90/9").
            if (maxLevel < 10) maxLevel *= 10;
            maxLevel = (int)Math.Round(maxLevel / 10.0, MidpointRounding.AwayFromZero) * 10;

            if (level < 1 || maxLevel < level) return false;

            ascended = 20 <= level && level < maxLevel;
            return true;
        }
    }
}
