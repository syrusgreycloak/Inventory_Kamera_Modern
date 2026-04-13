using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// Infrastructure wrapper around the Core GameDataService.
    /// Implements IGameDataService for injection into scrapers.
    /// </summary>
    internal class InfraGameDataService : IGameDataService
    {
        private readonly GameDataService _core;

        public InfraGameDataService(GameDataService core)
        {
            _core = core;
        }

        public Dictionary<string, string> Weapons => _core.Weapons;
        public Dictionary<string, string> Materials => _core.Materials;
        public Dictionary<string, string> DevItems => _core.DevItems;
        public Dictionary<string, JObject> Characters => _core.Characters;
        public Dictionary<string, JObject> Artifacts => _core.Artifacts;
        public Dictionary<string, string> Stats => _core.Stats;
        public Dictionary<string, string> Elements => _core.Elements;
        public List<string> GearSlots => _core.GearSlots;
        public HashSet<string> EnhancementMaterials => _core.EnhancementMaterials;

        public void ReloadData() => _core.ReloadData();
        public void UpdateCharacterName(string t, string n) => _core.UpdateCharacterName(t, n);

        public bool IsValidWeapon(string s) => _core.IsValidWeapon(s);
        public bool IsValidSetName(string s) => _core.IsValidSetName(s);
        public bool IsValidStat(string s) => _core.IsValidStat(s);
        public bool IsValidSlot(string s) => _core.IsValidSlot(s);
        public bool IsValidCharacter(string s) => _core.IsValidCharacter(s);
        public bool IsValidElement(string s) => _core.IsValidElement(s);
        public bool IsEnhancementMaterial(string s) => _core.IsEnhancementMaterial(s);
        public bool IsValidMaterial(string s) => _core.IsValidMaterial(s);

        public string FindClosestWeapon(string s, int c = 90) => _core.FindClosestWeapon(s, c);
        public string FindClosestCharacterName(string s, int c = 90) => _core.FindClosestCharacterName(s, c);
        public string FindClosestSetName(string s, int c = 90) => _core.FindClosestSetName(s, c);
        public string FindClosestArtifactSetFromArtifactName(string s, int c = 90) => _core.FindClosestArtifactSetFromArtifactName(s, c);
        public string FindClosestGearSlot(string s) => _core.FindClosestGearSlot(s);
        public string FindClosestStat(string s, int c = 90) => _core.FindClosestStat(s, c);
        public string FindElementByName(string s, int c = 90) => _core.FindElementByName(s, c);
        public string FindClosestDevelopmentName(string s, int c = 90) => _core.FindClosestDevelopmentName(s, c);
        public string FindClosestMaterialName(string s, int c = 90) => _core.FindClosestMaterialName(s, c);

        public bool CharacterMatchesElement(string n, string e) => _core.CharacterMatchesElement(n, e);
        public string GetElementForCharacter(string n) => _core.GetElementForCharacter(n);
        public List<string> GetCharactersElements(string n) => _core.GetCharactersElements(n);
    }
}
