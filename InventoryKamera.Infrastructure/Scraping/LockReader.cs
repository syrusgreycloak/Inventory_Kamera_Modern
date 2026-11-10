using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>Reads the lock toggle on a weapon or artifact card.</summary>
    public static class LockReader
    {
        /// <summary>Share of red pixels at or above which the padlock counts as locked.</summary>
        public const double MinRedFraction = 0.03;

        /// <summary>
        /// True if the lock crop shows the red (locked) padlock.
        /// </summary>
        /// <remarks>
        /// This used to compare one pixel with the dark frame colour (70, 80, 100) at a tolerance of
        /// 10. On the Fast scan speed the card is sometimes captured while the lock icon is still
        /// fading in, and that pixel is lighter: one scan exported 70 of 392 locked artifacts as
        /// unlocked (the standard exporter shows the same symptom). The red padlock is visible
        /// throughout the fade: locked crops had 194-491 red pixels (9%+), unlocked ones none, over
        /// 684 artifacts and 155 (unlocked) weapons.
        /// </remarks>
        public static bool IsLocked(Image<Rgba32> lockCrop)
        {
            int red = 0, total = lockCrop.Width * lockCrop.Height;
            if (total == 0) return false;

            for (int y = 0; y < lockCrop.Height; y++)
            {
                for (int x = 0; x < lockCrop.Width; x++)
                {
                    var p = lockCrop[x, y];
                    if (p.R > p.G + 50 && p.R > p.B + 40) red++;
                }
            }

            return red >= total * MinRedFraction;
        }
    }
}
