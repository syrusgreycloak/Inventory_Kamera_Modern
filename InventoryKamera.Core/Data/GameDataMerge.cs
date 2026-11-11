using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace InventoryKamera
{
    /// <summary>
    /// Merges freshly built game data with the existing (verified) data before it's saved, so an
    /// update never throws away something that was right.
    /// </summary>
    /// <remarks>
    /// Rebuilding from the game data repository can lose information that the shipped reference
    /// data has: a material whose name is missing from the text map is skipped (Vitalized
    /// Dragontooth), a constellation name can be a "???" placeholder (Columbina), and a character
    /// whose C3/C5 descriptions can't be matched gets a default order (Aloy). Entries the new data
    /// lacks are kept, and placeholder or unresolved values keep the existing value. Everything
    /// that was kept is reported, so it can be logged.
    /// </remarks>
    public static class GameDataMerge
    {
        /// <summary>
        /// Adds the existing entries the updated list lacks (weapons, materials, artifact sets).
        /// Returns the keys that were kept.
        /// </summary>
        public static List<string> KeepMissing<T>(IDictionary<string, T> updated, IReadOnlyDictionary<string, T> existing)
        {
            var kept = new List<string>();
            if (existing == null) return kept;

            foreach (var entry in existing)
            {
                if (updated.ContainsKey(entry.Key)) continue;
                updated[entry.Key] = entry.Value;
                kept.Add(entry.Key);
            }
            kept.Sort();
            return kept;
        }

        /// <summary>
        /// Merges characters: keeps characters the update lacks, keeps the existing constellation
        /// name where the new one is a placeholder, and keeps the existing constellation order for
        /// characters in <paramref name="unresolvedOrder"/> (and Traveler elements the update
        /// couldn't resolve). Returns a description of each value kept.
        /// </summary>
        public static List<string> MergeCharacters(
            IDictionary<string, JObject> updated,
            IReadOnlyDictionary<string, JObject> existing,
            ISet<string> unresolvedOrder)
        {
            var kept = new List<string>();
            if (existing == null) return kept;

            foreach (var key in KeepMissing(updated, existing))
                kept.Add($"{key} (missing from the update)");

            foreach (var entry in existing)
            {
                if (!updated.TryGetValue(entry.Key, out var character) || ReferenceEquals(character, entry.Value)) continue;
                var old = entry.Value;

                if (IsPlaceholder(character["ConstellationName"]) && !IsPlaceholder(old["ConstellationName"]))
                {
                    character["ConstellationName"] = old["ConstellationName"].DeepClone();
                    kept.Add($"{entry.Key} constellation name");
                }

                if (character["ConstellationOrder"] is JObject elements && old["ConstellationOrder"] is JObject oldElements)
                {
                    // Traveler: one order per element; elements the update couldn't resolve are absent
                    foreach (var element in oldElements.Properties())
                    {
                        if (elements[element.Name] != null) continue;
                        elements[element.Name] = element.Value.DeepClone();
                        kept.Add($"{entry.Key} {element.Name} constellation order");
                    }
                }
                else if (unresolvedOrder != null && unresolvedOrder.Contains(entry.Key) && old["ConstellationOrder"] is JArray)
                {
                    character["ConstellationOrder"] = old["ConstellationOrder"].DeepClone();
                    kept.Add($"{entry.Key} constellation order");
                }
            }
            return kept;
        }

        /// <summary>True if a text value is missing, empty, or a "???"-style placeholder from the text map.</summary>
        public static bool IsPlaceholder(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null) return true;
            var texts = value is JArray array ? array.Select(t => t?.ToString()) : new[] { value.ToString() };
            return texts.All(t => string.IsNullOrWhiteSpace(t) || t.Trim().All(c => c == '?'));
        }
    }
}
