using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace InventoryKamera.Avalonia.Services.PlatformStubs
{
    /// <summary>
    /// Non-Windows stub for <see cref="IGameDataService"/>.
    /// Every method throws <see cref="PlatformNotSupportedException"/>.
    /// The ScanViewModel platform guard prevents these from being reached at runtime.
    /// </summary>
    public class StubGameDataService : IGameDataService
    {
        private static void Throw() =>
            throw new PlatformNotSupportedException("Scanning requires Windows.");

        public Dictionary<string, string> Weapons         { get { Throw(); return null; } }
        public Dictionary<string, string> Materials       { get { Throw(); return null; } }
        public Dictionary<string, string> DevItems        { get { Throw(); return null; } }
        public Dictionary<string, JObject> Characters     { get { Throw(); return null; } }
        public Dictionary<string, JObject> Artifacts      { get { Throw(); return null; } }
        public Dictionary<string, string> Stats           { get { Throw(); return null; } }
        public Dictionary<string, string> Elements        { get { Throw(); return null; } }
        public List<string> GearSlots                     { get { Throw(); return null; } }
        public HashSet<string> EnhancementMaterials       { get { Throw(); return null; } }

        public void ReloadData()                          { Throw(); }
        public void UpdateCharacterName(string target, string name) { Throw(); }

        public bool IsValidWeapon(string weapon)          { Throw(); return false; }
        public bool IsValidSetName(string setName)        { Throw(); return false; }
        public bool IsValidStat(string stat)              { Throw(); return false; }
        public bool IsValidSlot(string gearSlot)          { Throw(); return false; }
        public bool IsValidCharacter(string character)    { Throw(); return false; }
        public bool IsValidElement(string element)        { Throw(); return false; }
        public bool IsEnhancementMaterial(string material){ Throw(); return false; }
        public bool IsValidMaterial(string name)          { Throw(); return false; }

        public string FindClosestWeapon(string name, int minConfidence = 90)                           { Throw(); return null; }
        public string FindClosestCharacterName(string name, int minConfidence = 90)                    { Throw(); return null; }
        public string FindClosestSetName(string name, int minConfidence = 90)                          { Throw(); return null; }
        public string FindClosestArtifactSetFromArtifactName(string name, int minConfidence = 90)      { Throw(); return null; }
        public string FindClosestGearSlot(string input)                                                { Throw(); return null; }
        public string FindClosestStat(string stat, int minConfidence = 90)                             { Throw(); return null; }
        public string FindElementByName(string name, int minConfidence = 90)                           { Throw(); return null; }
        public string FindClosestDevelopmentName(string name, int minConfidence = 90)                  { Throw(); return null; }
        public string FindClosestMaterialName(string name, int minConfidence = 90)                     { Throw(); return null; }

        public bool CharacterMatchesElement(string name, string element) { Throw(); return false; }
        public string GetElementForCharacter(string name)                { Throw(); return null; }
        public List<string> GetCharactersElements(string name)           { Throw(); return null; }
    }
}
