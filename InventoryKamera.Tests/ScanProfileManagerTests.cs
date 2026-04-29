using System;
using System.IO;
using InventoryKamera.Configuration;
using Xunit;

namespace InventoryKamera.Tests
{
    public class ScanProfileManagerTests
    {
        private static string ProfilePath =>
            Path.Combine(AppContext.BaseDirectory, "inventorylists", "ScanProfile.json");

        [Fact]
        public void Load_RealProfileFile_PopulatesActiveProfile()
        {
            var mgr = new ScanProfileManager();
            mgr.Load(ProfilePath, 16.0 / 9.0);

            Assert.True(mgr.IsLoaded);
            Assert.NotNull(mgr.ActiveProfile);
            Assert.NotNull(mgr.ActiveProfile.Artifacts);
            Assert.NotNull(mgr.ActiveProfile.Weapons);
        }

        [Fact]
        public void Load_16x9AspectRatio_Picks16x9Profile()
        {
            var mgr = new ScanProfileManager();
            mgr.Load(ProfilePath, 16.0 / 9.0);

            Assert.Equal("16:9", mgr.ActiveProfile.Name);
            Assert.Equal(1.7778, mgr.ActiveProfile.AspectRatio, 4);
        }

        [Fact]
        public void Load_16x10AspectRatio_Picks16x10Profile()
        {
            var mgr = new ScanProfileManager();
            mgr.Load(ProfilePath, 16.0 / 10.0);

            Assert.Equal("16:10", mgr.ActiveProfile.Name);
            Assert.Equal(1.6, mgr.ActiveProfile.AspectRatio, 4);
        }

        [Fact]
        public void Load_BetweenRatios_PicksClosestProfile()
        {
            var mgr = new ScanProfileManager();
            // 1.70 is closer to 1.6 (16:10) than to 1.7778 (16:9): |1.7-1.6|=0.1 vs |1.7-1.7778|=0.0778
            // So actually 16:9 wins at 1.70. Use 1.65 instead — closer to 16:10.
            mgr.Load(ProfilePath, 1.65);

            Assert.Equal("16:10", mgr.ActiveProfile.Name);
        }

        [Fact]
        public void Load_16x9_ArtifactSubstatsRegionMatchesScraperConstants()
        {
            // Regression test: ScanProfile.json artifact substats region for 16:9
            // must match the values previously hardcoded in ArtifactScraper.GetSubstatsBitmap
            // (x=0.0911, y=0.4216, w=0.8097, h=0.1841). If these drift, every 16:9 substats
            // OCR breaks silently.
            var mgr = new ScanProfileManager();
            mgr.Load(ProfilePath, 16.0 / 9.0);

            var r = mgr.ActiveProfile.Artifacts.Substats;
            Assert.Equal(0.0911, r.X, 4);
            Assert.Equal(0.4216, r.Y, 4);
            Assert.Equal(0.8097, r.W, 4);
            Assert.Equal(0.1841, r.H, 4);
        }

        [Fact]
        public void Load_16x9_WeaponRefinementRegionMatchesScraperConstants()
        {
            var mgr = new ScanProfileManager();
            mgr.Load(ProfilePath, 16.0 / 9.0);

            var r = mgr.ActiveProfile.Weapons.Refinement;
            Assert.Equal(0.061, r.X, 3);
            Assert.Equal(0.421, r.Y, 3);
            Assert.Equal(0.065, r.W, 3);
            Assert.Equal(0.033, r.H, 3);
        }

        [Fact]
        public void Load_NonExistentFile_Throws()
        {
            var mgr = new ScanProfileManager();
            Assert.Throws<FileNotFoundException>(() =>
                mgr.Load("definitely_does_not_exist_12345.json", 1.7778));
        }

        [Fact]
        public void IsLoaded_BeforeLoad_ReturnsFalse()
        {
            var mgr = new ScanProfileManager();
            Assert.False(mgr.IsLoaded);
            Assert.Null(mgr.ActiveProfile);
        }
    }
}
