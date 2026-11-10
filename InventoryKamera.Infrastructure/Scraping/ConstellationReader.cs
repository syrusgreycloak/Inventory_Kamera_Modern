using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>
    /// Counts activated constellations from a single screenshot of the constellation screen,
    /// instead of clicking each constellation and checking for the "Activate" button (which took
    /// ~1.6 s per constellation, ~10 s for a C6 character).
    /// </summary>
    /// <remarks>
    /// Activated nodes have a bright, element-coloured frame; locked nodes are dark discs with a
    /// padlock. Measured on a full 16:9 scan (94 characters x 6 nodes) over a ring 30-36 px from the
    /// node centre at 1080p: activated nodes averaged at least 173 brightness, locked at most 102.
    /// </remarks>
    public static class ConstellationReader
    {
        /// <summary>Node centres on a 16:9 constellation screen, as fractions of width and height.</summary>
        public static readonly (double X, double Y)[] NodeCentres16x9 =
        {
            (1520 / 1920.0, 276 / 1080.0),
            (1573 / 1920.0, 387 / 1080.0),
            (1610 / 1920.0, 498 / 1080.0),
            (1610 / 1920.0, 609 / 1080.0),
            (1573 / 1920.0, 720 / 1080.0),
            (1502 / 1920.0, 831 / 1080.0),
        };

        /// <summary>Ring brightness at or above this means activated.</summary>
        public const double ActivatedMinBrightness = 150;

        /// <summary>Ring brightness at or below this means locked. In between is too close to call.</summary>
        public const double LockedMaxBrightness = 125;

        /// <summary>
        /// Number of activated constellations (0-6), or null if the screenshot can't be trusted: a node
        /// in the uncertain brightness band, or an impossible pattern (a locked node before an
        /// activated one). Callers should fall back to clicking through the constellations.
        /// </summary>
        public static int? CountActivated(Image<Rgba32> screenshot)
        {
            int count = 0;
            bool seenLocked = false;

            foreach (var centre in NodeCentres16x9)
            {
                double brightness = RingBrightness(screenshot, centre);

                if (brightness >= ActivatedMinBrightness)
                {
                    if (seenLocked) return null; // constellations unlock in order
                    count++;
                }
                else if (brightness <= LockedMaxBrightness)
                {
                    seenLocked = true;
                }
                else
                {
                    return null;
                }
            }

            return count;
        }

        /// <summary>Average brightness (max channel) of a ring 30-36 px (at 1080p) from the node centre.</summary>
        public static double RingBrightness(Image<Rgba32> screenshot, (double X, double Y) centre)
        {
            int cx = (int)(centre.X * screenshot.Width);
            int cy = (int)(centre.Y * screenshot.Height);
            double scale = screenshot.Height / 1080.0;
            double inner = 30 * scale, outer = 36 * scale;
            int r = (int)Math.Ceiling(outer);

            double sum = 0;
            int count = 0;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < inner || d > outer) continue;

                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= screenshot.Width || y >= screenshot.Height) continue;

                    var p = screenshot[x, y];
                    sum += Math.Max(p.R, Math.Max(p.G, p.B));
                    count++;
                }
            }

            return count == 0 ? 0 : sum / count;
        }
    }
}
