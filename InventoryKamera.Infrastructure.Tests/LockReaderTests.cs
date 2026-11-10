using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class LockReaderTests
    {
        /// <summary>
        /// Artifact lock crops from a real 1080p scan. "locked_fading" was captured while the lock
        /// icon was still fading in (Fast scan speed): the old single-pixel check read it as unlocked.
        /// "unlocked" is an open padlock with a slight pink tint.
        /// </summary>
        [Theory]
        [InlineData("locked.png", true)]
        [InlineData("locked_fading.png", true)]
        [InlineData("unlocked.png", false)]
        public void IsLocked_SavedCrop(string file, bool expected)
        {
            using var crop = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Locks", file));
            Assert.Equal(expected, LockReader.IsLocked(crop));
        }
    }
}
