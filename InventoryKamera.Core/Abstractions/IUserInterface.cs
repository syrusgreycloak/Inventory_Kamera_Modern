using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace InventoryKamera
{
    public interface IUserInterface
    {
        // Gear (weapons and artifacts)
        void SetGear(Image<Rgba32> bm, Weapon weapon);
        void SetGear(Image<Rgba32> bm, Artifact artifact);
        void SetGearPictureBox(Image<Rgba32> bm);
        void SetGearTextBox(string text);

        // Character
        void SetCharacter_NameAndElement(Image<Rgba32> bm, string name, string element);
        void SetCharacter_Level(Image<Rgba32> bm, int level, int maxLevel);
        void SetCharacter_Constellation(int level);
        void SetCharacter_Talent(Image<Rgba32> bm, string text, int i);

        // Materials / Mora
        void SetMora(Image<Rgba32> mora, int count);

        // Counters / Max
        void SetWeapon_Max(int value);
        void SetArtifact_Max(int value);
        void IncrementWeaponCount();
        void IncrementArtifactCount();
        void IncrementCharacterCount();

        // Status and errors
        void SetProgramStatus(string status, bool ok = true);
        void AddError(string error);
        void SetMainCharacterName(string name);

        // Navigation image
        void SetNavigation_Image(Image<Rgba32> bm);

        // Reset methods
        void ResetGearDisplay();
        void ResetCharacterDisplay();
        void ResetCounters();
        void ResetErrors();
        void ResetAll();
    }
}
