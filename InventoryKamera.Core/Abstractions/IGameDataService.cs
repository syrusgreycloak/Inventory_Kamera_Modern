using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace InventoryKamera
{
    public interface IGameDataService
    {
        // Data access
        Dictionary<string, string> Weapons { get; }
        Dictionary<string, string> Materials { get; }
        Dictionary<string, string> DevItems { get; }
        Dictionary<string, JObject> Characters { get; }
        Dictionary<string, JObject> Artifacts { get; }
        Dictionary<string, string> Stats { get; }
        List<string> GearSlots { get; }
        HashSet<string> EnhancementMaterials { get; }

        // Lifecycle
        void ReloadData();
        void UpdateCharacterName(string target, string name);

        // Validation
        bool IsValidWeapon(string weapon);
        bool IsValidSetName(string setName);
        bool IsValidStat(string stat);
        bool IsValidSlot(string gearSlot);
        bool IsValidCharacter(string character);
        bool IsValidElement(string element);
        bool IsEnhancementMaterial(string material);
        bool IsValidMaterial(string name);

        // Lookup / fuzzy match
        string FindClosestWeapon(string name, int minConfidence = 90);
        string FindClosestCharacterName(string name, int minConfidence = 90);
        string FindClosestSetName(string name, int minConfidence = 90);
        string FindClosestArtifactSetFromArtifactName(string name, int minConfidence = 90);
        string FindClosestGearSlot(string input);
        string FindClosestStat(string stat, int minConfidence = 90);
        string FindElementByName(string name, int minConfidence = 90);
        string FindClosestDevelopmentName(string name, int minConfidence = 90);
        string FindClosestMaterialName(string name, int minConfidence = 90);

        // Character helpers
        bool CharacterMatchesElement(string name, string element);
        string GetElementForCharacter(string name);
        List<string> GetCharactersElements(string name);
    }
}
