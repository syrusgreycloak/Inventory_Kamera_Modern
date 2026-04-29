using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace InventoryKamera.Imaging
{
    /// <summary>
    /// Small helpers for working with <see cref="Image{TPixel}"/> across the Core library.
    /// </summary>
    internal static class ImageSharpExtensions
    {
        /// <summary>Returns a new cropped copy of <paramref name="src"/> bounded to <paramref name="region"/>.</summary>
        internal static Image<Rgba32> Crop(this Image<Rgba32> src, Rectangle region)
        {
            // Clamp region to source bounds so callers do not need to pre-clip.
            int x = region.X < 0 ? 0 : region.X;
            int y = region.Y < 0 ? 0 : region.Y;
            int w = region.Width;
            int h = region.Height;
            if (x + w > src.Width) w = src.Width - x;
            if (y + h > src.Height) h = src.Height - y;
            if (w <= 0 || h <= 0) return new Image<Rgba32>(1, 1);

            var clone = src.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h)));
            return (Image<Rgba32>)clone;
        }
    }
}
