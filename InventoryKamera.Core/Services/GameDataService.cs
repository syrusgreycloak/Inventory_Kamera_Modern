using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace InventoryKamera
{
    public class GameDataService : IGameDataService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly List<string> _elementNames = new List<string>
        {
            "pyro", "hydro", "dendro", "electro", "anemo", "cryo", "geo",
        };

        public Dictionary<string, string> Weapons { get; private set; }
        public Dictionary<string, string> Materials { get; private set; }
        public Dictionary<string, string> DevItems { get; private set; }
        public Dictionary<string, JObject> Characters { get; private set; }
        public Dictionary<string, JObject> Artifacts { get; private set; }
        public List<string> GearSlots { get; } = new List<string> { "flower", "plume", "sands", "goblet", "circlet" };
        public HashSet<string> EnhancementMaterials { get; } = new HashSet<string>
        {
            "enhancementore", "fineenhancementore", "mysticenhancementore",
            "sanctifyingunction", "sanctifyingessence",
        };

        public Dictionary<string, string> Stats { get; } = new Dictionary<string, string>
        {
            ["hp"] = "hp", ["hp%"] = "hp_", ["atk"] = "atk", ["atk%"] = "atk_",
            ["def"] = "def", ["def%"] = "def_", ["energyrecharge"] = "enerRech_",
            ["elementalmastery"] = "eleMas", ["healingbonus"] = "heal_",
            ["critrate"] = "critRate_", ["critdmg"] = "critDMG_",
            ["physicaldmgbonus"] = "physical_dmg_",
        };

        public Dictionary<string, string> Elements { get; private set; }

        private readonly DatabaseManager _db;

        public GameDataService(DatabaseManager db)
        {
            _db = db;
            Elements = new Dictionary<string, string>();
            foreach (var element in _elementNames)
            {
                Stats[$"{element}dmgbonus"] = $"{element}_dmg_";
                Elements[element] = char.ToUpper(element[0]) + element.Substring(1);
            }
            ReloadData();
            WireModelValidator();
        }

        private void WireModelValidator()
        {
            ModelValidator.IsValidWeapon = IsValidWeapon;
            ModelValidator.IsValidCharacter = IsValidCharacter;
            ModelValidator.IsValidElement = IsValidElement;
            ModelValidator.IsValidSlot = IsValidSlot;
            ModelValidator.IsValidSetName = IsValidSetName;
            ModelValidator.IsValidStat = IsValidStat;
            ModelValidator.MarkWiredUp();
        }

        public void ReloadData()
        {
            Characters = _db.LoadCharacters();
            Artifacts = _db.LoadArtifacts();
            Weapons = _db.LoadWeapons();
            DevItems = _db.LoadDevItems();
            Materials = _db.LoadMaterials();
        }

        public void UpdateCharacterName(string target, string name)
        {
            target = ConvertToGood(target).ToLower();
            name = ConvertToGood(name).ToLower();
            if (target == name) return;
            if (Characters.TryGetValue(name, out _))
                Logger.Error("{0} already exists as a character. This may confuse Kamera for {1}.", name, target);
            if (Characters.TryGetValue(target, out _))
            {
                Characters[target]["CustomName"] = name;
                Logger.Info("Set {0} custom name to {1}", target, name);
            }
            else throw new KeyNotFoundException($"Could not find '{target}' in characters.json");
        }

        // --- Validation ---

        public bool IsValidWeapon(string weapon) =>
            Weapons.ContainsValue(weapon) || Weapons.ContainsKey(weapon.ToLower());

        public bool IsValidSetName(string setName)
        {
            if (Artifacts.TryGetValue(setName, out _) || Artifacts.TryGetValue(setName.ToLower(), out _)) return true;
            foreach (var set in Artifacts.Values)
                foreach (var field in set)
                    if (field.Value?.ToString() == setName) return true;
            return false;
        }

        public bool IsValidStat(string stat) => Stats.ContainsValue(stat);
        public bool IsValidSlot(string gearSlot) => GearSlots.Contains(gearSlot);

        public bool IsValidCharacter(string character) =>
            character.Contains("Traveler") || character == "Wanderer" ||
            character == "Manequin1" || character == "Manequin2" ||
            Characters.ContainsKey(character.ToLower());

        public bool IsValidElement(string element) =>
            Elements.ContainsValue(element) || Elements.ContainsKey(element.ToLower());

        public bool IsEnhancementMaterial(string material) =>
            EnhancementMaterials.Contains(material.ToLower()) ||
            Materials.ContainsValue(material) || Materials.ContainsKey(material.ToLower());

        public bool IsValidMaterial(string name) =>
            Materials.ContainsValue(name) || Materials.ContainsKey(name.ToLower());

        // --- FindClosest* ---

        public string FindClosestWeapon(string name, int minConfidence = 90) =>
            FindClosestInDict(name, Weapons, minConfidence);

        public string FindClosestCharacterName(string name, int minConfidence = 90)
        {
            var temp = new Dictionary<string, JObject>();
            foreach (var character in Characters)
            {
                if (character.Value.TryGetValue("CustomName", out var cn))
                    temp[(string)cn] = character.Value;
                else
                    temp[character.Key] = character.Value;
            }
            return FindClosestInDict(name, temp, minConfidence);
        }

        public string FindClosestSetName(string name, int minConfidence = 90) =>
            FindClosestInDict(name, Artifacts, minConfidence);

        public string FindClosestArtifactSetFromArtifactName(string name, int minConfidence = 90)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            string closestMatch = null;
            double highestConfidence = 0;

            foreach (var artifactSet in Artifacts)
            {
                if (artifactSet.Value["GOOD"] == null || artifactSet.Value["artifacts"] == null) continue;
                string currentSet = artifactSet.Value["GOOD"].ToString();

                foreach (var slot in artifactSet.Value["artifacts"].Values())
                {
                    if (slot["normalizedName"] == null) continue;
                    string artifactName = slot["normalizedName"].ToString();
                    if (artifactName == name) return currentSet;
                    double similarity = StringSimilarity(name, artifactName);
                    if (similarity > minConfidence && similarity > highestConfidence)
                    {
                        highestConfidence = similarity;
                        closestMatch = currentSet;
                    }
                }
            }
            return closestMatch;
        }

        public string FindClosestGearSlot(string input)
        {
            foreach (var slot in GearSlots)
                if (input.Contains(slot)) return slot;
            return input;
        }

        public string FindClosestStat(string stat, int minConfidence = 90) =>
            FindClosestInDict(stat, Stats, minConfidence);

        public string FindElementByName(string name, int minConfidence = 90) =>
            FindClosestInDict(name, Elements, minConfidence);

        public string FindClosestDevelopmentName(string name, int minConfidence = 90)
        {
            string value = FindClosestInDict(name, DevItems, minConfidence);
            return !string.IsNullOrWhiteSpace(value) ? value : FindClosestInDict(name, Materials, minConfidence);
        }

        public string FindClosestMaterialName(string name, int minConfidence = 90)
        {
            string value = FindClosestInDict(name, Materials, minConfidence);
            return !string.IsNullOrWhiteSpace(value) ? value : FindClosestInDict(name, Materials, minConfidence);
        }

        // --- Character helpers ---

        public bool CharacterMatchesElement(string name, string element) =>
            !string.IsNullOrWhiteSpace(name) && GetCharactersElements(name.ToLower())?.Contains(element.ToLower()) == true;

        public string GetElementForCharacter(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            if (Characters.TryGetValue(name.ToLower(), out var data))
            {
                var elementsList = data["Element"].ToObject<List<string>>();
                return elementsList?.Count > 0 ? elementsList[0] : "";
            }
            return "";
        }

        public List<string> GetCharactersElements(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<string>();
            if (Characters.TryGetValue(name.ToLower(), out var data))
                return data["Element"].ToObject<List<string>>();
            return new List<string>();
        }

        // --- String matching internals ---

        private string FindClosestInDict(string source, Dictionary<string, string> targets, int minConfidence)
        {
            if (string.IsNullOrWhiteSpace(source)) return "";
            if (targets.TryGetValue(source, out string value)) return value;
            var keys = new HashSet<string>(targets.Keys);
            if (keys.Count(k => k.Contains(source)) == 1) return targets[keys.First(k => k.Contains(source))];
            source = FindClosestInList(source, keys, minConfidence);
            return targets.TryGetValue(source, out value) ? value : source;
        }

        private string FindClosestInDict(string source, Dictionary<string, JObject> targets, int minConfidence)
        {
            if (string.IsNullOrWhiteSpace(source)) return "";
            if (targets.TryGetValue(source, out JObject value)) return (string)value["GOOD"];
            var keys = new HashSet<string>(targets.Keys);
            if (keys.Count(k => k.Contains(source)) == 1) return (string)targets[keys.First(k => k.Contains(source))]["GOOD"];
            source = FindClosestInList(source, keys, minConfidence);
            return targets.TryGetValue(source, out value) ? (string)value["GOOD"] : source;
        }

        private string FindClosestInList(string source, HashSet<string> targets, double minConfidence)
        {
            if (targets.Contains(source)) return source;
            if (string.IsNullOrWhiteSpace(source)) return null;
            string mostSimilar = "";
            double bestScore = 0;
            foreach (var target in targets)
            {
                double score = StringSimilarity(source, target);
                if (score > minConfidence && score > bestScore) { bestScore = score; mostSimilar = target; }
            }
            return mostSimilar;
        }

        private static double StringSimilarity(string s1, string s2)
        {
            int distance = LevenshteinDistance(s1, s2);
            int maxLen = Math.Max(s1.Length, s2.Length);
            return maxLen == 0 ? 100.0 : (1.0 - distance / (double)maxLen) * 100.0;
        }

        private static int LevenshteinDistance(string s1, string s2)
        {
            int m = s1.Length, n = s2.Length;
            int[,] dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = s1[i - 1] == s2[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(Math.Min(dp[i - 1, j], dp[i, j - 1]), dp[i - 1, j - 1]);
            return dp[m, n];
        }

        private static string ConvertToGood(string text)
        {
            text = text.ToLower();
            var pascal = System.Globalization.CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(text);
            return Regex.Replace(pascal, @"[\W]", string.Empty);
        }
    }
}
