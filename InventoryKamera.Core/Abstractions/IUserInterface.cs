using System.Drawing;

namespace InventoryKamera
{
    public interface IUserInterface
    {
        // Gear (weapons and artifacts)
        void SetGear(Bitmap bm, Weapon weapon);
        void SetGear(Bitmap bm, Artifact artifact);
        void SetGearPictureBox(Bitmap bm);
        void SetGearTextBox(string text);

        // Character
        void SetCharacter_NameAndElement(Bitmap bm, string name, string element);
        void SetCharacter_Level(Bitmap bm, int level, int maxLevel);
        void SetCharacter_Constellation(int level);
        void SetCharacter_Talent(Bitmap bm, string text, int i);

        // Materials / Mora
        void SetMora(Bitmap mora, int count);

        // Counters / Max
        void SetWeapon_Max(int value);
        void SetArtifact_Max(int value);
        void IncrementWeaponCount();
        void IncrementArtifactCount();
        void IncrementCharacterCount();

        // Status and errors
        void SetProgramStatus(string status, bool ok = true);
        void AddError(string error);

        // Navigation image
        void SetNavigation_Image(Bitmap bm);

        // Reset methods
        void ResetGearDisplay();
        void ResetCharacterDisplay();
        void ResetCounters();
        void ResetErrors();
        void ResetAll();
    }
}
