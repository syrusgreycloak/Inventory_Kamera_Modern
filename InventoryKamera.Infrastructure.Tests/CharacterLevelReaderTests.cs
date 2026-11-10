using InventoryKamera;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class CharacterLevelReaderTests
    {
        /// <summary>OCR text from a real scan (2026-11-10), with the correct reading.</summary>
        [Theory]
        [InlineData("Level 90 / 90", 90, 90, false)]
        [InlineData("Level 60 / 60", 60, 60, false)]
        [InlineData("Level 60 / 70", 60, 70, true)]
        [InlineData("Level 20 / 40", 20, 40, true)]
        [InlineData("Level1 / 20", 1, 20, false)]
        [InlineData("Level 80 / 90", 80, 90, true)]
        // The trailing 0 of the max level misread as brackets (Jahoda, Kaveh)
        [InlineData("Level 60 / 7()", 60, 70, true)]
        [InlineData("Level 20 / 4(", 20, 40, true)]
        // ... or dropped entirely
        [InlineData("Level 90 / 9", 90, 90, false)]
        // A stray bracket after a correct 0 (Mika: 40/400 exported as ascended)
        [InlineData("Level 40 / 40)", 40, 40, false)]
        [InlineData("Level 20 / 40)", 20, 40, true)]
        [InlineData("Level 1 / 20)", 1, 20, false)]
        [InlineData("Level 95 / 95 .", 95, 95, false)]
        [InlineData("Level 95 / 100", 95, 100, true)]
        // Other misreads seen in saved scan logs
        [InlineData("Level 90 / 9C", 90, 90, false)]
        [InlineData("» Level 70 / 7(", 70, 70, false)]
        [InlineData("Level 70 / 7(r", 70, 70, false)]
        [InlineData("Level 50 / 5¢(", 50, 50, false)]
        [InlineData("Level 60 / 6(", 60, 60, false)]
        [InlineData("Level 40 / 4()", 40, 40, false)]
        [InlineData("Level 20 / 4.0", 20, 40, true)]
        [InlineData("Level 60-/ 60", 60, 60, false)]
        [InlineData("- Level 70 / 70) -", 70, 70, false)]
        [InlineData("Level 1/20,", 1, 20, false)]
        public void TryParse_ScanText_ReadsLevelAndAscension(string text, int level, int maxLevel, bool ascended)
        {
            Assert.True(CharacterLevelReader.TryParse(text, out int actualLevel, out int actualMax, out bool actualAscended));
            Assert.Equal(level, actualLevel);
            Assert.Equal(maxLevel, actualMax);
            Assert.Equal(ascended, actualAscended);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Level 6060")]     // no slash: retry
        [InlineData("Level 90 7 90")]  // slash read as 7: retry
        [InlineData("Level 60% 60)")]  // slash read as %: retry
        [InlineData("Level / 90")]     // no level
        [InlineData("Level 80 / 70")]  // max below level: misread, retry
        public void TryParse_Unusable_ReturnsFalse(string text)
        {
            Assert.False(CharacterLevelReader.TryParse(text, out _, out _, out _));
        }
    }
}
