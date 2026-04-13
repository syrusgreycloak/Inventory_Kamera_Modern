using System;
using System.IO;
using Newtonsoft.Json;

namespace InventoryKamera.Configuration
{
    public class ScanProfileManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ScanProfileFile _profileFile;
        private AspectRatioProfile _activeProfile;

        public AspectRatioProfile ActiveProfile => _activeProfile;

        public void Load(string profilePath, double windowAspectRatio)
        {
            try
            {
                string json = File.ReadAllText(profilePath);
                _profileFile = JsonConvert.DeserializeObject<ScanProfileFile>(json);
                _activeProfile = FindBestProfile(windowAspectRatio);
                if (_activeProfile != null)
                    Logger.Info("Loaded scan profile: {0} (aspect ratio {1:F4})", _activeProfile.Name, windowAspectRatio);
                else
                    Logger.Warn("No matching scan profile found for aspect ratio {0:F4}", windowAspectRatio);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load scan profile from {0}", profilePath);
                throw;
            }
        }

        public AspectRatioProfile FindBestProfile(double windowAspectRatio)
        {
            if (_profileFile?.Profiles == null) return null;

            AspectRatioProfile best = null;
            double bestDiff = double.MaxValue;

            foreach (var profile in _profileFile.Profiles.Values)
            {
                double diff = Math.Abs(profile.AspectRatio - windowAspectRatio);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = profile;
                }
            }

            return best;
        }

        /// <summary>Convert a relative coordinate (0.0-1.0) to absolute pixels.</summary>
        public static int ToPixels(double relative, int dimension) => (int)(relative * dimension);
    }
}
