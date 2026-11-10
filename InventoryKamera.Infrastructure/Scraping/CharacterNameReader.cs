using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InventoryKamera.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    /// <summary>
    /// OCRs the character name from the right-hand stats panel. Kept separate from
    /// <see cref="CharacterScraper"/> so it can be tested against saved crops.
    /// </summary>
    public static class CharacterNameReader
    {
        /// <summary>
        /// A pixel counts as name text when its darkest channel is at least this bright. The name is
        /// white; coloured backgrounds (Vesna's teal, Xilonen's gold) are bright in some channels but
        /// not all, which is what defeated the old grayscale threshold.
        /// </summary>
        public const int WhiteTextMinChannel = 225;

        /// <summary>
        /// Raw OCR readings of the name crop, best method first: a white-text mask, then the original
        /// grayscale/contrast/threshold pipeline as a fallback. Evaluated lazily, so the fallback only
        /// runs if the caller couldn't match the first reading.
        /// </summary>
        /// <remarks>
        /// Measured on saved crops: the mask read all 101 names the old pipeline read, plus all 89
        /// crops the old pipeline failed on (it returned nothing for most of them).
        /// </remarks>
        public static IEnumerable<string> ReadCandidates(Image<Rgba32> crop, IImageProcessor imageProcessor, IOcrEngine ocrEngine)
        {
            yield return ReadWithWhiteMask(crop, imageProcessor, ocrEngine);
            yield return ReadWithGrayscaleThreshold(crop, imageProcessor, ocrEngine);
        }

        public static string ReadWithWhiteMask(Image<Rgba32> crop, IImageProcessor imageProcessor, IOcrEngine ocrEngine)
        {
            using (var mask = WhiteTextMask(crop, WhiteTextMinChannel))
            using (var resized = imageProcessor.ResizeImage(mask, mask.Width * 2, mask.Height * 2))
            {
                return ocrEngine.AnalyzeText(resized, PageSegmentationMode.SingleLine);
            }
        }

        /// <summary>The original pipeline: grayscale, contrast 60, threshold 200, invert, 2x.</summary>
        public static string ReadWithGrayscaleThreshold(Image<Rgba32> crop, IImageProcessor imageProcessor, IOcrEngine ocrEngine)
        {
            var images = new List<Image<Rgba32>>();
            try
            {
                var n = Track(images, imageProcessor.SetGrayscale(crop));
                n = Track(images, imageProcessor.SetContrast(n, 60));
                n = Track(images, imageProcessor.SetThreshold(n, 200));
                n = Track(images, imageProcessor.SetInvert(n));
                n = Track(images, imageProcessor.ResizeImage(n, n.Width * 2, n.Height * 2));
                return ocrEngine.AnalyzeText(n, PageSegmentationMode.SingleLine);
            }
            finally
            {
                // Some operations return their input, so dispose each distinct image once
                var disposed = new HashSet<Image<Rgba32>>();
                foreach (var image in images)
                    if (disposed.Add(image)) image.Dispose();
            }
        }

        private static Image<Rgba32> Track(List<Image<Rgba32>> images, Image<Rgba32> image)
        {
            images.Add(image);
            return image;
        }

        /// <summary>Black where all three channels are at least <paramref name="minChannel"/>, white elsewhere.</summary>
        public static Image<Rgba32> WhiteTextMask(Image<Rgba32> source, int minChannel)
        {
            var black = new Rgba32(0, 0, 0, 255);
            var white = new Rgba32(255, 255, 255, 255);
            var mask = new Image<Rgba32>(source.Width, source.Height);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    var p = source[x, y];
                    mask[x, y] = Math.Min(p.R, Math.Min(p.G, p.B)) >= minChannel ? black : white;
                }
            }
            return mask;
        }

        /// <summary>In-game names of the Miliastra Wonderland mannequins until the player renames them.</summary>
        public const string DefaultMannequin1Name = "Manekina", DefaultMannequin2Name = "Manekin";

        /// <summary>
        /// Matches a cleaned name reading (lower case, letters and digits only) against the names
        /// the player chose: "Traveler", "Wanderer", "Manequin1" or "Manequin2", or null if it's none
        /// of them. Mannequins also match their in-game default names.
        /// </summary>
        /// <remarks>
        /// Mannequins aren't in the game data (Genshin Optimizer doesn't support them; the scanner
        /// skips them), so they can only be recognised by name here.
        /// </remarks>
        public static string MatchCustomName(string cleanedText, ScanSettings settings)
        {
            if (string.IsNullOrWhiteSpace(cleanedText)) return null;

            bool Is(string name) => !string.IsNullOrWhiteSpace(name) && cleanedText == Clean(name);

            if (Is(settings?.TravelerName)) return "Traveler";
            if (Is(settings?.WandererName)) return "Wanderer";
            if (Is(settings?.Manequin1Name) || Is(DefaultMannequin1Name)) return "Manequin1";
            if (Is(settings?.Manequin2Name) || Is(DefaultMannequin2Name)) return "Manequin2";
            return null;
        }

        /// <summary>Lower case, letters and digits only: the form name readings are compared in.</summary>
        public static string Clean(string name) => Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]", string.Empty);
    }
}
