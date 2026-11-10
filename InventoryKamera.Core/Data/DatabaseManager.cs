using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;


namespace InventoryKamera
{
    public class DatabaseManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string _listdir = Path.Combine(AppContext.BaseDirectory, "inventorylists") + Path.DirectorySeparatorChar;

        public string ListsDir
        {
            get
            {
                Directory.CreateDirectory(_listdir);
                return _listdir;
            }
            set { _listdir = value; }
        }

        private const string WeaponsJson = "weapons.json";
        private const string ArtifactsJson = "artifacts.json";
        private const string CharactersJson = "characters.json";
        private const string MaterialsJson = "materials.json";

        private readonly string NewVersion = "version.txt";

        // Dimbreath/AnimeGameData (project 53216109) was deprecated at game version 6.6 after hitting the
        // GitLab repository size limit; data from 7.0 on is published in Dimbreath/animegamedata2 (branch "main").
        private const string commitsAPIURL = "https://gitlab.com/api/v4/projects/83871005/repository/commits";
        private const string repoBaseURL = "https://gitlab.com/Dimbreath/animegamedata2/-/raw/main/";
        private const string TextMapEnURL = repoBaseURL + "TextMap/TextMapEN.json";
        private const string TextMapMediumEnURL = repoBaseURL + "TextMap/TextMap_MediumEN.json";
        private const string CharactersURL = repoBaseURL + "ExcelBinOutput/AvatarExcelConfigData.json";
        private const string ConstellationsURL = repoBaseURL + "ExcelBinOutput/FetterInfoExcelConfigData.json";
        private const string TalentsURL = repoBaseURL + "ExcelBinOutput/AvatarTalentExcelConfigData.json";
        private const string SkillsURL = repoBaseURL + "ExcelBinOutput/AvatarSkillExcelConfigData.json";
        private const string SkillDepotsURL = repoBaseURL + "ExcelBinOutput/AvatarSkillDepotExcelConfigData.json";

        private const string ArtifactsDisplayItemURL = repoBaseURL + "ExcelBinOutput/DisplayItemExcelConfigData.json";
        private const string ArtifactsCodex = repoBaseURL + "ExcelBinOutput/ReliquaryCodexExcelConfigData.json";
        private const string ArtifactSetConfig = repoBaseURL + "ExcelBinOutput/ReliquaryExcelConfigData.json";

        private const string WeaponsURL = repoBaseURL + "ExcelBinOutput/WeaponExcelConfigData.json";
        private const string MaterialsURL = repoBaseURL + "ExcelBinOutput/MaterialExcelConfigData.json";

        private ConcurrentDictionary<string, string> Mappings = new ConcurrentDictionary<string, string>();

        public Version LocalVersion = new Version();
        public Version RemoteVersion = new Version();

        private static readonly List<string> elements = new List<string>
        {
            "Pyro",
            "Hydro",
            "Dendro",
            "Electro",
            "Anemo",
            "Cryo",
            "Geo",
        };

        private static readonly List<string> slots = new List<string>
        {
            "flower of life",
            "plume of death",
            "sands of eon",
            "goblet of eonothem",
            "circlet of logos",
        };

        public DatabaseManager()
        {
            Directory.CreateDirectory(ListsDir);


            if (!File.Exists(ListsDir + NewVersion)) File.WriteAllText(ListsDir + NewVersion, new Version().ToString());


            LocalVersion = new Version(File.ReadAllText(ListsDir + NewVersion));
        }

        public bool UpdateAvailable()
        {
            Logger.Info("Checking for newer game data...");
            RemoteVersion = CheckRemoteVersion();
            return RemoteVersion == null
                ? throw new NullReferenceException("Could not get current Genshin version.")
                : RemoteVersion.CompareTo(LocalVersion) > 0;
        }

        private Version CheckRemoteVersion()
        {
            {
                string pattern = @"(\d+\.\d+\.\d+)";

                string body = DownloadStringWithRetry(commitsAPIURL);
                var commits = JsonConvert.DeserializeObject<List<JObject>>(body);
                foreach (var commit in commits)
                {
                    var c = commit.ToObject<Dictionary<string, object>>();
                    var match = Regex.Match(c["title"].ToString(), pattern);
                    if (match.Success)
                    {
                        var v = match.Groups[1].Value;
                        Debug.WriteLine("Latest version found {0}", v);
                        return new Version(v);
                    }
                }
            }
            return null;
        }

        public Dictionary<string, JObject> LoadCharacters()
        {
            return GetList(ListType.Characters).ToObject<Dictionary<string, JObject>>();
        }

        public Dictionary<string, string> LoadWeapons()
        {
            return GetList(ListType.Weapons).ToObject<Dictionary<string, string>>();
        }

        public Dictionary<string, JObject> LoadArtifacts()
        {
            return GetList(ListType.Artifacts).ToObject<Dictionary<string, JObject>>();
        }

        public Dictionary<string, string> LoadMaterials()
        {
            return GetList(ListType.Materials).ToObject<Dictionary<string, string>>();
        }

        public Dictionary<string, string> LoadDevItems()
        {
            return GetList(ListType.CharacterDevelopmentItems).ToObject<Dictionary<string, string>>();
        }

        private JToken GetList(ListType list)
        {
            string file = "";
            switch (list)
            {
                case ListType.Weapons:
                    file = WeaponsJson;
                    break;

                case ListType.Artifacts:
                    file = ArtifactsJson;
                    break;

                case ListType.Characters:
                    file = CharactersJson;
                    break;

                case ListType.CharacterDevelopmentItems:
                case ListType.Materials:
                    file = MaterialsJson;
                    break;

                default:
                    break;
            }

            if (!File.Exists(ListsDir + file)) throw new FileNotFoundException($"Data file does not exist for {list}.");
            string json = LoadJsonFromFile(file);
            return json == "{}"
                ? throw new FormatException($"Data file for {list} is invalid. Please try running the auto updater and try again.")
                : JToken.Parse(json);
        }

        public UpdateStatus UpdateGameData(bool force = false)
        {
            try
            {
                UpdateStatus overallStatus = UpdateStatus.Skipped;
                var statusLock = new object();
                LastUpdateRateLimited = false;
                LastUpdateCancelled = false;
                rateLimitDecision = RateLimitDecision.NotAsked;
                LoadMappings();

                if (force)
                {
                    Logger.Info("Forcing update for game data");
                }
                // Always probe the remote version — the post-update version write
                // below requires a non-default RemoteVersion to update LocalVersion,
                // and a non-forced update path otherwise leaves it as 0.0.
                RemoteVersion = CheckRemoteVersion() ?? RemoteVersion;

                var lists = Enum.GetValues(typeof(ListType)).Cast<ListType>().ToList();

                lists.RemoveAll(e => e == ListType.CharacterDevelopmentItems);

                lists.AsParallel().ForAll(e =>
                {
                    var status = UpdateList(e, force);

                    lock (statusLock) 
                    {
                        switch (status)
                        {
                            case UpdateStatus.Fail:
                                overallStatus = UpdateStatus.Fail;
                                Logger.Error("Failed to update {0} data", e);
                                break;
                            case UpdateStatus.Success:
                                if (overallStatus != UpdateStatus.Fail)
                                    overallStatus = UpdateStatus.Success;
                                break;
                            default:
                                if (overallStatus == UpdateStatus.Skipped)
                                    overallStatus = UpdateStatus.Skipped;
                                break;
                        }
                    }
                });

                if (overallStatus != UpdateStatus.Fail)
                {

                    if (RemoteVersion != new Version())
                    {
                        LocalVersion = RemoteVersion;
                        File.WriteAllText(ListsDir + NewVersion, LocalVersion.ToString());
                    }
                }
                else
                    Logger.Error($"Could not update all information for version {RemoteVersion}");

                ReleaseMappings();
                return overallStatus;
            }
            catch (Exception e)
            {
                ReleaseMappings();
                Logger.Error(e);
                return UpdateStatus.Fail;
            }
            
        }

        private void LoadMappings()
        {
            lock (Mappings)
            {
                if (!Mappings.Any())
                {
                    // Load main TextMap
                    var mapping = JObject.Parse(LoadJsonFromURLAsync(TextMapEnURL))
                        .ToObject<Dictionary<string, string>>()
                        .Where(e => !string.IsNullOrWhiteSpace(e.Value)) // Remove any mapping with empty
                        .ToDictionary(i => i.Key, i => i.Value);

                    // Load TextMap_MediumEN (added by Dimbreath to split large file)
                    var mediumMapping = JObject.Parse(LoadJsonFromURLAsync(TextMapMediumEnURL))
                        .ToObject<Dictionary<string, string>>()
                        .Where(e => !string.IsNullOrWhiteSpace(e.Value)) // Remove any mapping with empty
                        .ToDictionary(i => i.Key, i => i.Value);

                    // Merge medium mappings into main mapping
                    foreach (var entry in mediumMapping)
                    {
                        mapping[entry.Key] = entry.Value; // Should be no overlap
                    }

                    Mappings = new ConcurrentDictionary<string, string>(mapping);
                }
            }
        }

        private void ReleaseMappings()
        {
            Mappings.Clear();
        }

        private string LoadJsonFromURLAsync(string url) => DownloadStringWithRetry(url);

        // TextMapEN is ~55 MB, so allow far more than HttpClient's default 100 s timeout.
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        private const int MaxDownloadAttempts = 5;
        private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(60);

        /// <summary>
        /// True if the most recent <see cref="UpdateGameData"/> run failed because GitLab kept
        /// rate-limiting downloads (HTTP 429) after all retries, as opposed to some other error.
        /// </summary>
        public bool LastUpdateRateLimited { get; private set; }

        /// <summary>True if the user chose to stop the most recent update instead of waiting out a rate limit.</summary>
        public bool LastUpdateCancelled { get; private set; }

        /// <summary>
        /// Asked (on a worker thread, at most once per update run) when GitLab first rate-limits a download.
        /// Return true to keep waiting and retrying, false to stop the update. If unset, retries proceed.
        /// </summary>
        public Func<bool> ConfirmWaitForRateLimit { get; set; }

        private enum RateLimitDecision { NotAsked, Wait, Stop }
        private volatile RateLimitDecision rateLimitDecision = RateLimitDecision.NotAsked;
        private readonly object rateLimitLock = new object();

        // Lists download in parallel; the lock makes the first thread to hit a 429 ask the user
        // while the others wait for (and then share) that answer, so only one dialog appears.
        private bool ShouldKeepWaitingForRateLimit()
        {
            lock (rateLimitLock)
            {
                if (rateLimitDecision == RateLimitDecision.NotAsked)
                {
                    bool wait = ConfirmWaitForRateLimit?.Invoke() ?? true;
                    rateLimitDecision = wait ? RateLimitDecision.Wait : RateLimitDecision.Stop;
                    Logger.Info(wait
                        ? "User chose to wait for the GitLab rate limit to clear."
                        : "User chose to stop the update because of the GitLab rate limit.");
                }
                return rateLimitDecision == RateLimitDecision.Wait;
            }
        }

        private void ThrowIfUpdateStopped()
        {
            if (rateLimitDecision == RateLimitDecision.Stop)
            {
                LastUpdateCancelled = true;
                throw new OperationCanceledException("Game data update stopped by the user (GitLab rate limit).");
            }
        }

        /// <summary>
        /// Downloads a URL as UTF-8 text. GitLab throttles anonymous downloads with HTTP 429;
        /// those (and 503s) are retried with exponential backoff, honouring Retry-After when given.
        /// </summary>
        private string DownloadStringWithRetry(string url)
        {
            for (int attempt = 1; ; attempt++)
            {
                ThrowIfUpdateStopped();

                using (var response = Http.GetAsync(url).GetAwaiter().GetResult())
                {
                    var code = (int)response.StatusCode;
                    bool retryable = code == 429 || code == 503;

                    if (!retryable)
                    {
                        response.EnsureSuccessStatusCode();
                        var bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        return System.Text.Encoding.UTF8.GetString(bytes);
                    }

                    if (code == 429 && !ShouldKeepWaitingForRateLimit()) ThrowIfUpdateStopped();

                    if (attempt >= MaxDownloadAttempts)
                    {
                        if (code == 429) LastUpdateRateLimited = true;
                        throw new HttpRequestException(
                            $"GitLab returned {code} ({response.ReasonPhrase}) for {url} after {attempt} attempts.");
                    }

                    // Exponential backoff (5, 10, 20, 40 s) with jitter, unless the server says how long to wait
                    var retryAfter = response.Headers.RetryAfter;
                    TimeSpan delay = retryAfter?.Delta
                        ?? (retryAfter?.Date - DateTimeOffset.UtcNow)
                        ?? TimeSpan.FromSeconds(5 * Math.Pow(2, attempt - 1) + new Random().NextDouble() * 2);
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(1);
                    if (delay > MaxRetryDelay) delay = MaxRetryDelay;

                    Logger.Warn("GitLab returned {0} for {1}; retrying in {2:F0}s (attempt {3}/{4}).",
                        code, url, delay.TotalSeconds, attempt + 1, MaxDownloadAttempts);
                    System.Threading.Thread.Sleep(delay);
                }
            }
        }

        private UpdateStatus UpdateList(ListType list, bool force = false)
        {
            UpdateStatus status = UpdateStatus.Success;

            Logger.Info("Updating {0}", list);

            try
            {
                switch (list)
                {
                    case ListType.Weapons:
                        status = UpdateWeapons(force);
                        break;

                    case ListType.Artifacts:
                        status = UpdateArtifacts(force);
                        break;

                    case ListType.Characters:
                        status = UpdateCharacters(force);
                        break;

                    case ListType.Materials:
                        status = UpdateMaterials(force);
                        break;

                    default:
                        break;
                }
                Logger.Info("Finished updating {0} ({1})", list, status);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Stopped updating {0} data (user cancelled during GitLab rate limit)", list);
                return UpdateStatus.Fail;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update {0} data", list);
                Logger.Error(e);
                return UpdateStatus.Fail;
            }

            return status;
        }

        private UpdateStatus UpdateCharacters(bool force)
        {
            var status = UpdateStatus.Skipped;

            // Load existing data (or empty dict if file doesn't exist or force mode)
            var data = force
                ? new ConcurrentDictionary<string, JObject>()
                : JToken.Parse(LoadJsonFromFile(CharactersJson)).ToObject<ConcurrentDictionary<string, JObject>>();

            var newData = new ConcurrentDictionary<string, JObject>(data);

            try
            {
                var characters = JArray.Parse(LoadJsonFromURLAsync(CharactersURL)).ToObject<List<JObject>>();
                var constellations = JArray.Parse(LoadJsonFromURLAsync(ConstellationsURL)).ToObject<List<JObject>>();
                var talents = JArray.Parse(LoadJsonFromURLAsync(TalentsURL)).ToObject<List<JObject>>();
                var skills = JArray.Parse(LoadJsonFromURLAsync(SkillsURL)).ToObject<List<JObject>>();
                var skillDepots = JArray.Parse(LoadJsonFromURLAsync(SkillDepotsURL)).ToObject<List<JObject>>();

                // Lookups for resolving constellation order via each character's skill depot
                var skillsById = skills.Where(s => s["id"] != null).GroupBy(s => (int)s["id"]).ToDictionary(g => g.Key, g => g.First());
                var talentsById = talents.Where(t => t["talentId"] != null).GroupBy(t => (int)t["talentId"]).ToDictionary(g => g.Key, g => g.First());
                var depotsById = skillDepots.Where(d => d["id"] != null).GroupBy(d => (int)d["id"]).ToDictionary(g => g.Key, g => g.First());

                characters.RemoveAll(c => !c.ContainsKey("useType") || c["useType"].ToString() != "AVATAR_FORMAL");

                Logger.Debug("Found {0} playable characters available", characters.Count);
                characters.AsParallel().ForAll(character =>
                {
                    // Check if character name mapping exists (skip unreleased characters)
                    string nameHash = character["nameTextMapHash"].ToString();
                    if (!Mappings.ContainsKey(nameHash))
                    {
                        Logger.Warn("Character hash {0} not found in Mappings. It's likely unreleased.", nameHash);
                        return;
                    }

                    string name = Mappings[nameHash].ToString();

                    try
                    {

                        // Match on internal icon names, which are stable across versions (display names are not:
                        // every traveler variant displays as "Traveler"). Note MarionetteNew is Sandrone, who is playable.
                        var iconName = character["iconName"]?.ToString() ?? "";

                        // Non-playable Miliastra Wonderland mannequins (displayed as "Manekin"/"Manekina")
                        if (iconName.Contains("Mannequin")) return;

                        // Both travelers are listed but are essentially identical; keep only PlayerBoy.
                        if (iconName.EndsWith("PlayerGirl")) return;

                        // Since 7.x there is also a crossbow PlayerBoy variant: the gun-based shooter-mode body
                        // (no elements, skill, burst or constellations; its skill depot is empty). Only the
                        // original traveler lists one depot per element.
                        if (iconName.EndsWith("PlayerBoy") && ((character["candSkillDepotIds"] as JArray)?.Count ?? 0) < 2) return;

                        string PascalCase = CultureInfo.GetCultureInfo("en").TextInfo.ToTitleCase(name);
                        string nameGOOD = Regex.Replace(PascalCase, @"[\W]", string.Empty);
                        string nameKey = nameGOOD.ToLower();
                        int characterID = (int)character["id"];

                        // Filter out non-playable characters (NPCs like Columbina, Ineffa)
                        if (characterID > 10000900) return;


                        var value = new JObject();

                        bool travelerMissingConstellationOrder = nameKey == "traveler" &&
                            newData.ContainsKey("traveler") &&
                            !newData["traveler"]["ConstellationOrder"].HasValues;

                        if (!newData.ContainsKey(nameKey) || travelerMissingConstellationOrder)
                        {
                            value.Add("GOOD", nameGOOD);

                            // Some characters have different internal names.
                            // Ex: Jean -> Qin, Yanfei -> Feiyan, etc.
                            name = character["iconName"].ToString().Split('_').Last(); // UI_AvatarIcon_[Qin] -> Qin

                            if (name.ToLower() == "PlayerBoy".ToLower()) // Handle traveler elements separately
                            {
                                var constellationNames = new JArray { "Viator", "Viatrix" };
                                var constellationOrder = new JObject();

                                var playerElements = new Dictionary<string, string>
                                {
                                    { "Electric", "electro" },
                                    { "Fire", "pyro" },
                                    { "Grass", "dendro" },
                                    { "Rock", "geo"},
                                    { "Water", "hydro"},
                                    { "Wind", "anemo"},
                                    { "Ice", "cryo"}
                                };

                                value.Add("Element", new JArray(playerElements.Values));

                                // Each traveler element has its own skill depot, identified by talentStarName
                                // (e.g. "Talent_PlayerBoy_Fire").
                                var travelerDepots = (character["candSkillDepotIds"] as JArray ?? new JArray())
                                    .Select(id => depotsById.TryGetValue((int)id, out var d) ? d : null)
                                    .Where(d => d != null)
                                    .ToList();

                                foreach (var element in playerElements)
                                {
                                    var depot = travelerDepots.FirstOrDefault(d => d["talentStarName"]?.ToString().EndsWith($"_{element.Key}") == true);
                                    var order = depot == null ? null : ResolveConstellationOrder(depot, $"{nameGOOD} ({element.Value})");
                                    if (order == null) continue;
                                    constellationOrder.Add(element.Value, order);
                                }
                                value.Add("ConstellationOrder", constellationOrder);
                            }
                            else // Any other character that isn't traveler
                            {
                                value.Add("ConstellationName", new JArray
                                {
                                    GetConstellationNameFromId(characterID)
                                });

                                if (!depotsById.TryGetValue((int)character["skillDepotId"], out var depot))
                                {
                                    Logger.Warn("Character {0} skill depot {1} not found.", nameGOOD, character["skillDepotId"]);
                                    return;
                                }

                                // Characters without readable constellations (e.g. Aloy) never reach C3, so any order is harmless
                                var constellationOrder = ResolveConstellationOrder(depot, nameGOOD) ?? new JArray { "skill", "burst" };
                                value.Add("ConstellationOrder", constellationOrder);

                                value.Add("Element", new JArray(GetElementFromID(characterID)));
                            }

                            var weaponArchetype = character["weaponType"].ToString();
                            WeaponType weaponType;
                            if (weaponArchetype.Contains("SWORD_ONE_HAND")) weaponType = WeaponType.Sword;
                            else if (weaponArchetype.Contains("CLAYMORE")) weaponType = WeaponType.Claymore;
                            else if (weaponArchetype.Contains("POLE")) weaponType = WeaponType.Polearm;
                            else if (weaponArchetype.Contains("BOW")) weaponType = WeaponType.Bow;
                            else if (weaponArchetype.Contains("CATALYST")) weaponType = WeaponType.Catalyst;
                            else throw new IndexOutOfRangeException($"{name} uses unknown weapon type {weaponArchetype}");

                            value.Add("WeaponType", (int)weaponType);

                            if (travelerMissingConstellationOrder)
                            {
                                newData[nameKey] = value;
                                status = UpdateStatus.Success;
                            }
                            else if (newData.TryAdd(nameKey, value)) status = UpdateStatus.Success;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Problem when generating character information for {0}", name);
                        Logger.Warn(ex);
                    }
                });

                // Determines which talents C3 and C5 boost by reading those constellations' descriptions
                // ("Increases the Level of <talent> by 3") and matching them against the depot's talent names.
                // Returns [c3Talent, c5Talent] using "auto"/"skill"/"burst", or null if it can't be resolved.
                JArray ResolveConstellationOrder(JObject depot, string label)
                {
                    string TextOf(JToken hash) =>
                        hash != null && Mappings.TryGetValue(hash.ToString(), out var text) ? text : "";

                    string SkillName(JToken skillId) =>
                        skillId != null && skillsById.TryGetValue((int)skillId, out var s) ? TextOf(s["nameTextMapHash"]) : "";

                    var depotSkills = depot["skills"] as JArray;
                    var talentNames = new[]
                    {
                        ("auto", SkillName(depotSkills?.ElementAtOrDefault(0))),
                        ("skill", SkillName(depotSkills?.ElementAtOrDefault(1))),
                        ("burst", SkillName(depot["energySkill"])),
                    };

                    string Classify(string description)
                    {
                        foreach (var (key, talentName) in talentNames)
                            if (!string.IsNullOrEmpty(talentName) && description.Contains(talentName)) return key;
                        if (description.Contains("Normal Attack")) return "auto";
                        if (description.Contains("Elemental Skill")) return "skill";
                        if (description.Contains("Elemental Burst")) return "burst";
                        return null;
                    }

                    var depotTalents = depot["talents"] as JArray;
                    JObject Constellation(int index)
                    {
                        var id = depotTalents?.ElementAtOrDefault(index);
                        return id != null && talentsById.TryGetValue((int)id, out var t) ? t : null;
                    }

                    var c3 = Constellation(2);
                    var c5 = Constellation(4);
                    var c3Talent = c3 == null ? null : Classify(TextOf(c3["descTextMapHash"]));
                    var c5Talent = c5 == null ? null : Classify(TextOf(c5["descTextMapHash"]));

                    if (c3Talent == null || c5Talent == null || c3Talent == c5Talent)
                    {
                        Logger.Warn("Could not resolve constellation order for {0} (C3: {1}, C5: {2}).", label, c3Talent ?? "?", c5Talent ?? "?");
                        return null;
                    }

                    return new JArray { c3Talent, c5Talent };
                }

                string GetConstellationNameFromId(int targetID)
                {
                    foreach (var constellation in from constellation in constellations
                                                  where (((int)constellation["avatarId"]) == targetID)
                                                  select constellation)
                    {
                        return Mappings[constellation["avatarConstellationBeforTextMapHash"].ToString()].ToString();
                    }

                    return "";
                }

                string GetElementFromID(int targetID)
                {
                    foreach (var element in from constellation in constellations
                                            where (((int)constellation["avatarId"]) == targetID)
                                            select constellation)
                    {
                        return Mappings[element["avatarVisionBeforTextMapHash"].ToString()].ToString().ToLower();
                    }
                    return "";
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                status = UpdateStatus.Fail;
            }

            // Validate the new data before saving
            if (status == UpdateStatus.Success)
            {
                if (ValidateCharacterData(newData))
                {
                    Logger.Info("Character data validation passed. Saving {0} characters.", newData.Count);
                    SaveJsonToFile(JsonConvert.SerializeObject(new SortedDictionary<string, JObject>(newData)), CharactersJson);
                }
                else
                {
                    Logger.Error("Character data validation failed. Keeping existing data.");
                    status = UpdateStatus.Fail;
                }
            }

            return status;
        }

        private bool ValidateCharacterData(ConcurrentDictionary<string, JObject> data)
        {
            // Sanity checks to prevent saving corrupt/empty data
            if (data == null || data.Count == 0)
            {
                Logger.Error("Validation failed: Character data is null or empty");
                return false;
            }

            // Expect at least 50 characters (as of 6.5.0 there are 90+)
            if (data.Count < 50)
            {
                Logger.Error("Validation failed: Only {0} characters found (expected at least 50)", data.Count);
                return false;
            }

            // Check that critical characters exist (sanity check for complete data)
            string[] criticalCharacters = { "traveler", "amber", "kaeya", "lisa" };
            foreach (var charKey in criticalCharacters)
            {
                if (!data.ContainsKey(charKey))
                {
                    Logger.Error("Validation failed: Missing critical character '{0}'", charKey);
                    return false;
                }
            }

            // Validate structure of a sample character
            foreach (var kvp in data.Take(5))
            {
                var charData = kvp.Value;
                if (!charData.ContainsKey("GOOD") ||
                    !charData.ContainsKey("Element") ||
                    !charData.ContainsKey("WeaponType"))
                {
                    Logger.Error("Validation failed: Character '{0}' missing required fields", kvp.Key);
                    return false;
                }

                // Check ConstellationOrder exists (except for traveler which has element-specific)
                if (kvp.Key != "traveler" && !charData.ContainsKey("ConstellationOrder"))
                {
                    Logger.Error("Validation failed: Character '{0}' missing ConstellationOrder", kvp.Key);
                    return false;
                }
            }

            Logger.Debug("Character data validation passed: {0} characters, all checks OK", data.Count);
            return true;
        }

        private UpdateStatus UpdateArtifacts(bool force)
        {
            var status = UpdateStatus.Skipped;

            // Load existing data (or empty dict if file doesn't exist or force mode)
            var data = force
                ? new ConcurrentDictionary<string, JObject>()
                : JToken.Parse(LoadJsonFromFile(ArtifactsJson)).ToObject<ConcurrentDictionary<string, JObject>>();

            var newData = new ConcurrentDictionary<string, JObject>(data);

            var artifactDisplays = JArray.Parse(LoadJsonFromURLAsync(ArtifactsDisplayItemURL)).ToObject<List<JObject>>();
            artifactDisplays.RemoveAll(a => a.TryGetValue("icon", out var icon) && !icon.ToString().Contains("RelicIcon"));

            var codex = JArray.Parse(LoadJsonFromURLAsync(ArtifactsCodex)).ToObject<List<JObject>>();
            var sets = JArray.Parse(LoadJsonFromURLAsync(ArtifactSetConfig)).ToObject<List<JObject>>();

            artifactDisplays.AsParallel().ForAll(artifactDisplay =>
            {
                if (Mappings.ContainsKey(artifactDisplay["nameTextMapHash"].ToString()))
                {
                    string setName = Mappings[artifactDisplay["nameTextMapHash"].ToString()];                  // Archaic Petra
                    try
                    {
                        var setNamePascalCase = CultureInfo.GetCultureInfo("en").TextInfo.ToTitleCase(setName); // Archaic Petra
                        var setNameGOOD = Regex.Replace(setNamePascalCase, @"[\W]", string.Empty);              // ArchaicPetra
                        var setNameKey = setNameGOOD.ToLower();                                                 // archaicpetra
                        var setNameNormalized = setName;
                        var setID = (int)artifactDisplay["param"];

                        if (!newData.ContainsKey(setNameKey))
                        {
                            foreach (var set in codex)
                            {
                                var artifacts = new JObject();

                                if ((int)set["suitId"] == setID)
                                {
                                    var artifactIDs = new Dictionary<string, JToken>()
                                    {
                                        { "flower", set["flowerId"]},
                                        { "plume", set["leatherId"] },
                                        { "sands" , set["sandId"] },
                                        { "goblet", set["cupId"] },
                                        { "circlet", set["capId"] },
                                    };
                                    
                                    foreach (var artifact in sets.Where(s => artifactIDs.Values.Contains((int)s["id"])))
                                    {
                                        var slot = artifactIDs.First(x => x.Value != null && (int)x.Value == (int)artifact["id"]).Key;
                                        string artifactHash = artifact["nameTextMapHash"].ToString();
                                        // Check if artifact piece mapping exists (skip unreleased artifacts)
                                        if (!Mappings.ContainsKey(artifactHash))
                                        {
                                            Logger.Warn("Artifact piece hash {0} not found in Mappings. It's likely unreleased.", artifactHash);
                                            continue;
                                        }
                                        var artifactName = Mappings[artifactHash];                                  // Goblet of the Sojourner
                                        string artifactNamePascalCase = CultureInfo.GetCultureInfo("en").TextInfo.ToTitleCase(artifactName);  // Goblet Of The Sojourner
                                        string artifactNameGOOD = Regex.Replace(artifactNamePascalCase, @"[\W]", string.Empty);               // GobletOfTheSojourner
                                        string artifactNormalized = artifactNameGOOD.ToLower();                                               // gobletofthesojourner

                                        artifacts.Add(slot, new JObject
                                        {
                                            { "artifactName", artifactName },
                                            { "GOOD", artifactNameGOOD },
                                            { "normalizedName", artifactNormalized },
                                        });
                                    }
                                }

                                if (artifacts.Count < 1) continue;
                                var value = new JObject
                                {
                                    { "setName", setName },
                                    { "GOOD", setNameGOOD },
                                    { "normalizedName", setNameGOOD.ToLower() },
                                    { "artifacts", artifacts }
                                };
                                if (newData.TryAdd(setNameKey, value) && status != UpdateStatus.Fail) status = UpdateStatus.Success;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("An error was encountered when gathering information for the artifact set {0}", setName);
                        Logger.Error(ex);
                        status = UpdateStatus.Fail;
                    }
                }
            });

            // Validate the new data before saving
            if (status == UpdateStatus.Success)
            {
                if (ValidateArtifactData(newData))
                {
                    Logger.Info("Artifact data validation passed. Saving {0} artifact sets.", newData.Count);
                    SaveJsonToFile(JsonConvert.SerializeObject(new SortedDictionary<string, JObject>(newData)), ArtifactsJson);
                }
                else
                {
                    Logger.Error("Artifact data validation failed. Keeping existing data.");
                    status = UpdateStatus.Fail;
                }
            }

            return status;
        }

        private bool ValidateArtifactData(ConcurrentDictionary<string, JObject> data)
        {
            if (data == null || data.Count == 0)
            {
                Logger.Error("Validation failed: Artifact data is null or empty");
                return false;
            }

            // Expect at least 30 artifact sets (as of 6.5.0 there are 40+)
            if (data.Count < 30)
            {
                Logger.Error("Validation failed: Only {0} artifact sets found (expected at least 30)", data.Count);
                return false;
            }

            // Check that critical artifact sets exist
            string[] criticalSets = { "gladiatorsfinale", "wandererstroupe", "noblesseoblige" };
            foreach (var setKey in criticalSets)
            {
                if (!data.ContainsKey(setKey))
                {
                    Logger.Error("Validation failed: Missing critical artifact set '{0}'", setKey);
                    return false;
                }
            }

            // Validate structure of a sample set
            foreach (var kvp in data.Take(3))
            {
                var setData = kvp.Value;
                if (!setData.ContainsKey("GOOD") ||
                    !setData.ContainsKey("artifacts"))
                {
                    Logger.Error("Validation failed: Artifact set '{0}' missing required fields", kvp.Key);
                    return false;
                }
            }

            Logger.Debug("Artifact data validation passed: {0} sets, all checks OK", data.Count);
            return true;
        }

        private UpdateStatus UpdateWeapons(bool force)
        {
            var status = UpdateStatus.Skipped;

            // Load existing data (or empty dict if file doesn't exist or force mode)
            var data = force
                ? new ConcurrentDictionary<string, string>()
                : JToken.Parse(LoadJsonFromFile(WeaponsJson)).ToObject<ConcurrentDictionary<string, string>>();

            var newData = new ConcurrentDictionary<string, string>(data);

            try
            {
                List<JObject> weapons = JArray.Parse(LoadJsonFromURLAsync(WeaponsURL)).ToObject<List<JObject>>();

                weapons.AsParallel().ForAll(weapon =>
                {
                    try
                    {
                        if (Mappings.ContainsKey(weapon["nameTextMapHash"].ToString()))
                        {
                            var name = Mappings[weapon["nameTextMapHash"].ToString()];
                            string PascalCase = CultureInfo.GetCultureInfo("en").TextInfo.ToTitleCase(name); // Dull Blade
                            string nameGOOD = Regex.Replace(PascalCase, @"[\W]", string.Empty);              // DullBlade
                            string nameKey = nameGOOD.ToLower();                                             // dullblade

                            if (newData.TryAdd(nameKey, nameGOOD)) status = UpdateStatus.Success;
                        }
                        else Logger.Warn("Weapon hash {0} not found in Mappings. It's likely unreleased.", weapon["nameTextMapHash"].ToString());
                    }
                    catch (Exception ex) { Logger.Warn(ex, weapon["nameTextMapHash"].ToString()); }
                });
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                status = UpdateStatus.Fail;
            }

            // Validate the new data before saving
            if (status == UpdateStatus.Success)
            {
                if (ValidateWeaponData(newData))
                {
                    Logger.Info("Weapon data validation passed. Saving {0} weapons.", newData.Count);
                    SaveJsonToFile(JsonConvert.SerializeObject(new SortedDictionary<string, string>(newData)), WeaponsJson);
                }
                else
                {
                    Logger.Error("Weapon data validation failed. Keeping existing data.");
                    status = UpdateStatus.Fail;
                }
            }

            return status;
        }

        private bool ValidateWeaponData(ConcurrentDictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                Logger.Error("Validation failed: Weapon data is null or empty");
                return false;
            }

            // Expect at least 100 weapons (as of 6.5.0 there are 150+)
            if (data.Count < 100)
            {
                Logger.Error("Validation failed: Only {0} weapons found (expected at least 100)", data.Count);
                return false;
            }

            // Check that critical weapons exist
            string[] criticalWeapons = { "dullblade", "silversword", "beginnersprotector" };
            foreach (var weaponKey in criticalWeapons)
            {
                if (!data.ContainsKey(weaponKey))
                {
                    Logger.Error("Validation failed: Missing critical weapon '{0}'", weaponKey);
                    return false;
                }
            }

            Logger.Debug("Weapon data validation passed: {0} weapons, all checks OK", data.Count);
            return true;
        }

        private UpdateStatus UpdateMaterials(bool force)
        {
            var status = UpdateStatus.Skipped;

            var materialCategories = new List<string>
            {
                "MATERIAL_EXP_FRUIT",
                "MATERIAL_AVATAR_MATERIAL",
                "MATERIAL_EXCHANGE",
                "MATERIAL_WOOD",
                "MATERIAL_FISH_BAIT",
                "MATERIAL_WEAPON_EXP_STONE",
            };

            // Load existing data (or empty dict if file doesn't exist or force mode)
            var data = force
                ? new ConcurrentDictionary<string, string>()
                : JToken.Parse(LoadJsonFromFile(MaterialsJson)).ToObject<ConcurrentDictionary<string, string>>();

            var newData = new ConcurrentDictionary<string, string>(data);

            var materials = JArray.Parse(LoadJsonFromURLAsync(MaterialsURL)).ToObject<List<JObject>>();
            materials.RemoveAll(material => !(material.TryGetValue("materialType", out var materialType) && materialCategories.Contains(materialType.ToString())));

            materials.AsParallel().ForAll(material =>
            {
                try
                {
                    JToken jToken = material["nameTextMapHash"];
                    if (Mappings.TryGetValue(jToken.ToString(), out var name))
                    {
                        var PascalCase = CultureInfo.GetCultureInfo("en").TextInfo.ToTitleCase(name);
                        var nameGood = Regex.Replace(PascalCase, @"[\W]", string.Empty);
                        var nameKey = nameGood.ToLower();

                        if (newData.TryAdd(nameKey, nameGood) && status != UpdateStatus.Fail) status = UpdateStatus.Success;
                    }
                    else
                    {
                        Logger.Warn("Material hash {0} not found in Mappings.", material["nameTextMapHash"].ToString());
                    }
                }
                catch (Exception ex) { Logger.Warn(ex); }
            });

            // Validate the new data before saving
            if (status == UpdateStatus.Success)
            {
                if (ValidateMaterialData(newData))
                {
                    Logger.Info("Material data validation passed. Saving {0} materials.", newData.Count);
                    SaveJsonToFile(JsonConvert.SerializeObject(new SortedDictionary<string, string>(newData)), MaterialsJson);
                }
                else
                {
                    Logger.Error("Material data validation failed. Keeping existing data.");
                    status = UpdateStatus.Fail;
                }
            }

            return status;
        }

        private bool ValidateMaterialData(ConcurrentDictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                Logger.Error("Validation failed: Material data is null or empty");
                return false;
            }

            // Expect at least 50 materials (as of 6.5.0 there are 100+)
            if (data.Count < 50)
            {
                Logger.Error("Validation failed: Only {0} materials found (expected at least 50)", data.Count);
                return false;
            }

            Logger.Debug("Material data validation passed: {0} materials, all checks OK", data.Count);
            return true;
        }

        private string FetchHTML(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    return client.DownloadString(url);
                }
            }
            catch { throw; }

        }

        private string LoadJsonFromFile(string fileName)
        {
            lock (this)
            {
                try
                {
                    using (StreamReader file = File.OpenText(ListsDir + fileName))
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        return JToken.ReadFrom(reader).ToString();
                    }
                }
                catch (Exception)
                {
                    File.Create(ListsDir + fileName).Close();
                    return "{}";
                }
            }
        }

        private bool SaveJsonToFile(string json, string fileName)
        {
            lock (this)
            {
                try
                {
                    using (StreamWriter file = new StreamWriter(ListsDir + fileName))
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        writer.Formatting = Formatting.Indented;
                        JToken.Parse(json).WriteTo(writer);
                    }
                    return true;
                }
                catch (Exception ex) { Logger.Warn(ex); return false; }
            }
        }

        private string ConvertToGOOD(string text)
        {
            foreach (Match match in Regex.Matches(text, @"&#\d*;"))
            {
                if (int.TryParse(Regex.Replace(match.Value, @"\D", string.Empty), out int dec))
                    text = text.Replace(match.Value, Char.ToString(Convert.ToChar(dec)));

                else
                    Logger.Warn("Failed to parse decimal value {0} from string {1}", match.Value, text);
            }
            var pascal = CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(text);
            return Regex.Replace(pascal, @"[\W]", string.Empty);
        }
    }

    public enum ListType
    {
        Characters,
        Weapons,
        Artifacts,
        CharacterDevelopmentItems,
        Materials,
    }

    public enum UpdateStatus
    {
        Fail,
        Skipped,
        Success,
    }
}
