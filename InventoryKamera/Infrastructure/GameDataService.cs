using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace InventoryKamera.Infrastructure
{
    internal class GameDataService
    {
        public Dictionary<string, string> Weapons => GenshinProcesor.Weapons;
        public Dictionary<string, string> Materials => GenshinProcesor.Materials;
        public Dictionary<string, string> DevItems => GenshinProcesor.DevItems;
        public Dictionary<string, JObject> Characters => GenshinProcesor.Characters;
        public Dictionary<string, JObject> Artifacts => GenshinProcesor.Artifacts;
        public Dictionary<string, string> Stats => GenshinProcesor.Stats;
        public List<string> GearSlots => GenshinProcesor.gearSlots;
        public HashSet<string> EnhancementMaterials => GenshinProcesor.enhancementMaterials;

        public void ReloadData() => GenshinProcesor.ReloadData();
        public void UpdateCharacterName(string target, string name) => GenshinProcesor.UpdateCharacterName(target, name);
        public void AssignTravelerName(string name) => GenshinProcesor.AssignTravelerName(name);
        public void RestartEngines() => GenshinProcesor.RestartEngines();
    }
}
