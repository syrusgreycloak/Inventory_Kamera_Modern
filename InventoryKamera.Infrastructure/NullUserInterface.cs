using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// No-op implementation of IUserInterface used when no UI form is available
    /// (e.g., during static initialization before the main form is shown).
    /// </summary>
    internal class NullUserInterface : IUserInterface
    {
        public void SetGear(Image<Rgba32> bm, Weapon weapon) { }
        public void SetGear(Image<Rgba32> bm, Artifact artifact) { }
        public void SetGearPictureBox(Image<Rgba32> bm) { }
        public void SetGearTextBox(string text) { }

        public void SetCharacter_NameAndElement(Image<Rgba32> bm, string name, string element) { }
        public void SetCharacter_Level(Image<Rgba32> bm, int level, int maxLevel) { }
        public void SetCharacter_Constellation(int level) { }
        public void SetCharacter_Talent(Image<Rgba32> bm, string text, int i) { }

        public void SetMora(Image<Rgba32> mora, int count) { }

        public void SetWeapon_Max(int value) { }
        public void SetArtifact_Max(int value) { }
        public void IncrementWeaponCount() { }
        public void IncrementArtifactCount() { }
        public void IncrementCharacterCount() { }

        public void SetProgramStatus(string status, bool ok = true) { }
        public void AddError(string error) { }
        public void SetMainCharacterName(string name) { }

        public void SetNavigation_Image(Image<Rgba32> bm) { }

        public void ResetGearDisplay() { }
        public void ResetCharacterDisplay() { }
        public void ResetCounters() { }
        public void ResetErrors() { }
        public void ResetAll() { }
    }
}
