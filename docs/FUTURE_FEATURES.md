# Future Features — Extended Vision

**Purpose:** Capture feature ideas beyond the current three milestones. These are not scheduled — they are possibilities to evaluate when Milestone 3 is stable and the architecture can support them.

*Last updated: 2026-04-29*

---

## 1. Additional Inventory Scanning

The current scanner covers characters, weapons, artifacts, and materials. The following pages are not yet scanned.

### 1.1 Lv20 5★ Artifact Reshape Screen (roll distribution)

Already referenced in Milestone 3 (`totalRolls` field, opt-in, ~2-3s overhead per artifact). Expanded scope here:

- Scan the roll distribution badges (①②③④⑤) per substat
- Record which substats received rolls and how many
- Store as `SubstatRolls` in the database: `{"critRate_": 3, "critDMG_": 2, "atk_": 1}`
- Only eligible for lv20 5★ artifacts; opt-in due to scan time overhead
- Enables AI queries like "which of my artifacts rolled efficiently vs. got lucky on a bad substat?"

### 1.2 Wishes / Pull History

- Navigate to the wish history screen (each banner type separately)
- OCR pull date, item name, rarity, banner type
- Store in database with timestamps — enables tracking pity across sessions, banner history, anniversary reports
- **Limitation:** Genshin only retains ~6 months of wish history in-app; scanner cannot recover older pulls

### 1.3 Sanctifying Elixir Tracking

- Scan the Elixir count from the relevant inventory/crafting screen
- Track monthly usage and remaining count
- Feed into MCP tool: "which unequipped lv20 5★ artifacts are elixir extraction candidates?"
- Pairs naturally with the existing `elixirCrafted` GOOD v3 field

### 1.4 Fragile Resin

- Scan count from the bag/inventory screen
- Track over time — useful context for "how much resin capacity do I have available for domain farming?"

### 1.5 Parametric Transformer

- Scan weekly cooldown status (available / days until reset)
- Scan current gauge fill level if mid-use
- Cross-reference with current material stockpiles to suggest optimal inputs
- Example AI query: "I need Guide-tier Elysium books — what materials should I put in the transformer this week?"
- Eligible "transformer-only" materials (i.e. mob drops that serve no ascension/talent purpose for any owned character) are surfaced automatically

### 1.6 Sigils

- Scan Sigil counts per region (Anemo Sigils, Geo Sigils, Electro Sigils, etc.)
- Primarily completionist/shop tracking — flag when sigil count exceeds available shop items

### 1.7 Oculi

- Scan Statue of the Seven resonance levels per region as a proxy for oculi remaining
- Or scan the oculi count directly from the Precious Items tab if visible
- Completionist feature — "how many oculi are left uncollected in each region?"

### 1.8 Radiant Spincrystals

- Scan count from Precious Items tab
- Completionist tracking only — total available in game is fixed, so "X of Y collected" is meaningful

### 1.9 Quest Items Tab — Collectible Books / Lore Pages

- Scan the collectible book entries in the Quest Items tab
- Each book has ~10 pages scattered around the overworld; track which pages are collected vs. missing per book
- Completionist feature — "You have 7/10 pages of [Book Title]; missing pages 3, 6, 9"
- Requires a `collectible_books.json` reference file listing all known books and their page counts (see Section 4)
- Other key quest items can be scanned for tracking purposes, though most are consumed on use and have no build relevance

### 1.10 Teapot Furnishings

- Scan owned furnishing blueprints and crafted furnishings
- Cross-reference against known furnishing sets to report set completion status
- Example: "You have 8/10 pieces for the Amidst Whispering Winds set — missing: [X, Y]"
- Requires a furnishing set reference data file (see Section 4)

### 1.11 Cooking / Recipes

- **Trigger requirement:** User must open the cookfire and navigate to the recipe list, then click scan
- Scan known recipes (unlocked vs. locked)
- Scan proficiency level per recipe (how many times cooked toward mastery)
- Track "auto-cook" unlocked recipes (proficiency maxed)
- Cross-reference with ingredient inventory to flag "cookable now" recipes

### 1.12 Achievements

- **Trigger requirement:** User opens the Achievements screen, then clicks scan; app pages through each category
- Scan achievement name, completion status, and primogem reward (claimed / unclaimed)
- Track total achievements completed vs. total available
- Flag unclaimed primogem rewards ("You have X achievements completed but rewards not yet claimed")
- Cross-reference against `achievements.json` reference data to identify which achievements are still completable
- Categorize by type: exploration, combat, commission, story, etc.
- Useful for primogem farming planning — surface achievable achievements that are close to completion
- **Reference data requirement:** `inventorylists/achievements.json` — all known achievements, category, primogem value, and any completion hints (see Section 4)
- **Scale consideration:** Genshin has 1000+ achievements; scanning all categories will take significant time. Likely needs a progress bar and cancellation support, similar to how artifact scanning works today.

### 1.13 Battle Pass

- **Trigger requirement:** User opens Battle Pass screen, then clicks scan
- Scan current BP level, XP progress, and unclaimed rewards
- Scan weekly/daily mission list with completion status
- Generate optimal task completion path based on current progress and available tasks
- Example output: "Complete 3 more domains and 2 commission quests to reach the next BP reward tier this week"
- **Cross-feature integration with Cooking (Section 1.11):** The BP weekly task "Cook 20 dishes" can be directly fulfilled using the "cookable now" list from recipe scanning. Two optimization modes:
  - **Speed mode:** Prioritize auto-cook recipes (maxed proficiency) — fastest path to 20 dishes
  - **Completionist mode:** Prioritize recipes where proficiency is *not* maxed — completes BP task while simultaneously making progress toward cooking mastery. Best of both worlds for players working toward full recipe completion.

---

## 2. Team Compositions

**Recommendation: Yes, as user-defined storage — not AI-generated recommendations.**

The MCP server + Claude is better suited for recommending teams dynamically based on roster and builds. Hardcoding meta teams into reference data goes stale every version. However, storing the user's *actual settled teams* is valuable for two reasons:

1. **Conflict detection** — "Chiori is in both Team A and Team B; these teams cannot run simultaneously in Spiral Abyss"
2. **Artifact/weapon allocation awareness** — knowing which characters share a team informs priority decisions (e.g., who gets the EM goblet first)

### Data model

```json
{
  "teams": [
    {
      "name": "Lunar-Bloom",
      "characters": ["Lauma", "Nefer", "Jahoda", "SangonomiyaKokomi"],
      "notes": "S+ Lunar-Bloom core. Kokomi backup until Nefer obtained."
    },
    {
      "name": "Lunar-Crystallize",
      "characters": ["Zibai", "Chiori", "Illuga", "Aino"],
      "notes": "Triple Geo + Hydro applicator"
    }
  ]
}
```

### Scanning approach

Teams cannot be scanned directly — the game has no single team list screen. Two options:

- **Manual entry** in the app UI (user defines teams)
- **Party screen scan** — user navigates to each saved party, clicks scan; app OCRs the four character portraits and adds to team list

The party screen scan is lower confidence due to portrait OCR complexity; manual entry is more reliable initially.

### MCP tools enabled by team storage

- `AnalyzeTeam` (already planned in Milestone 3) becomes more useful when teams are pre-defined
- New: `CheckSpiralConflicts` — flag characters or artifacts assigned to two teams that cannot co-exist in a 36-floor Spiral run
- New: `TeamFarmingPriority` — given your defined teams, rank which characters need ascension/talent materials most urgently

---

## 3. Build Reference Data

**Recommendation: Community-contributed versioned JSON — not web scraping.**

Web scraping is brittle (sites restructure, rate-limit, or change markup), requires internet access, and risks ToS issues. Embedding static HTML parsers for KQM, Game8, or GameWith is maintenance-heavy.

The existing `inventorylists/*.json` pattern is the right model: structured, git-diffable, community-editable via PR, loaded once at startup.

### Proposed structure: `inventorylists/character_builds.json`

```json
{
  "version": "6.5",
  "characters": {
    "Lauma": {
      "roles": ["support", "reaction-trigger"],
      "builds": [
        {
          "name": "Lunar-Bloom EM Support",
          "priority": 1,
          "artifactSets": [
            {"set": "SilkenMoonsSerenade", "pieces": 4}
          ],
          "mainStats": {
            "sands": ["eleMas"],
            "goblet": ["eleMas"],
            "circlet": ["eleMas", "critRate_"]
          },
          "substatPriority": ["eleMas", "enerRech_", "critRate_", "critDMG_"],
          "notes": "EM main stats; 4pc SMS for party EM buff and Gleaming Moon effect"
        }
      ]
    },
    "Illuga": {
      "roles": ["support"],
      "builds": [
        {
          "name": "Lunar-Crystallize Support",
          "priority": 1,
          "artifactSets": [
            {"set": "SilkenMoonsSerenade", "pieces": 4}
          ],
          "mainStats": {
            "sands": ["eleMas", "enerRech_"],
            "goblet": ["eleMas"],
            "circlet": ["eleMas"]
          },
          "substatPriority": ["eleMas", "enerRech_", "def_"],
          "notes": "Buff scales with EM. Only Burst needs leveling. C2 enables off-field SMS proc."
        }
      ]
    }
  }
}
```

### Proposed structure: `inventorylists/character_reference.json`

```json
{
  "Lauma": {
    "element": "Dendro",
    "weapon": "catalyst",
    "ascension": {
      "gem": "NagadusEmerald",
      "boss": "LightbearingScaleFeather",
      "local": "MoonfallSilver",
      "mob": ["TatteredWarrant", "FrostEtchedWarrant", "ImmaculateWarrant"]
    },
    "talents": {
      "book": "Moonlight",
      "domain": "LightlessCapital",
      "domainDays": ["Monday", "Thursday", "Sunday"],
      "weeklyBoss": "LordOfErodedPrimalFire",
      "weeklyDrop": "ErodedScaleFeather"
    },
    "priorityTalents": ["skill", "burst"],
    "constellationNotes": {
      "C1": "Skip if Kokomi is in team — redundant healing",
      "C2": "Significant damage increase"
    }
  },
  "Illuga": {
    "element": "Geo",
    "weapon": "polearm",
    "ascension": {
      "gem": "PrithivaTopaz",
      "boss": "CyclicMilitaryKuuvahkiCore",
      "local": "PineAmber",
      "mob": ["BrokenDriveShaft", "PrecisionDriveShaft", "ReinforcedDriveShaft"]
    },
    "talents": {
      "book": "Elysium",
      "domain": "LightlessCapital",
      "domainDays": ["Tuesday", "Friday", "Sunday"],
      "weeklyBoss": "TBD",
      "weeklyDrop": "TBD"
    },
    "priorityTalents": ["burst"],
    "constellationNotes": {
      "C2": "Enables off-field SMS 4pc proc — significant team DPS increase",
      "C6": "Major buff potency increase"
    }
  }
}
```

### Why this approach

- **No internet required** — all reference data ships with the app
- **Community-maintainable** — same PR workflow as existing `inventorylists/`
- **Version-tagged** — when meta shifts (new sets, new characters), old entries aren't deleted, just superseded
- **MCP-queryable** — Claude can read these files directly without any scraping
- **AI can annotate** — Claude can suggest updates to `character_builds.json` based on conversation, which the user reviews and commits

---

## 4. Additional Reference Data Files

Beyond character builds, the following reference data files would eliminate the manual lookups required for AI-assisted planning:

### `inventorylists/domain_schedule.json`

Covers both talent book domains and weapon material domains on a unified weekday schedule.

```json
{
  "LightlessCapital": {
    "location": "LempoIsle, Nod-Krai",
    "type": "talent",
    "schedule": {
      "Monday":    ["Moonlight"],
      "Tuesday":   ["Elysium"],
      "Wednesday": ["Vagrancy"],
      "Thursday":  ["Moonlight"],
      "Friday":    ["Elysium"],
      "Saturday":  ["Vagrancy"],
      "Sunday":    ["Moonlight", "Elysium", "Vagrancy"]
    }
  },
  "ForsakenRift": {
    "location": "Mondstadt",
    "type": "weapon",
    "schedule": {
      "Monday":    ["Decarabian"],
      "Tuesday":   ["BorealWolf"],
      "Wednesday": ["DandelionGladiator"],
      "Thursday":  ["Decarabian"],
      "Friday":    ["BorealWolf"],
      "Saturday":  ["DandelionGladiator"],
      "Sunday":    ["Decarabian", "BorealWolf", "DandelionGladiator"]
    }
  }
}
```

**Cross-feature optimization — weekly domain planner:**

When combined with character ascension gaps, talent shortages, and weapon ascension needs, the domain schedule enables a full weekly farming plan:

- "Today is Tuesday — Lightless Capital has Elysium (Aino/Illuga talents) and ForsakenRift has Boreal Wolf (weapon material for X). Run both."
- "You need 4 more Guides of Vagrancy for Flins' talent — Wednesday is your next opportunity."
- "You have 10 weekly domain clears for BP — here is the optimal day-by-day assignment across all your material gaps."

The 10 weekly domain clears BP task maps directly onto this: given all pending material needs, assign domain runs to days of the week to maximize coverage across talent books, weapon materials, and artifact farming simultaneously.

---

## 6. Daily Activity Planner

The culmination of all scanning and reference data features — a unified daily activity plan that answers "what should I do in Genshin today?"

### Activity types covered

| Activity | BP Task | Build Value | Resin Cost |
|---|---|---|---|
| Talent domains | ✓ (10 weekly domains) | Talent books | 20 |
| Weapon material domains | ✓ (10 weekly domains) | Weapon ascension mats | 20 |
| Artifact domains | ✓ (10 weekly domains) | Artifact sets | 20 |
| World bosses | ✓ (weekly challenges) | Character ascension mats | 40 |
| Trounce domains (weekly bosses) | ✓ (3 weekly trounce clears) | Talent upgrade mats (weekly drops) | 30 (×3 max) |
| Ley Lines — Revelation | ✓ (use ley lines ×2) | Character XP materials | 20 |
| Ley Lines — Wealth | ✓ (use ley lines ×2) | Mora | 20 |
| Parametric Transformer | — | Talent books (via mob drops) | 0 |
| Commissions | ✓ (daily ×4) | Mora, XP, primogems | 0 |

### Planner logic

Given current state (BP scan + inventory scan + character build gaps), generate a prioritized daily to-do list:

1. **Commissions** — always 4, always worth doing (primogems + BP)
2. **Trounce domains** — 3 per week, high value (weekly talent mats + BP). Schedule early in the week.
3. **Resin allocation** — distribute remaining resin across:
   - Domains (talent books on correct weekday → build gaps)
   - Domains (weapon mats on correct weekday → build gaps)
   - World bosses (ascension mats → build gaps)
   - Ley Lines (Mora/XP if no domain/boss priority today)
   - Artifact domains (when ascension/talent gaps are filled)
4. **Parametric Transformer** — flag if weekly reset is available and suggest inputs

### Example daily output (MCP tool: `GetDailyPlan`)

```
Today: Tuesday

MUST DO:
  ✓ 4 Daily Commissions (primogems + BP)
  ✓ Parametric Transformer available — feed Chasmlight Fin × 3 + Lunar Fin × 5
    → Expected output: Guide-tier Elysium books (Aino/Illuga talent gap)

RESIN PRIORITY (160 resin available):
  1. Lightless Capital — Elysium stage (20 resin × 3 runs)
     → Aino needs 4 Guide of Elysium, Illuga needs 3
  2. Lord of Eroded Primal Fire — world boss (40 resin)
     → Lauma needs LightbearingScaleFeather × 3
  3. Ley Line — Wealth (20 resin × 2)
     → Mora reserve below threshold

WEEKLY PROGRESS:
  Trounce domains: 1/3 complete
  Domain clears: 3/10 complete
  Cook 20 dishes: 8/20 — run cookable proficiency list after domains
```

### Reference data additions needed

- `inventorylists/boss_reference.json` — world boss name, location, ascension drop(s), resin cost
- `inventorylists/trounce_reference.json` — weekly boss name, weekly talent drop(s), resin cost, which characters use each drop
- `inventorylists/leyline_reference.json` — locations per region, Revelation vs Wealth type
- Mora and XP threshold settings (user-configurable minimum reserves)

### `inventorylists/boss_drops.json`

Maps boss name → ascension material drop(s) → which characters use them.

### `inventorylists/weapon_reference.json`

Per weapon: type, substat, passive summary, recommended characters/roles, refinement value notes.

### `inventorylists/furnishing_sets.json`

Per furnishing set: required pieces, what each piece is, trust reward value. Used by the teapot scanning feature (Section 1.10).

### `inventorylists/achievements.json`

Per achievement: name, category, primogem reward, completion criteria summary, version added. Enables achievement gap analysis via MCP without requiring a full scan every session — user can scan once, diff against reference to find what's missing.

```json
{
  "version": "6.5",
  "achievements": [
    {
      "id": 840001,
      "name": "Wonders of the World",
      "category": "Wonders of the World",
      "primogems": 5,
      "description": "Unlock 1 achievement.",
      "versionAdded": "1.0"
    }
  ]
}
```

### `inventorylists/collectible_books.json`

Per book: title, region, total pages, page locations (general description). Used for the Quest Items collectible tracking feature (Section 1.9).

### `inventorylists/local_specialties.json`

Per region: specialty name, respawn timer, general location description, which characters use it for ascension.

---

## 5. MCP Tool Additions (Milestone 3 Extensions)

New tools enabled by the above reference data and scanning additions:

| Tool | Description |
|---|---|
| `GetFarmingSchedule` | Given a list of characters to build, return today's optimal domain/boss/mob farming route |
| `GetParametricSuggestion` | Based on current talent book shortages and material stockpile, recommend transformer inputs |
| `GetElixirCandidates` | Return unequipped unlocked lv4+ 5★ artifacts ranked by extraction priority |
| `CheckTeamConflicts` | Given user-defined teams, flag Spiral Abyss scheduling conflicts |
| `GetAscensionGaps` | For a character or list of characters, report missing ascension materials vs. inventory |
| `GetTalentGaps` | Report talent book shortages by domain day, factoring in transformer-eligible materials |
| `GetWeaponRefinements` | Show weapons with refinement upgrade opportunities from current duplicate inventory |
| `GetAchievementGaps` | List incomplete achievements by category, sorted by primogem value or proximity to completion |
| `GetUnclaimedRewards` | Surface completed achievements with unclaimed primogem rewards |
| `GetCollectibleProgress` | Report missing lore book pages and other collectible completion status |

---

*Feeds into: `MILESTONE_3_AI_ANALYSIS.md`, `ROADMAP.md`*
*Related conversation context: 2026-04-28/29 Genshin planning session*
