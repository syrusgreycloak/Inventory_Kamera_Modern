using System;
using System.Collections.Generic;
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
    /// padlock. Brightness is the median over a ring 30-36 px from the node centre (at 1080p), not
    /// the average: a lit star in the background can sit right behind a node, and its glow covers
    /// part of the ring. Keqing's locked 6th node averaged 130 that way (inconclusive), but its
    /// median was 84. Over a full 16:9 scan (94 characters x 6 nodes) the median was at least 177
    /// for activated nodes and at most 84 for locked ones.
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
        public const double ActivatedMinBrightness = 145;

        /// <summary>Ring brightness at or below this means locked. In between is too close to call.</summary>
        public const double LockedMaxBrightness = 115;

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

        /// <summary>Median brightness (max channel) of a ring 30-36 px (at 1080p) from the node centre.</summary>
        public static double RingBrightness(Image<Rgba32> screenshot, (double X, double Y) centre)
        {
            int cx = (int)(centre.X * screenshot.Width);
            int cy = (int)(centre.Y * screenshot.Height);
            double scale = screenshot.Height / 1080.0;
            double inner = 30 * scale, outer = 36 * scale;
            int r = (int)Math.Ceiling(outer);

            var values = new List<int>();
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < inner || d > outer) continue;

                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= screenshot.Width || y >= screenshot.Height) continue;

                    var p = screenshot[x, y];
                    values.Add(Math.Max(p.R, Math.Max(p.G, p.B)));
                }
            }

            if (values.Count == 0) return 0;
            values.Sort();
            return values[values.Count / 2];
        }
    }
}
