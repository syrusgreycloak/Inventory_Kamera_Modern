using System.IO;
using InventoryKamera;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Infrastructure.Tests
{
    public class ElixirReaderTests
    {
        /// <summary>Whole artifact cards from a 1920x1080 scan.</summary>
        [Theory]
        [InlineData("elixir_goblet.png", true)]   // "Sanctifying Elixir Definition" band under the stars
        [InlineData("regular.png", false)]
        public void IsElixirCrafted_ScannedCard(string file, bool expected)
        {
            using var card = Image.Load<Rgba32>(Path.Combine(System.AppContext.BaseDirectory, "TestData", "Elixir", file));
            Assert.Equal(expected, ElixirReader.IsElixirCrafted(card, isNormal: true));
        }
    }
}
