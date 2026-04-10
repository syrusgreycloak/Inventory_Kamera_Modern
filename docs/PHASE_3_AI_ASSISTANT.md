# Phase 3: AI Assistant Integration

**Status:** Planned (Post-Avalonia Migration)
**Dependencies:** Phase 2 Avalonia UI complete, Phase 1.5 ScanProfile.json system
**Goal:** Transform Inventory Kamera from a data extraction tool into an intelligent assistant that helps players make informed decisions about gear, teams, and progression

---

## Overview

Phase 3 adds an AI-powered chat interface that allows users to query their inventory data and receive personalized recommendations based on current game meta information. Instead of just exporting JSON files for external tools, users can directly ask questions like "What's the best build for my Raiden?" and get answers based on their actual inventory.

---

## Problem Statement

### Current Limitations

After scanning, users must:
1. **Export GOOD JSON** to external optimizer tools
2. **Manually compare** optimizer recommendations against game guides
3. **Context switch** between multiple websites/tools
4. **Learn complex tools** (Genshin Optimizer, Frzyc calculator, etc.)
5. **No conversational help** - can't ask follow-up questions

### Target Experience (Phase 3)

Users should be able to:
1. **Scan inventory** as usual
2. **Open AI Assistant tab** in the same app
3. **Ask natural language questions**: "What artifacts should I give Raiden?"
4. **Get personalized answers** based on their actual inventory
5. **Receive meta-aware advice** (current Abyss, team synergies, farming priorities)
6. **Have conversations** with follow-up questions and refinements
7. **Choose their AI provider** (Claude, Gemini, future: local LLMs)

---

## Feature Scope

### Phase 3.1: Database Foundation

**In Scope:**
- SQLite database schema for characters, weapons, artifacts, materials
- Export to SQLite alongside existing GOOD JSON export
- Database viewer tab (read-only grid view of tables)
- Historical scan tracking (optional)
- Database migration from GOOD JSON (for existing users)

**Out of Scope:**
- ❌ Manual editing of database (read-only in UI)
- ❌ Advanced SQL query interface for users
- ❌ Multi-user or cloud sync

### Phase 3.2: AI Chat Interface

**In Scope:**
- AI provider configuration (API key, model selection)
- Chat interface with message history
- Core AI tools: database queries, character details
- Conversation persistence (save/load chat sessions)
- Token usage tracking and cost estimation
- Support for Claude (Anthropic), Gemini (Google), and Ollama APIs

**Out of Scope:**
- ❌ Self-hosted local LLM integration (Phase 4+)
- ❌ Voice input/output
- ❌ Image generation
- ❌ Multi-user chat rooms

### Phase 3.3: Meta Intelligence

**In Scope:**
- Web scraping/fetching for character build guides (game8.com, KeqingMains)
- Artifact set recommendations
- Team composition analysis
- Weapon recommendations
- Basic stat optimization (CR/CD ratios, ER requirements)
- Caching of meta information

**Out of Scope:**
- ❌ Real-time damage calculations (too complex for Phase 3)
- ❌ Automatic game data updates (manual cache refresh only)
- ❌ Community tier lists integration

### Phase 3.4: Advanced Features

**In Scope:**
- Historical progression tracking ("You've gained 15 artifacts since last week")
- Farming priority recommendations
- Quick action buttons ("Optimize Current Team", "Best Builds")
- Export recommendations as text/markdown
- Monthly budget limits for API costs

**Out of Scope:**
- ❌ Automatic artifact locking/unlocking in-game (not possible)
- ❌ Predictive analytics (ML-based progression forecasting)
- ❌ Social features (sharing builds with friends)

---

## Database Schema

### Core Tables

```sql
-- Characters table
CREATE TABLE characters (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    key TEXT NOT NULL,              -- GOOD format key (e.g., "RaidenShogun")
    level INTEGER NOT NULL,
    constellation INTEGER DEFAULT 0,
    ascension INTEGER DEFAULT 0,
    talent_auto INTEGER DEFAULT 1,
    talent_skill INTEGER DEFAULT 1,
    talent_burst INTEGER DEFAULT 1,
    equipped_weapon_id INTEGER,
    scan_timestamp DATETIME NOT NULL,
    FOREIGN KEY (equipped_weapon_id) REFERENCES weapons(id)
);

-- Weapons table
CREATE TABLE weapons (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    key TEXT NOT NULL,              -- GOOD format key (e.g., "EngulfingLightning")
    level INTEGER NOT NULL,
    ascension INTEGER DEFAULT 0,
    refinement INTEGER DEFAULT 1,
    locked BOOLEAN DEFAULT 0,
    location TEXT,                  -- Character key if equipped, empty if unequipped
    rarity INTEGER NOT NULL,
    scan_timestamp DATETIME NOT NULL
);

-- Artifacts table
CREATE TABLE artifacts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    set_key TEXT NOT NULL,          -- GOOD format key (e.g., "EmblemOfSeveredFate")
    slot TEXT NOT NULL,             -- flower, plume, sands, goblet, circlet
    rarity INTEGER NOT NULL,
    level INTEGER NOT NULL,
    main_stat_key TEXT NOT NULL,    -- hp, atk, def, hp_, atk_, def_, eleMas, ener_, etc.
    main_stat_value REAL NOT NULL,
    locked BOOLEAN DEFAULT 0,
    location TEXT,                  -- Character key if equipped
    scan_timestamp DATETIME NOT NULL
);

-- Artifact substats table (one-to-many relationship)
CREATE TABLE artifact_substats (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    artifact_id INTEGER NOT NULL,
    stat_key TEXT NOT NULL,         -- Same keys as main_stat_key
    stat_value REAL NOT NULL,
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id) ON DELETE CASCADE
);

-- Materials table
CREATE TABLE materials (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    key TEXT NOT NULL,              -- Material key (e.g., "CrystalChunk")
    count INTEGER NOT NULL,
    category TEXT,                  -- enhancement, ascension, talent, etc.
    scan_timestamp DATETIME NOT NULL
);

-- Scan history tracking (optional but useful)
CREATE TABLE scan_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    scan_timestamp DATETIME NOT NULL,
    scan_type TEXT NOT NULL,        -- full, weapons_only, artifacts_only, characters_only
    items_scanned INTEGER NOT NULL,
    duration_seconds REAL
);

-- AI conversation history
CREATE TABLE chat_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_name TEXT,
    created_timestamp DATETIME NOT NULL,
    last_message_timestamp DATETIME NOT NULL
);

CREATE TABLE chat_messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id INTEGER NOT NULL,
    role TEXT NOT NULL,             -- user, assistant, system
    content TEXT NOT NULL,
    timestamp DATETIME NOT NULL,
    tokens_used INTEGER,
    FOREIGN KEY (session_id) REFERENCES chat_sessions(id) ON DELETE CASCADE
);

-- Meta information cache
CREATE TABLE meta_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cache_key TEXT UNIQUE NOT NULL, -- e.g., "character_build:RaidenShogun"
    cache_value TEXT NOT NULL,      -- JSON blob
    source_url TEXT,
    cached_timestamp DATETIME NOT NULL,
    expiry_timestamp DATETIME       -- NULL = never expires
);
```

### Indexes for Performance

```sql
CREATE INDEX idx_characters_key ON characters(key);
CREATE INDEX idx_weapons_key ON weapons(key);
CREATE INDEX idx_weapons_location ON weapons(location);
CREATE INDEX idx_artifacts_set ON artifacts(set_key);
CREATE INDEX idx_artifacts_location ON artifacts(location);
CREATE INDEX idx_artifacts_slot ON artifacts(slot);
CREATE INDEX idx_substats_artifact ON artifact_substats(artifact_id);
CREATE INDEX idx_materials_key ON materials(key);
CREATE INDEX idx_chat_session ON chat_messages(session_id);
CREATE INDEX idx_meta_cache_key ON meta_cache(cache_key);
```

---

## User Interface Design

### Main Window Layout (Avalonia)

```
┌────────────────────────────────────────────────────────────────┐
│ Inventory Kamera                               [─][□][×]       │
├────────────────────────────────────────────────────────────────┤
│ [Scan] [Settings] [Export] [Database] [AI Assistant] ◄─ Tabs  │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  AI Assistant Tab (selected)                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ ┌────────────────────────────────────────────────────┐   │  │
│  │ │ Chat History                                       │   │  │
│  │ │                                                    │   │  │
│  │ │ Assistant: Hello! I can help you optimize your   │   │  │
│  │ │ Genshin Impact inventory. What would you like    │   │  │
│  │ │ to know?                                          │   │  │
│  │ │                                                    │   │  │
│  │ │ You: What's the best artifact set for my Raiden? │   │  │
│  │ │                                                    │   │  │
│  │ │ Assistant: Let me check your inventory...        │   │  │
│  │ │ [Analyzing database...]                           │   │  │
│  │ │                                                    │   │  │
│  │ │ I see you have Raiden Shogun at C2, level 90.   │   │  │
│  │ │ For optimal performance, I recommend:             │   │  │
│  │ │                                                    │   │  │
│  │ │ **Recommended:** 4pc Emblem of Severed Fate      │   │  │
│  │ │ - Increases ER by 20%                             │   │  │
│  │ │ - Burst DMG +25% of ER (up to 75%)               │   │  │
│  │ │                                                    │   │  │
│  │ │ **Your Current Setup:** 2pc Gladiator, 2pc       │   │  │
│  │ │ Thundering Fury                                   │   │  │
│  │ │                                                    │   │  │
│  │ │ **Available in Inventory:**                       │   │  │
│  │ │ You have enough Emblem pieces for a strong set: │   │  │
│  │ │                                                    │   │  │
│  │ │ • Flower (Lv20) - CR 10.5%, CD 21%, ER 11%      │   │  │
│  │ │ • Plume (Lv20) - ER 18%, CD 14%, ATK% 10%       │   │  │
│  │ │ • Sands (Lv20, ER%) - CR 7%, CD 28%             │   │  │
│  │ │ • Goblet (Lv16, Electro%) - CR 10%, CD 15%      │   │  │
│  │ │ • Circlet (Lv20, CR%) - CD 31%, ER 11%          │   │  │
│  │ │                                                    │   │  │
│  │ │ This set would give you approximately:            │   │  │
│  │ │ - 220% ER (excellent for C2 Raiden)              │   │  │
│  │ │ - 75/195 CR/CD ratio (great for Catch R5)        │   │  │
│  │ │                                                    │   │  │
│  │ │ Would you like me to suggest which current       │   │  │
│  │ │ artifacts to replace?                             │   │  │
│  │ │                                                    │   │  │
│  │ │                                    [Copy Answer]  │   │  │
│  │ └────────────────────────────────────────────────────┘   │  │
│  │                                                          │  │
│  │ Quick Actions:                                           │  │
│  │ [Optimize Current Team] [Artifact Recommendations]       │  │
│  │ [Farming Priority] [Team Comp Ideas]                     │  │
│  │                                                          │  │
│  │ Ask a question:                                          │  │
│  │ ┌──────────────────────────────────────────┐             │  │
│  │ │ Type your question here...               │ [Send]      │  │
│  │ └──────────────────────────────────────────┘             │  │
│  │                                                          │  │
│  │ Session: Default   Tokens: 1,247  Est. Cost: $0.02      │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

### Settings/Preferences - AI Configuration

```
┌─────────────────────────────────────────────────────────────┐
│ AI Assistant Settings                          [─][□][×]    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│ Provider Configuration                                       │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ AI Provider: [Anthropic (Claude) ▼]                    │  │
│ │   • Anthropic (Claude)                                 │  │
│ │   • Google (Gemini)                                    │  │
│ │   • Ollama                                             │  │
│ │                                                         │  │
│ │ API Key: [sk-ant-api03-*********************] [Show]   │  │
│ │          [Test Connection]                             │  │
│ │                                                         │  │
│ │ Model: [claude-sonnet-4.5 ▼]                           │  │
│ │   • claude-opus-4.5 (Most capable, $15/$75 per M)      │  │
│ │   • claude-sonnet-4.5 (Balanced, $3/$15 per M)         │  │
│ │   • claude-haiku-4 (Fast & cheap, $0.25/$1.25 per M)   │  │
│ │                                                         │  │
│ └────────────────────────────────────────────────────────┘  │
│                                                              │
│ Features                                                     │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ ☑ Enable web search for game guides                    │  │
│ │ ☑ Cache meta information locally (reduces API costs)   │  │
│ │ ☑ Show token usage and cost estimates                  │  │
│ │ ☐ Include artifact screenshots in context (higher cost)│  │
│ └────────────────────────────────────────────────────────┘  │
│                                                              │
│ Cost Management                                              │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ Monthly Budget Limit: [$5.00    ] (optional)           │  │
│ │ Current Month Usage: $1.23 / $5.00                      │  │
│ │                                                         │  │
│ │ ☑ Warn before expensive queries (>10k tokens)          │  │
│ │ ☑ Auto-clear chat history after 30 days                │  │
│ └────────────────────────────────────────────────────────┘  │
│                                                              │
│ Meta Information Sources                                     │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ ☑ game8.co (character guides)                           │  │
│ │ ☑ KeqingMains (in-depth analysis)                       │  │
│ │ ☐ Genshin.gg (tier lists) - experimental               │  │
│ │                                                         │  │
│ │ Cache Expiry: [7 days ▼]                                │  │
│ │ [Clear Cache Now]                                       │  │
│ └────────────────────────────────────────────────────────┘  │
│                                                              │
│                                  [Cancel]  [Save Settings]  │
└─────────────────────────────────────────────────────────────┘
```

### Database Viewer Tab

```
┌────────────────────────────────────────────────────────────────┐
│ Database Viewer                                                │
├────────────────────────────────────────────────────────────────┤
│ Table: [Characters ▼]                                          │
│                                                                 │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │ Key            │Level│Const│Weapon          │Last Scanned  │ │
│ ├────────────────┼─────┼─────┼────────────────┼──────────────┤ │
│ │ RaidenShogun   │  90 │  C2 │EngulfingLight │ 2026-04-09   │ │
│ │ Bennett        │  80 │  C5 │ MistsplitterRe │ 2026-04-09   │ │
│ │ Xiangling      │  80 │  C6 │ TheCatch       │ 2026-04-09   │ │
│ │ Xingqiu        │  90 │  C6 │ SacrificialSwo │ 2026-04-09   │ │
│ │ Nahida         │  90 │  C0 │ AThousandFloat │ 2026-04-09   │ │
│ │ ...            │     │     │                │              │ │
│ └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ [Export to CSV] [Refresh]                                      │
└────────────────────────────────────────────────────────────────┘
```

---

## AI Tool Definitions

The AI assistant has access to these tools via function calling:

### Database Query Tools

```csharp
namespace InventoryKamera.AI.Tools
{
    public class DatabaseTools
    {
        private readonly SQLiteConnection _db;

        /// <summary>
        /// Execute a read-only SQL query against the user's inventory database.
        /// Returns results as JSON.
        /// </summary>
        [AITool("query_inventory")]
        [Description("Execute SQL SELECT query against inventory database. " +
                     "Tables: characters, weapons, artifacts, artifact_substats, materials. " +
                     "Example: SELECT key, level FROM characters WHERE constellation >= 6")]
        public string QueryInventory(
            [Description("SQL SELECT query (read-only)")] string sqlQuery)
        {
            // Validate query is SELECT only (no INSERT/UPDATE/DELETE)
            if (!IsReadOnlyQuery(sqlQuery))
            {
                return JsonConvert.SerializeObject(new
                {
                    error = "Only SELECT queries are allowed"
                });
            }

            try
            {
                var results = _db.Query<dynamic>(sqlQuery);
                return JsonConvert.SerializeObject(results, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get detailed information about a specific character including
        /// equipped weapon and artifacts.
        /// </summary>
        [AITool("get_character_details")]
        [Description("Get complete character information including talents, " +
                     "constellation, equipped gear, and artifact stats")]
        public CharacterDetails GetCharacterDetails(
            [Description("Character key (e.g., 'RaidenShogun', 'Bennett')")]
            string characterKey)
        {
            var character = _db.Table<Character>()
                .FirstOrDefault(c => c.Key == characterKey);

            if (character == null)
            {
                return null;
            }

            // Get equipped weapon
            Weapon weapon = null;
            if (character.EquippedWeaponId != null)
            {
                weapon = _db.Table<Weapon>()
                    .FirstOrDefault(w => w.Id == character.EquippedWeaponId);
            }

            // Get equipped artifacts
            var artifacts = _db.Table<Artifact>()
                .Where(a => a.Location == characterKey)
                .ToList();

            // Get substats for each artifact
            foreach (var artifact in artifacts)
            {
                artifact.Substats = _db.Table<ArtifactSubstat>()
                    .Where(s => s.ArtifactId == artifact.Id)
                    .ToList();
            }

            return new CharacterDetails
            {
                Character = character,
                EquippedWeapon = weapon,
                EquippedArtifacts = artifacts
            };
        }

        /// <summary>
        /// Find best available artifacts for a character based on desired
        /// set and main stats.
        /// </summary>
        [AITool("find_best_artifacts")]
        [Description("Search inventory for best artifact pieces matching desired " +
                     "set and main stats. Returns ranked by substat quality.")]
        public ArtifactRecommendation FindBestArtifacts(
            [Description("Character key to optimize for")] string characterKey,
            [Description("Desired artifact set key (e.g., 'EmblemOfSeveredFate')")]
            string setKey,
            [Description("Desired main stats by slot (JSON: {\"sands\":\"ener_\",\"goblet\":\"electro_dmg_\",\"circlet\":\"critRate_\"})")]
            string desiredMainStats = null)
        {
            var mainStatsDict = string.IsNullOrEmpty(desiredMainStats)
                ? new Dictionary<string, string>()
                : JsonConvert.DeserializeObject<Dictionary<string, string>>(desiredMainStats);

            var artifacts = _db.Table<Artifact>()
                .Where(a => a.SetKey == setKey)
                .ToList();

            var recommendation = new ArtifactRecommendation();

            foreach (var slot in new[] { "flower", "plume", "sands", "goblet", "circlet" })
            {
                var candidates = artifacts.Where(a => a.Slot == slot);

                // Filter by desired main stat if specified
                if (mainStatsDict.TryGetValue(slot, out string desiredStat))
                {
                    candidates = candidates.Where(a => a.MainStatKey == desiredStat);
                }

                // Rank by substat quality (CV = CR*2 + CD for DPS)
                var ranked = candidates
                    .Select(a => new
                    {
                        Artifact = a,
                        Score = CalculateSubstatScore(a, characterKey)
                    })
                    .OrderByDescending(x => x.Score)
                    .ToList();

                if (ranked.Any())
                {
                    recommendation.BestPieces[slot] = ranked.First().Artifact;
                    recommendation.Alternatives[slot] = ranked.Skip(1).Take(3)
                        .Select(x => x.Artifact).ToList();
                }
            }

            return recommendation;
        }

        /// <summary>
        /// Get summary statistics about the user's inventory.
        /// </summary>
        [AITool("get_inventory_summary")]
        [Description("Get high-level statistics about inventory (character count, " +
                     "5-star weapons, artifact sets, etc.)")]
        public InventorySummary GetInventorySummary()
        {
            return new InventorySummary
            {
                TotalCharacters = _db.Table<Character>().Count(),
                MaxConstellationCharacters = _db.Table<Character>()
                    .Count(c => c.Constellation == 6),
                FiveStarWeapons = _db.Table<Weapon>()
                    .Count(w => w.Rarity == 5),
                Level20Artifacts = _db.Table<Artifact>()
                    .Count(a => a.Level == 20),
                LastScanDate = _db.Table<ScanHistory>()
                    .OrderByDescending(s => s.ScanTimestamp)
                    .FirstOrDefault()?.ScanTimestamp
            };
        }
    }
}
```

### Meta Information Tools

```csharp
public class MetaTools
{
    private readonly HttpClient _httpClient;
    private readonly SQLiteConnection _cacheDb;

    /// <summary>
    /// Get optimal build guide for a character from game8.co or cached data.
    /// </summary>
    [AITool("get_character_build_guide")]
    [Description("Fetch recommended artifacts, weapons, stats, and teams for a character " +
                 "from game8.co or local cache")]
    public BuildGuide GetCharacterBuildGuide(
        [Description("Character key (e.g., 'RaidenShogun')")] string characterKey)
    {
        // Check cache first
        string cacheKey = $"character_build:{characterKey}";
        var cached = GetFromCache(cacheKey);
        if (cached != null && !cached.IsExpired())
        {
            return JsonConvert.DeserializeObject<BuildGuide>(cached.CacheValue);
        }

        // Fetch from game8.co
        var guide = FetchGame8BuildGuide(characterKey);

        // Cache for 7 days
        SaveToCache(cacheKey, JsonConvert.SerializeObject(guide),
            expiryDays: 7, sourceUrl: guide.SourceUrl);

        return guide;
    }

    /// <summary>
    /// Get weapon information including stats, passive, and recommended characters.
    /// </summary>
    [AITool("get_weapon_guide")]
    [Description("Get weapon stats, passive ability, and which characters benefit most")]
    public WeaponGuide GetWeaponGuide(
        [Description("Weapon key (e.g., 'EngulfingLightning')")] string weaponKey)
    {
        // Similar caching logic
        string cacheKey = $"weapon_guide:{weaponKey}";
        // ... fetch and return
    }

    /// <summary>
    /// Analyze team composition for elemental resonance, synergies, and role coverage.
    /// </summary>
    [AITool("analyze_team_composition")]
    [Description("Evaluate a team of 4 characters for synergies, resonance, and role balance")]
    public TeamAnalysis AnalyzeTeam(
        [Description("Array of 4 character keys")] string[] characterKeys)
    {
        if (characterKeys.Length != 4)
        {
            return new TeamAnalysis { Error = "Team must have exactly 4 characters" };
        }

        var analysis = new TeamAnalysis
        {
            Characters = characterKeys,
            ElementalResonance = DetermineResonance(characterKeys),
            Roles = DetermineRoles(characterKeys),
            Synergies = FindSynergies(characterKeys),
            Weaknesses = FindWeaknesses(characterKeys)
        };

        return analysis;
    }

    /// <summary>
    /// Get current Spiral Abyss floor 12 recommended teams and enemy information.
    /// </summary>
    [AITool("get_current_abyss_meta")]
    [Description("Fetch current Spiral Abyss rotation enemies, buffs, and recommended teams")]
    public AbyssGuide GetCurrentAbyssMeta()
    {
        // Scrape from community sites or use cached data
        // Cache expires every 2 weeks (half-patch cycle)
        string cacheKey = "abyss_meta:current";
        var cached = GetFromCache(cacheKey);
        if (cached != null && !cached.IsExpired())
        {
            return JsonConvert.DeserializeObject<AbyssGuide>(cached.CacheValue);
        }

        var guide = FetchCurrentAbyssGuide();
        SaveToCache(cacheKey, JsonConvert.SerializeObject(guide),
            expiryDays: 14);

        return guide;
    }

    /// <summary>
    /// Calculate stat goals for a character (CR/CD ratio, ER requirements, etc.)
    /// </summary>
    [AITool("calculate_stat_requirements")]
    [Description("Calculate optimal stat thresholds for a character (CR/CD ratio, ER, EM, etc.)")]
    public StatRequirements CalculateStatRequirements(
        [Description("Character key")] string characterKey,
        [Description("Weapon key they're using")] string weaponKey,
        [Description("Team role: dps, sub-dps, support, healer")] string role)
    {
        // Load character base stats and scalings
        // Calculate based on common guidelines:
        // - DPS: 1:2 CR/CD ratio, 60+ CR minimum
        // - ER requirements based on team and role
        // - EM for reaction-based characters

        return new StatRequirements
        {
            CritRate = new StatGoal { Minimum = 60, Recommended = 70 },
            CritDamage = new StatGoal { Minimum = 120, Recommended = 140 },
            EnergyRecharge = new StatGoal { Minimum = 180, Recommended = 220 },
            // ... etc
        };
    }
}
```

### Utility Tools

```csharp
public class UtilityTools
{
    /// <summary>
    /// Calculate farming priority based on current inventory and meta.
    /// </summary>
    [AITool("get_farming_priorities")]
    [Description("Recommend which domains/materials to farm based on inventory gaps " +
                 "and character investment priorities")]
    public FarmingPriority GetFarmingPriorities(
        [Description("Focus characters to prioritize (optional)")]
        string[] focusCharacters = null)
    {
        // Analyze:
        // 1. Which characters need talent books/boss mats
        // 2. Which artifact sets are needed for top characters
        // 3. Weapon ascension materials needed
        // 4. Current material inventory vs requirements

        return new FarmingPriority
        {
            TopPriority = new[]
            {
                new FarmingTask
                {
                    Domain = "Momiji-Dyed Court",
                    Reason = "Need Emblem set for Raiden (current artifacts low CV)",
                    EstimatedResinDays = 7
                },
                new FarmingTask
                {
                    Domain = "Taishan Mansion",
                    Reason = "Bennett needs 18 Gold talent books (C5 talents 1/8/8)",
                    EstimatedResinDays = 3
                }
            }
        };
    }

    /// <summary>
    /// Track progression changes between scans.
    /// </summary>
    [AITool("get_progression_delta")]
    [Description("Compare current scan with previous scan to show what changed " +
                 "(new artifacts, level ups, new characters, etc.)")]
    public ProgressionDelta GetProgressionDelta(
        [Description("Number of days to look back (default 7)")] int daysBack = 7)
    {
        var currentScan = GetLatestScan();
        var previousScan = GetScanBefore(DateTime.UtcNow.AddDays(-daysBack));

        if (previousScan == null)
        {
            return new ProgressionDelta
            {
                Error = "No previous scan found in that timeframe"
            };
        }

        return new ProgressionDelta
        {
            NewArtifacts = currentScan.ArtifactCount - previousScan.ArtifactCount,
            CharacterLevelUps = CountCharacterLevelUps(previousScan, currentScan),
            NewWeapons = currentScan.WeaponCount - previousScan.WeaponCount,
            // ... etc
        };
    }
}
```

---

## Technical Architecture

### Project Structure

```
InventoryKamera/
├── InventoryKamera/                  # Main WinForms project (until Phase 2)
├── InventoryKamera.Avalonia/         # New Avalonia UI (Phase 2+)
│   ├── Views/
│   │   ├── MainWindow.axaml          # Main app window with tabs
│   │   ├── AIChatView.axaml          # AI Assistant tab
│   │   ├── DatabaseView.axaml        # Database viewer tab
│   │   └── AISettingsView.axaml      # AI configuration dialog
│   ├── ViewModels/
│   │   ├── AIChatViewModel.cs        # Chat interface logic
│   │   ├── DatabaseViewModel.cs      # Database viewer logic
│   │   └── AISettingsViewModel.cs    # Settings management
│   └── Services/
│       ├── AIService.cs              # AI provider abstraction
│       ├── ClaudeService.cs          # Anthropic API implementation
│       ├── GeminiService.cs          # Google Gemini implementation
│       └── OllamaService.cs          # Ollama API implementation
├── InventoryKamera.AI/               # New class library for AI features
│   ├── Tools/
│   │   ├── DatabaseTools.cs          # Inventory query tools
│   │   ├── MetaTools.cs              # Build guide fetching
│   │   └── UtilityTools.cs           # Farming priority, progression
│   ├── Models/
│   │   ├── BuildGuide.cs
│   │   ├── TeamAnalysis.cs
│   │   ├── CharacterDetails.cs
│   │   └── ...
│   └── Scrapers/
│       ├── Game8Scraper.cs           # Scrape game8.co
│       └── KeqingMainsScraper.cs     # Scrape KeqingMains guides
├── InventoryKamera.Database/         # New class library for database
│   ├── Models/
│   │   ├── Character.cs              # SQLite entity models
│   │   ├── Weapon.cs
│   │   ├── Artifact.cs
│   │   └── ...
│   ├── Services/
│   │   ├── DatabaseService.cs        # CRUD operations
│   │   ├── MigrationService.cs       # GOOD JSON → SQLite migration
│   │   └── QueryService.cs           # Complex queries
│   └── Migrations/
│       └── InitialSchema.sql         # Database initialization
└── InventoryKamera.Tests/
    └── AI/
        ├── DatabaseToolsTests.cs
        └── MetaToolsTests.cs
```

### AI Provider Abstraction

```csharp
public interface IAIService
{
    Task<AIResponse> SendMessageAsync(
        string userMessage,
        List<AIMessage> conversationHistory,
        List<AITool> availableTools);

    Task<bool> TestConnectionAsync();

    (int inputTokens, int outputTokens) EstimateTokens(string message);

    decimal CalculateCost(int inputTokens, int outputTokens);
}

public class ClaudeService : IAIService
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public async Task<AIResponse> SendMessageAsync(
        string userMessage,
        List<AIMessage> conversationHistory,
        List<AITool> availableTools)
    {
        var messages = conversationHistory
            .Append(new AIMessage { Role = "user", Content = userMessage })
            .Select(m => new Message
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();

        var tools = availableTools.Select(t => new Tool
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();

        var response = await _client.Messages.CreateAsync(new MessageRequest
        {
            Model = _model,
            Messages = messages,
            Tools = tools,
            MaxTokens = 4096
        });

        // Handle tool calls
        while (response.StopReason == "tool_use")
        {
            var toolCalls = response.Content
                .OfType<ToolUseBlock>()
                .ToList();

            foreach (var toolCall in toolCalls)
            {
                var tool = availableTools.First(t => t.Name == toolCall.Name);
                var result = await tool.ExecuteAsync(toolCall.Input);

                messages.Add(new Message
                {
                    Role = "assistant",
                    Content = response.Content
                });

                messages.Add(new Message
                {
                    Role = "user",
                    Content = new ToolResultBlock
                    {
                        ToolUseId = toolCall.Id,
                        Content = result
                    }
                });
            }

            response = await _client.Messages.CreateAsync(new MessageRequest
            {
                Model = _model,
                Messages = messages,
                Tools = tools,
                MaxTokens = 4096
            });
        }

        return new AIResponse
        {
            Content = response.Content.OfType<TextBlock>().First().Text,
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens
        };
    }

    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        // Claude Sonnet 4.5: $3/M input, $15/M output
        return (inputTokens / 1_000_000m * 3m) +
               (outputTokens / 1_000_000m * 15m);
    }
}

public class OllamaService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaService(string apiKey, string model, string baseUrl = "https://api.ollama.com")
    {
        _apiKey = apiKey;
        _model = model;
        _baseUrl = baseUrl;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public async Task<AIResponse> SendMessageAsync(
        string userMessage,
        List<AIMessage> conversationHistory,
        List<AITool> availableTools)
    {
        var messages = conversationHistory
            .Append(new AIMessage { Role = "user", Content = userMessage })
            .Select(m => new { role = m.Role, content = m.Content })
            .ToList();

        var tools = availableTools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.InputSchema
            }
        }).ToList();

        var request = new
        {
            model = _model,
            messages = messages,
            tools = tools,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/chat", request);
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();

        // Handle tool calls (similar to Claude, but Ollama uses different format)
        while (result.Message.ToolCalls != null && result.Message.ToolCalls.Any())
        {
            foreach (var toolCall in result.Message.ToolCalls)
            {
                var tool = availableTools.First(t => t.Name == toolCall.Function.Name);
                var toolResult = await tool.ExecuteAsync(toolCall.Function.Arguments);

                messages.Add(new { role = "assistant", content = result.Message.Content, tool_calls = result.Message.ToolCalls });
                messages.Add(new { role = "tool", tool_call_id = toolCall.Id, content = toolResult });
            }

            request = new { model = _model, messages = messages, tools = tools, stream = false };
            response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/chat", request);
            result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        }

        return new AIResponse
        {
            Content = result.Message.Content,
            InputTokens = result.PromptEvalCount ?? 0,
            OutputTokens = result.EvalCount ?? 0
        };
    }

    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        // Ollama pricing varies by model and tier
        // Example for paid tier (adjust based on actual Ollama pricing)
        return (inputTokens / 1_000_000m * 0.50m) +
               (outputTokens / 1_000_000m * 1.50m);
    }
}
```

### Database Export Integration

Modify existing `GOOD.cs` export to also write SQLite:

```csharp
public class DatabaseExporter
{
    private readonly SQLiteConnection _db;

    public void ExportInventoryToDatabase(Inventory inventory)
    {
        _db.BeginTransaction();

        try
        {
            var scanTimestamp = DateTime.UtcNow;

            // Export characters
            foreach (var character in inventory.Characters)
            {
                var weaponId = ExportWeaponIfEquipped(character, scanTimestamp);

                _db.Insert(new Character
                {
                    Key = character.key,
                    Level = character.level,
                    Constellation = character.constellation,
                    Ascension = character.ascension,
                    TalentAuto = character.talent?.auto ?? 1,
                    TalentSkill = character.talent?.skill ?? 1,
                    TalentBurst = character.talent?.burst ?? 1,
                    EquippedWeaponId = weaponId,
                    ScanTimestamp = scanTimestamp
                });

                ExportEquippedArtifacts(character.key, inventory, scanTimestamp);
            }

            // Export unequipped weapons
            foreach (var weapon in inventory.Weapons.Where(w => string.IsNullOrEmpty(w.location)))
            {
                _db.Insert(new Weapon
                {
                    Key = weapon.key,
                    Level = weapon.level,
                    Ascension = weapon.ascension,
                    Refinement = weapon.refinement,
                    Locked = weapon.@lock,
                    Location = weapon.location,
                    Rarity = weapon.rarity,
                    ScanTimestamp = scanTimestamp
                });
            }

            // Export artifacts
            foreach (var artifact in inventory.Artifacts)
            {
                var artifactId = _db.Insert(new Artifact
                {
                    SetKey = artifact.setKey,
                    Slot = artifact.slotKey,
                    Rarity = artifact.rarity,
                    Level = artifact.level,
                    MainStatKey = artifact.mainStatKey,
                    MainStatValue = artifact.statValue,
                    Locked = artifact.@lock,
                    Location = artifact.location,
                    ScanTimestamp = scanTimestamp
                });

                // Export substats
                foreach (var substat in artifact.substats ?? new List<Substat>())
                {
                    _db.Insert(new ArtifactSubstat
                    {
                        ArtifactId = artifactId,
                        StatKey = substat.key,
                        StatValue = substat.value
                    });
                }
            }

            // Record scan history
            _db.Insert(new ScanHistory
            {
                ScanTimestamp = scanTimestamp,
                ScanType = "full",
                ItemsScanned = inventory.Characters.Count +
                               inventory.Weapons.Count +
                               inventory.Artifacts.Count,
                DurationSeconds = 0 // TODO: track actual duration
            });

            _db.Commit();
        }
        catch (Exception ex)
        {
            _db.Rollback();
            Logger.Error(ex, "Failed to export inventory to database");
            throw;
        }
    }
}
```

---

## Privacy & Security

### API Key Storage

Use Windows Data Protection API (DPAPI) to encrypt API keys:

```csharp
public class SecureSettings
{
    public static void SaveAPIKey(string provider, string apiKey)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(apiKey);
        byte[] encryptedBytes = ProtectedData.Protect(
            keyBytes,
            null,
            DataProtectionScope.CurrentUser);

        string encryptedBase64 = Convert.ToBase64String(encryptedBytes);

        Properties.Settings.Default[$"{provider}_APIKey_Encrypted"] = encryptedBase64;
        Properties.Settings.Default.Save();
    }

    public static string LoadAPIKey(string provider)
    {
        string encryptedBase64 = Properties.Settings.Default[$"{provider}_APIKey_Encrypted"]?.ToString();

        if (string.IsNullOrEmpty(encryptedBase64))
            return null;

        byte[] encryptedBytes = Convert.FromBase64String(encryptedBase64);
        byte[] keyBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(keyBytes);
    }
}
```

### Data Privacy

- **Database stays local** - SQLite file stored in user's AppData folder
- **No telemetry** - Inventory data never uploaded to third-party servers
- **Direct API calls** - Communication goes directly to AI provider (Anthropic/Google)
- **User controls caching** - Can disable web scraping and use manual imports
- **Clear data option** - Easy database wipe in settings

### Cost Management

```csharp
public class CostTracker
{
    private readonly SQLiteConnection _db;

    public void RecordAPICall(string provider, int inputTokens, int outputTokens, decimal cost)
    {
        _db.Insert(new APICallLog
        {
            Provider = provider,
            Timestamp = DateTime.UtcNow,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = cost
        });
    }

    public decimal GetMonthlySpend(string provider)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return _db.Table<APICallLog>()
            .Where(log => log.Provider == provider && log.Timestamp >= startOfMonth)
            .Sum(log => log.EstimatedCost);
    }

    public bool IsUnderBudget(string provider, decimal estimatedCallCost)
    {
        var budget = Properties.Settings.Default[$"{provider}_MonthlyBudget"];
        if (budget == null) return true; // No budget set

        var currentSpend = GetMonthlySpend(provider);
        return currentSpend + estimatedCallCost <= (decimal)budget;
    }
}
```

Before expensive queries:

```csharp
public async Task<bool> ConfirmExpensiveQuery(int estimatedTokens)
{
    var cost = _aiService.CalculateCost(estimatedTokens, estimatedTokens);

    if (cost > 0.10m) // More than 10 cents
    {
        var result = await ShowDialog(
            "Expensive Query",
            $"This query will cost approximately ${cost:F2}. Continue?",
            "Yes", "No");

        return result == DialogResult.Yes;
    }

    return true;
}
```

---

## Web Scraping Implementation

### Game8 Build Guide Scraper

```csharp
public class Game8Scraper
{
    private readonly HttpClient _httpClient;

    public async Task<BuildGuide> FetchBuildGuideAsync(string characterKey)
    {
        // Map character key to game8 URL
        string characterName = CharacterKeyToDisplayName(characterKey);
        string url = $"https://game8.co/games/Genshin-Impact/archives/297465";

        var html = await _httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var guide = new BuildGuide
        {
            CharacterKey = characterKey,
            SourceUrl = url,
            FetchedTimestamp = DateTime.UtcNow
        };

        // Parse recommended artifact sets
        var artifactSection = doc.DocumentNode
            .SelectSingleNode("//h3[contains(text(), 'Best Artifacts')]");

        if (artifactSection != null)
        {
            var artifactList = artifactSection.SelectSingleNode("following-sibling::div[1]//ul");
            guide.RecommendedArtifactSets = artifactList.SelectNodes(".//li")
                ?.Select(li => li.InnerText.Trim())
                .ToList() ?? new List<string>();
        }

        // Parse recommended weapons
        var weaponSection = doc.DocumentNode
            .SelectSingleNode("//h3[contains(text(), 'Best Weapons')]");

        if (weaponSection != null)
        {
            var weaponTable = weaponSection.SelectSingleNode("following-sibling::table[1]");
            guide.RecommendedWeapons = weaponTable.SelectNodes(".//tr")
                ?.Skip(1) // Skip header
                .Select(tr => new WeaponRecommendation
                {
                    WeaponName = tr.SelectSingleNode(".//td[1]")?.InnerText.Trim(),
                    Reasoning = tr.SelectSingleNode(".//td[2]")?.InnerText.Trim()
                })
                .ToList() ?? new List<WeaponRecommendation>();
        }

        // Parse main stat priorities
        var statSection = doc.DocumentNode
            .SelectSingleNode("//h3[contains(text(), 'Stats Priority')]");

        if (statSection != null)
        {
            // ... parse stat recommendations
        }

        return guide;
    }
}
```

### Caching Strategy

```csharp
public class MetaCache
{
    private readonly SQLiteConnection _db;

    public void SaveToCache(string key, object value, int expiryDays, string sourceUrl = null)
    {
        var json = JsonConvert.SerializeObject(value);

        _db.InsertOrReplace(new MetaCacheEntry
        {
            CacheKey = key,
            CacheValue = json,
            SourceUrl = sourceUrl,
            CachedTimestamp = DateTime.UtcNow,
            ExpiryTimestamp = expiryDays > 0
                ? DateTime.UtcNow.AddDays(expiryDays)
                : null
        });
    }

    public T GetFromCache<T>(string key) where T : class
    {
        var entry = _db.Table<MetaCacheEntry>()
            .FirstOrDefault(e => e.CacheKey == key);

        if (entry == null)
            return null;

        // Check expiry
        if (entry.ExpiryTimestamp.HasValue && entry.ExpiryTimestamp < DateTime.UtcNow)
        {
            _db.Delete(entry);
            return null;
        }

        return JsonConvert.DeserializeObject<T>(entry.CacheValue);
    }

    public void ClearExpiredCache()
    {
        _db.Table<MetaCacheEntry>()
            .Where(e => e.ExpiryTimestamp != null && e.ExpiryTimestamp < DateTime.UtcNow)
            .ToList()
            .ForEach(e => _db.Delete(e));
    }
}
```

---

## Example Conversation Flows

### Example 1: Artifact Optimization

```
User: What artifacts should I put on my Raiden Shogun?

AI: Let me check your inventory and current Raiden setup...
[Calls: get_character_details("RaidenShogun")]
[Calls: get_character_build_guide("RaidenShogun")]

I see you have Raiden Shogun at C2, level 90, with Engulfing Lightning R1.

Current artifacts: 2pc Gladiator + 2pc Thundering Fury

Recommended: 4pc Emblem of Severed Fate
- Increases Energy Recharge by 20%
- Increases Elemental Burst DMG by 25% of ER (up to 75%)
- Perfect for Raiden's burst-focused playstyle

[Calls: find_best_artifacts("RaidenShogun", "EmblemOfSeveredFate",
    {"sands":"ener_","goblet":"electro_dmg_","circlet":"critRate_"})]

Good news! You have excellent Emblem pieces available:

**Recommended 4pc Emblem Set:**
• Flower (Lv20) - CR 10.5%, CD 21%, ER 11%, ATK% 5%
• Plume (Lv20) - ER 18%, CD 14%, ATK% 10%, CR 3%
• Sands (Lv20, ER%) - CR 7%, CD 28%, ATK 16, ATK% 5%
• Goblet (Lv16, Electro DMG%) - CR 10%, CD 15%, ER 6%, DEF 16
• Circlet (Lv20, Crit Rate%) - CD 31%, ER 11%, ATK% 5%, EM 19

**Projected Stats (with Engulfing Lightning R1):**
- ER: ~220% (excellent for C2 Raiden)
- CR/CD: 75/195 (great 1:2.6 ratio)
- Electro DMG: 46.6% (from goblet)
- ATK: ~2100

This set would be a significant upgrade over your current 2pc/2pc setup,
giving you ~25-30% more burst damage.

Would you like me to:
1. Compare this to other available artifact sets?
2. Calculate how many artifact exp materials you'd need to upgrade the goblet to 20?
3. Suggest alternative pieces if you want higher crit rate?
```

### Example 2: Team Building for Abyss

```
User: I'm stuck on Abyss floor 12. Can you help me build a team?

AI: I'd be happy to help! Let me analyze your roster and the current Abyss rotation.

[Calls: get_inventory_summary()]
[Calls: get_current_abyss_meta()]
[Calls: query_inventory("SELECT key, level, constellation FROM characters WHERE level >= 70")]

For Abyss 12-3 (Current rotation: v4.5):
- First Half: Heavy hydro enemies (benefit from electro/cryo)
- Second Half: Ruin enemies (need shield break + AoE)

Based on your level 80+ characters, I recommend:

**Team 1 (First Half):**
1. Raiden Shogun (C2, Lv90) - Main DPS/Driver
2. Bennett (C5, Lv80) - ATK buffer + healer
3. Xiangling (C6, Lv80) - Pyro sub-DPS
4. Xingqiu (C6, Lv90) - Hydro application + damage reduction

This is the classic Raiden National team - excellent for this chamber.

**Team 2 (Second Half):**
1. Nahida (C0, Lv90) - Dendro application
2. Fischl (C6, Lv80) - Electro sub-DPS (Aggravate)
3. Zhongli (C0, Lv90) - Shield + resistance shred
4. Kazuha (C0, Lv90) - Grouping + elemental DMG buff

This Aggravate team handles shields well and has strong AoE.

[Calls: analyze_team_composition(["RaidenShogun","Bennett","Xiangling","Xingqiu"])]
[Calls: analyze_team_composition(["Nahida","Fischl","Zhongli","Kazuha"])]

Team 1 has Pyro Resonance (+25% ATK) and excellent single-target burst.
Team 2 has strong elemental reactions and shield-breaking capability.

Would you like me to review the artifacts on these characters to make sure
they're optimized?
```

### Example 3: Farming Priority

```
User: What should I be farming right now?

AI: Let me analyze your inventory and see where you have the most room for improvement.

[Calls: get_farming_priorities()]
[Calls: get_character_details("RaidenShogun")]
[Calls: get_character_details("Bennett")]

Based on your most-used characters and current inventory:

**Top Priority:**

1. **Momiji-Dyed Court (Emblem/Shimenawa domain)**
   - Why: Your Raiden has great Emblem pieces, but could use better sands (current: 7% CR)
   - Target: ER% sands with CR/CD substats
   - Estimated: 5-7 days of resin

2. **Taishan Mansion (Diligence books, Tuesdays/Fridays/Sundays)**
   - Why: Bennett is C5 but talents are only 1/8/8 (should be 1/12/12 at C5!)
   - Target: 18x Philosophies of Diligence + Crown of Insight
   - Estimated: 3 days + 1 Crown

3. **Narukami Island: Tenshukaku (Raiden boss)**
   - Why: Raiden is ascension 6 but several talents need Storm Beads
   - Target: 9x Storm Beads for skill 9→10
   - Estimated: 1-2 days

**Secondary:**

4. **Artifact strongbox: Noblesse Oblige**
   - Why: Bennett using off-set pieces, could get 4pc Noblesse from strongbox
   - Target: 4pc Noblesse with ER% sands, HP% goblet, HP% circlet
   - Cost: Feed your bad 5-star artifacts

Would you like me to create a weekly resin schedule for these priorities?
```

---

## Implementation Phases

### Phase 3.1: Database Foundation (2-3 weeks)

**Tasks:**
- [ ] Design and implement SQLite schema
- [ ] Create database service layer (CRUD operations)
- [ ] Modify GOOD export to also write SQLite
- [ ] Implement GOOD JSON → SQLite migration tool
- [ ] Add Database Viewer tab (Avalonia DataGrid)
- [ ] Add scan history tracking
- [ ] Write unit tests for database layer

**Deliverables:**
- Working SQLite database export alongside GOOD JSON
- Database viewer UI in Avalonia app
- Migration tool for existing users

### Phase 3.2: AI Chat Interface (3-4 weeks)

**Tasks:**
- [ ] Create AI service abstraction (IAIService interface)
- [ ] Implement ClaudeService (Anthropic API)
- [ ] Implement GeminiService (Google API)
- [ ] Implement OllamaService (Ollama API)
- [ ] Design and build AI Chat tab UI (Avalonia)
- [ ] Implement conversation persistence (save/load sessions)
- [ ] Add API configuration UI (settings dialog)
- [ ] Implement secure API key storage (DPAPI)
- [ ] Add token usage tracking and cost estimation
- [ ] Implement basic DatabaseTools (query_inventory, get_character_details)
- [ ] Add budget limits and warnings
- [ ] Write integration tests for AI services

**Deliverables:**
- Working chat interface with Claude/Gemini support
- Secure API key management
- Basic inventory querying capability
- Cost tracking and budget management

### Phase 3.3: Meta Intelligence (4-5 weeks)

**Tasks:**
- [ ] Implement MetaTools class
- [ ] Build Game8 web scraper
- [ ] Build KeqingMains guide scraper (optional)
- [ ] Implement meta information caching
- [ ] Add get_character_build_guide tool
- [ ] Add analyze_team_composition tool
- [ ] Add get_current_abyss_meta tool
- [ ] Add calculate_stat_requirements tool
- [ ] Implement find_best_artifacts ranking algorithm
- [ ] Add artifact substat scoring (CV, efficiency)
- [ ] Respect robots.txt and implement rate limiting
- [ ] Add cache management UI (clear, refresh, expiry settings)
- [ ] Write tests for scrapers and meta tools

**Deliverables:**
- Working build guide fetching and caching
- Team analysis capabilities
- Artifact recommendation engine
- Ethical web scraping with caching

### Phase 3.4: Advanced Features (2-3 weeks)

**Tasks:**
- [ ] Implement UtilityTools class
- [ ] Add get_farming_priorities tool
- [ ] Add get_progression_delta tool
- [ ] Implement quick action buttons in UI
- [ ] Add export conversation as markdown
- [ ] Add conversation search/filter
- [ ] Implement historical progression charts (optional)
- [ ] Add farming schedule generator
- [ ] Create comprehensive user documentation
- [ ] Add tutorial/walkthrough for first-time users
- [ ] Performance optimization (query caching, lazy loading)
- [ ] Write end-to-end tests

**Deliverables:**
- Farming priority recommendations
- Progression tracking
- Quick action buttons for common queries
- Complete feature set with documentation

---

## Success Criteria

### Phase 3.1 Complete When:
- [ ] Inventory data exports to SQLite database
- [ ] Database viewer shows characters, weapons, artifacts in grid
- [ ] Existing GOOD JSON files can be migrated to database
- [ ] Scan history is tracked and viewable
- [ ] Unit test coverage >80% for database layer
- [ ] Documentation for database schema

### Phase 3.2 Complete When:
- [ ] Users can configure Claude, Gemini, or Ollama API keys
- [ ] All three AI providers (Claude, Gemini, Ollama) are fully supported
- [ ] Chat interface sends/receives messages with conversation history
- [ ] AI can query inventory database and return results
- [ ] Token usage and costs are tracked per session
- [ ] Budget warnings prevent overspending
- [ ] API keys are encrypted at rest
- [ ] At least 3 users successfully use the feature in beta (one per provider)

### Phase 3.3 Complete When:
- [ ] AI can fetch build guides from game8.co
- [ ] Build guides are cached locally (7-day expiry)
- [ ] AI can analyze team compositions for synergies
- [ ] AI can recommend best available artifacts from inventory
- [ ] Artifact ranking considers crit value and character needs
- [ ] Web scraping respects rate limits and robots.txt
- [ ] Users can disable web scraping and use cache-only mode

### Phase 3.4 Complete When:
- [ ] AI can recommend farming priorities based on inventory gaps
- [ ] Progression tracking shows changes between scans
- [ ] Quick action buttons work (Optimize Team, Best Builds, etc.)
- [ ] Users can export conversations as markdown
- [ ] Comprehensive documentation with examples
- [ ] Beta testing with 5+ users shows positive feedback
- [ ] Performance: chat responses under 10 seconds for complex queries

---

## Non-Goals

These are explicitly **not** part of Phase 3:

- ❌ Self-hosted local LLM support (LM Studio, custom endpoints) - Phase 4+
- ❌ Real-time damage calculation (too complex, simulation territory)
- ❌ Automatic in-game actions (not possible without game modifications)
- ❌ Social features (sharing teams, comparing inventories with friends)
- ❌ Mobile app version of AI assistant
- ❌ Voice input/output
- ❌ Image generation (artifact card visualizations, etc.)
- ❌ Automatic game data updates (manual cache refresh only)
- ❌ Predictive analytics (ML forecasting of progression)

---

## Alternative: MCP Server Approach

An alternative to embedding the AI in the app would be to build a **Model Context Protocol (MCP) server** that exposes the inventory database:

**Benefits:**
- No need to build chat UI (use Claude Desktop or any MCP client)
- Separation of concerns (database access vs. UI)
- Can use any MCP-compatible client

**Implementation:**
```typescript
// mcp-server-genshin/index.ts
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import sqlite3 from "sqlite3";

const server = new Server({
  name: "genshin-inventory",
  version: "1.0.0"
});

const db = new sqlite3.Database("C:/Users/.../inventory.db");

server.setRequestHandler("tools/list", async () => ({
  tools: [
    {
      name: "query_inventory",
      description: "Query Genshin Impact inventory database",
      inputSchema: {
        type: "object",
        properties: {
          sql: { type: "string" }
        }
      }
    }
  ]
}));

server.setRequestHandler("tools/call", async (request) => {
  if (request.params.name === "query_inventory") {
    return new Promise((resolve) => {
      db.all(request.params.arguments.sql, (err, rows) => {
        resolve({ content: [{ type: "text", text: JSON.stringify(rows) }] });
      });
    });
  }
});

const transport = new StdioServerTransport();
await server.connect(transport);
```

Users would configure Claude Desktop to use this MCP server, then chat directly in Claude Desktop.

**Decision:** We'll proceed with embedded AI (Phase 3 as specified) for better integration and user experience, but may add MCP server support as Phase 4+ alternative.

---

## Related Documents

- `PHASE_1.5_PLAN.md` - ScanProfile.json and navigation region logging
- `PHASE_2_AVALONIA.md` - Avalonia UI migration plan (foundation for Phase 3 UI)
- `PHASE_2_VISUAL_REGION_CONFIG.md` - Visual region configuration tool
- `MODERNIZATION_PLAN.md` - Overall modernization roadmap

---

**Last Updated:** 2026-04-10
**Status:** Draft - Planned for Phase 3 (Post-Avalonia Migration)
