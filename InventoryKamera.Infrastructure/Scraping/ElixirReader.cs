using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>Reads whether an artifact card is elixir-crafted.</summary>
    public static class ElixirReader
    {
        /// <summary>
        /// True if the artifact card shows the purple "Sanctifying Elixir Definition" band under the
        /// stars, which elixir-crafted artifacts have.
        /// </summary>
        /// <remarks>
        /// Looks at the centre of the band, which is always background (the band's text is left of
        /// it). On one account's 684 artifacts this pixel was RGB(220, 192, 255) on all 10
        /// elixir-crafted artifacts and not purple on any other. The band also moves the level,
        /// lock and substats down, so the scraper shifts those regions when this is true.
        /// </remarks>
        /// <param name="card">The whole artifact card.</param>
        /// <param name="isNormal">True for 16:9, false for 16:10.</param>
        public static bool IsElixirCrafted(Image<Rgba32> card, bool isNormal)
        {
            double top = isNormal ? 0.3333 : 0.2941, height = isNormal ? 0.0526 : 0.0470;
            int x = (int)(card.Width * 0.40) + (int)(card.Width * 0.20) / 2;
            int y = (int)(card.Height * top) + (int)(card.Height * height) / 2;

            // Purple: blue highest, then red, then green
            var p = card[x, y];
            return p.B > 150 && p.B > p.R && p.R >= p.G;
        }
    }
}
