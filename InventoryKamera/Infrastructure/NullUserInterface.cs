using System.Drawing;

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// No-op implementation of IUserInterface used when no UI form is available
    /// (e.g., during static initialization before the main form is shown).
    /// </summary>
    internal class NullUserInterface : IUserInterface
    {
        public void SetGear(Bitmap bm, Weapon weapon) { }
        public void SetGear(Bitmap bm, Artifact artifact) { }
        public void SetGearPictureBox(Bitmap bm) { }
        public void SetGearTextBox(string text) { }

        public void SetCharacter_NameAndElement(Bitmap bm, string name, string element) { }
        public void SetCharacter_Level(Bitmap bm, int level, int maxLevel) { }
        public void SetCharacter_Constellation(int level) { }
        public void SetCharacter_Talent(Bitmap bm, string text, int i) { }

        public void SetMora(Bitmap mora, int count) { }

        public void SetWeapon_Max(int value) { }
        public void SetArtifact_Max(int value) { }
        public void IncrementWeaponCount() { }
        public void IncrementArtifactCount() { }
        public void IncrementCharacterCount() { }

        public void SetProgramStatus(string status, bool ok = true) { }
        public void AddError(string error) { }
        public void SetMainCharacterName(string name) { }

        public void SetNavigation_Image(Bitmap bm) { }

        public void ResetGearDisplay() { }
        public void ResetCharacterDisplay() { }
        public void ResetCounters() { }
        public void ResetErrors() { }
        public void ResetAll() { }
    }
}
