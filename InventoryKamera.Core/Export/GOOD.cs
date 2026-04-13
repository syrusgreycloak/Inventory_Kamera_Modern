using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace InventoryKamera
{
    public class GOOD
    {
        [JsonProperty("format")]
        public string Format { get; private set; }

        [JsonProperty("version")]
        public int Version { get; private set; }

        [JsonProperty("kamera_version")]
        public string AppVersion { get; private set; }

        [JsonProperty("source")]
        public string Source { get; private set; }

        [JsonProperty("weapons", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Weapon> Weapons { get; private set; }

        [JsonProperty("artifacts", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Artifact> Artifacts { get; private set; }

        [JsonProperty("characters", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Character> Characters { get; private set; }

        [JsonProperty("materials", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, int> Materials { get; private set; }

        public GOOD()
        {
            Format = "EMPTY";
            Version = 0;
            Source = "NOT FILLED";
        }

        public GOOD(List<Character> characters, Inventory inventory, bool equipWeapons, bool equipArtifacts) : this()
        {
            // Get rid of VS warning since we are converting this class to JSON
            Format = "GOOD";
            Version = 2;
            AppVersion = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString(3) ?? "unknown";
            Source = "Inventory_Kamera";

            // Assign Characters
            if (characters.Count > 0)
            {
                Characters = new List<Character>(characters);

                if (!equipWeapons)
                {
                    foreach (Character character in Characters) character.Weapon = null;
                }
            }



            // Assign Weapons
            if (inventory.Weapons.Count > 0)
            {
                Weapons = new List<Weapon>(inventory.Weapons);

                if (!equipWeapons)
                {
                    foreach (Weapon weapon in Weapons) weapon.EquippedCharacter = "";
                }
            }

            // Assign Artifacts
            if (inventory.Artifacts.Count > 0)
            {
                Artifacts = new List<Artifact>(inventory.Artifacts);

                if (!equipArtifacts)
                {
                    foreach (Artifact artifact in Artifacts) artifact.EquippedCharacter = "";
                }
            }

            // Assign materials
            if (inventory.AllMaterials.Count > 0)
            {
                Materials = new Dictionary<string, int>();
                inventory.AllMaterials.ToList().ForEach(material => Materials.Add(material.name, material.count));
            }
        }

        internal void WriteToJSON(string outputDirectory)
        {
            // Creates directory if doesn't exist
            Directory.CreateDirectory(outputDirectory);

            // Create file with timestamp in name
            string fileName = $"genshinData_GOOD_{DateTime.Now.ToString("yyyy_MM_dd_HH_mm")}.json";
            string outputFile = Path.Combine(outputDirectory, fileName);

            // Write file
            WriteToJson(outputFile);

            if (!File.Exists(outputFile))
            {
                throw new IOException($"Failed to write GOOD export to: {outputDirectory}");
            }
        }

        private void WriteToJson(string outputFile)
        {
            using (var streamWriter = new StreamWriter(outputFile))
            {
                streamWriter.WriteLine(ToString());
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this).ToString();
        }
    }
}
