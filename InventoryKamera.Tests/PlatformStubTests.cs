using System;
using InventoryKamera.Avalonia.Services.PlatformStubs;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Confirms that platform stubs throw <see cref="PlatformNotSupportedException"/>
    /// rather than silently returning default values.
    /// </summary>
    public class PlatformStubTests
    {
        [Fact]
        public void StubScreenCapture_CaptureWindow_Throws_PlatformNotSupportedException()
        {
            var stub = new StubScreenCapture();
            Assert.Throws<PlatformNotSupportedException>(() => stub.CaptureWindow());
        }

        [Fact]
        public void StubInputSimulator_Click_Throws_PlatformNotSupportedException()
        {
            var stub = new StubInputSimulator();
            Assert.Throws<PlatformNotSupportedException>(() => stub.Click());
        }

        [Fact]
        public void StubOcrEngine_AnalyzeText_Throws_PlatformNotSupportedException()
        {
            var stub = new StubOcrEngine();
            Assert.Throws<PlatformNotSupportedException>(() => stub.AnalyzeText(null));
        }

        [Fact]
        public void StubImageProcessor_Crop_Throws_PlatformNotSupportedException()
        {
            var stub = new StubImageProcessor();
            Assert.Throws<PlatformNotSupportedException>(() => stub.Crop(null, default));
        }

        [Fact]
        public void StubGameDataService_Weapons_Property_Throws_PlatformNotSupportedException()
        {
            var stub = new StubGameDataService();
            Assert.Throws<PlatformNotSupportedException>(() => { var _ = stub.Weapons; });
        }
    }
}
