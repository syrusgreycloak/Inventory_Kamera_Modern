# Phase 2.5: GOOD v3 Format Support

## Overview

Add support for GOOD format version 3 by implementing optional fields introduced in the latest Genshin Optimizer schema.

**Current Status:** Inventory Kamera exports GOOD v2 with all required fields
**Target:** Full GOOD v3 compatibility with optional enhancement fields

---

## GOOD v3 New Fields

### Artifact Fields to Add

| Field | Type | Required | Current Status | Difficulty |
|-------|------|----------|----------------|------------|
| `elixirCrafted` | boolean | Optional | ✅ Detection exists | Easy |
| `unactivatedSubstats` | Substat[] | Optional | ✅ Text detection | Medium |
| `totalRolls` | number (0-9) | Optional | ❌ Needs calculation | Medium |
| `substats[].initialValue` | number | Optional | ❌ Need to track | Hard |
| `astralMark` | boolean | Optional | ❌ Need research | Unknown |

### Version Field Update

Change from:
```csharp
Version = 2  // Hardcoded
```

To:
```csharp
Version = 3  // GOOD v3
```

---

## Implementation Plan

### Step 1: elixirCrafted (EASY - Already have detection!)

**Current State:**
- `ArtifactScraper.IsSanctified()` already detects the purple elixir indicator
- Used only for coordinate shifting, not stored in artifact data

**Changes Needed:**

1. **Update Artifact.cs** - Add new optional field:
```csharp
[JsonProperty("elixirCrafted")]
public bool ElixirCrafted { get; internal set; }
```

2. **Update ArtifactScraper.cs** - Pass detection result to artifact:
```csharp
// In CatalogueFromBitmapsAsync method, after detecting sanctified:
bool isSanctified = IsSanctified(card);

// Later when creating artifact:
var artifact = new Artifact(
    setName, rarity, level, gearSlot, mainStat, subStats,
    equippedCharacter, id, locked
);
artifact.ElixirCrafted = isSanctified;  // NEW LINE
```

**Testing:**
- Scan both regular and sanctified artifacts
- Verify JSON output includes `"elixirCrafted": true` for sanctified ones
- Verify regular artifacts have `"elixirCrafted": false`

**Acceptance Criteria:**
- ✅ `IsSanctified()` result stored in `Artifact.ElixirCrafted`
- ✅ Field appears in GOOD JSON export
- ✅ Works for both 16:9 and 16:10 aspect ratios

---

### Step 2: astralMark (RESEARCH NEEDED)

**Research Questions:**
1. What is "astral mark" in Genshin Impact?
2. Does it appear on artifact cards visually?
3. Is it available in current game version (6.4)?
4. How would we detect it via OCR/image analysis?

**Action Items:**
- [ ] Search Genshin Impact wiki/patch notes for "astral mark" or "astral" system
- [ ] Check if it's related to specific artifact sets or game mechanics
- [ ] Look for screenshots showing astral marked artifacts
- [ ] Determine if it's a future feature or already implemented

**Possible Scenarios:**

**A) Feature doesn't exist yet:**
- Add field to schema with default `false`
- Document that detection will be added when feature releases

**B) Feature exists but no visual indicator:**
- May require game data extraction (not OCR-able)
- Consider leaving unimplemented until indicator exists

**C) Feature exists with visual indicator:**
- Implement detection similar to `IsSanctified()`
- Add region detection to ScanProfile.json

**Placeholder Implementation:**
```csharp
[JsonProperty("astralMark")]
public bool AstralMark { get; internal set; } = false;  // Default false until we know more
```

---

### Step 3: totalRolls (MEDIUM - Reshape screen navigation required)

**What is totalRolls?**
Total number of substat upgrade rolls (0-9) an artifact has received.

**Background:**
- Artifacts start with 3-4 substats at level 0
- Every 4 levels (0→4→8→12→16→20), one substat gets upgraded
- 5 total upgrade opportunities for level 20 artifacts
- **IMPORTANT:** `totalRolls` counts upgrade rolls only, NOT initial substats

**DISCOVERY: Reshape Screen Shows Individual Roll Counts!**

For **5-star level 20 artifacts**, the game provides a "Reshape" screen that displays individual roll counts per substat!

**Reshape Screen Details:**
1. Only available for **5★ artifacts at level 20**
2. Access: Click "Details" button (lower right of artifact card)
3. Click "Reshape" option in left menu
4. Right side shows substats with **roll count badges** (circles with numbers)

**Example from Reshape screen:**
```
ATK      5.8%     (no badge shown)
CRIT Rate 12.4%   ③ (3 rolls)
CRIT DMG  12.4%   ① (1 roll)
DEF      5.1%     (no badge shown)
```

**Understanding totalRolls for Level 20 Artifacts:**

Level 20 artifacts have had 5 upgrade opportunities (at levels 4, 8, 12, 16, 20):
- **Started with 3 substats:** First upgrade **activates** 4th substat (not a roll), then 4 upgrades roll substats → **totalRolls = 4**
- **Started with 4 substats:** All 5 upgrades roll substats → **totalRolls = 5**

**Therefore: totalRolls for level 20 is ALWAYS 4 or 5** (sum of badge numbers only)

**Reshape Screen Display:**
- Substats with upgrade rolls show numbered badges: ①②③④⑤
- Substats with no upgrade rolls show bullet points: •
- **Badge numbers = upgrade rolls, bullet points = 0 rolls**

**Example from screenshot:**
```
ATK      5.8%     • (0 upgrade rolls)
CRIT Rate 12.4%   ③ (3 upgrade rolls)
CRIT DMG  12.4%   ① (1 upgrade roll)
DEF      5.1%     • (0 upgrade rolls)

Badge sum = 3 + 1 = 4 → totalRolls = 4
(This artifact started with 3 substats, 4th activated at level 4)
```

```csharp
// For 5★ level 20 artifacts scanned via Reshape screen:
// Sum ONLY the badge numbers (bullets contribute 0)
int totalRolls = rollCounts.Values.Sum();
// Example: 3 + 1 + 0 + 0 = 4 (started with 3 substats)
// Example: 2 + 1 + 1 + 1 = 5 (started with 4 substats)
// Example: 1 + 1 + 1 + 2 = 5 (started with 4 substats)

// Bonus: We can determine initial substat count!
int initialSubstatCount = (totalRolls == 4) ? 3 : 4;
```

**Implementation Approach:**

**Option A: Optional "Deep Scan" Mode** (Recommended)
```csharp
if (Settings.Default.ScanRollCounts && artifact.Rarity == 5 && artifact.Level == 20)
{
    // Navigate to Reshape screen
    Navigation.ClickDetailsButton();
    Navigation.SystemWait(Navigation.Speed.Normal);
    Navigation.ClickReshapeOption();
    Navigation.SystemWait(Navigation.Speed.Normal);

    // Scan individual roll counts from badges
    Dictionary<string, int> rollCounts = ScanReshapeRollCounts();

    // Calculate total
    artifact.TotalRolls = rollCounts.Values.Sum();

    // Store individual counts for potential future use
    foreach (var substat in artifact.SubStats)
    {
        if (rollCounts.ContainsKey(substat.Key))
            substat.RollCount = rollCounts[substat.Key];
    }

    // Navigate back
    Navigation.ClickBackButton();
    Navigation.SystemWait(Navigation.Speed.Normal);
}
else
{
    // Use heuristic for non-5★ or non-level-20 artifacts
    artifact.TotalRolls = EstimateTotalRolls(artifact);
}
```

**Option B: Two-Pass Scanning**
1. **Pass 1:** Quick scan of all artifacts (current behavior)
2. **Pass 2 (optional):** Re-scan 5★ level 20 artifacts via Reshape screen

**Challenges:**

1. **Slower Scanning:**
   - Extra navigation steps per artifact
   - Click Details → Wait → Click Reshape → Wait → Scan → Click Back → Wait
   - Could add ~2-3 seconds per 5★ level 20 artifact

2. **OCR of Circular Badges:**
   - Numbers appear in small circles
   - May need different preprocessing than rectangular regions
   - Could use circular crop + threshold for badge detection

3. **UI Element Detection:**
   - Need to locate "Details" button position
   - Need to locate "Reshape" option position
   - Need to locate roll count badge positions

4. **Not All Substats Have Badges:**
   - Only upgraded substats show roll counts
   - Initial substats (never rolled) have no badge
   - Need to handle missing badges (count = 0)

**Recommended Settings Option:**
```
☐ Scan individual roll counts (slower)
  Only for 5-star level 20 artifacts
  Adds ~2 seconds per eligible artifact
```

**Implementation Priority:**
- **Phase 2.5.1:** Use heuristic calculation (fast, good enough)
- **Phase 2.5.3:** Add Reshape screen scanning (optional, accurate)

**Heuristic Fallback (for non-level-20 or when Reshape scanning disabled):**
```csharp
private int? CalculateTotalRolls(Artifact artifact)
{
    // Can only accurately determine for level 0-3 artifacts
    if (artifact.Level < 4)
    {
        // No upgrades have happened yet, so totalRolls = 0
        return 0;
    }

    // For level 4-16: We can't know for sure without Reshape screen
    // because we don't know if the artifact started with 3 or 4 substats
    // Better to return null than guess incorrectly
    if (artifact.Level < 20)
    {
        return null;  // Omit field for levels 4-16
    }

    // For level 20: Should only reach here if Reshape scanning is disabled
    // We know it's either 4 or 5, but can't tell which without Reshape screen
    // Could make an educated guess based on substat count, but might be wrong
    return null;  // Recommend enabling Reshape scanning for level 20 artifacts
}
```

**Recommendation:**
- **Level 0-3:** totalRolls = 0 (no upgrades yet)
- **Level 4-16:** Omit field (can't determine without knowing initial substat count)
- **Level 20:** Use Reshape screen scanning (Phase 2.5.3) for accurate data
  - If Reshape disabled: Could guess 4 or 5, but better to omit field

---

### Step 4: substats[].initialValue (NOT IMPLEMENTABLE - Fundamental limitations)

**What is initialValue?**
The starting value of a substat before any upgrade rolls.

**Example:**
```json
{
  "key": "critRate_",
  "value": 10.5,        // Current value after upgrades
  "initialValue": 3.1   // Value when artifact was first obtained
}
```

**Why We Can't Implement This:**

**Problem 1: Only Capturable for 5★ Level 0 Artifacts**
- Only 5-star artifacts show "(unactivated)" substats at level 0-3
- 3-star and 4-star artifacts don't show unactivated text
- We can only capture initial values for a tiny subset of artifacts

**Problem 2: No Persistent Artifact Identity**
To track initial values, we'd need to:
1. Scan artifact at level 0 → capture initial values
2. Scan same artifact at level 20 → link to previous scan
3. Populate `initialValue` from the level 0 data

**But we can't link scans because:**

```csharp
// Current ID field is just scan position index
"id": 0  // First artifact on screen during this scan

// Problems:
// - Not persistent (changes when inventory sorted/filtered)
// - Not unique (same position in different scans = different artifacts)
// - Changes when artifacts leveled/moved/locked
```

**Problem 3: Can't Generate Persistent Hash**

**Attempted hash approach:**
```csharp
string hash = $"{setKey}_{slotKey}_{mainStatKey}_{substat1Key}_{substat2Key}_{substat3Key}_{substat4Key}";
// Example: "GladiatorsFinale_flower_hp_critRate_critDMG_atk_def"
```

**This has collisions!**
```json
// Artifact A
{"setKey": "GladiatorsFinale", "slotKey": "flower", "mainStatKey": "hp",
 "substats": [
   {"key": "critRate_", "value": 3.1},
   {"key": "critDMG_", "value": 5.4},
   {"key": "atk_", "value": 16},
   {"key": "def", "value": 19}
 ]}

// Artifact B (DIFFERENT artifact, SAME hash!)
{"setKey": "GladiatorsFinale", "slotKey": "flower", "mainStatKey": "hp",
 "substats": [
   {"key": "critRate_", "value": 7.8},  // Different value
   {"key": "critDMG_", "value": 13.2},  // Different value
   {"key": "atk_", "value": 11},        // Different value
   {"key": "def", "value": 23}          // Different value
 ]}
```

**Can't include values in hash because they change as artifact levels up!**

**Problem 4: Reverse-Engineering is Unreliable**

Even if we had game data for possible roll values:
```
Current Crit Rate: 10.5%
Level: 12 (3 upgrades)
Possible rolls: 2.7%, 3.1%, 3.5%, 3.9%

Combinations that sum to 10.5%:
- 3.1% + 3.9% + 3.5% = 10.5% ✓
- 3.5% + 3.5% + 3.5% = 10.5% ✓
- 2.7% + 3.9% + 3.9% = 10.5% ✓

Which one is correct? Impossible to know!
```

**The Fundamental Issue:**

Without a **game-assigned persistent artifact ID** (which we can't get via OCR), we have no way to:
1. Link the same artifact across multiple scans
2. Store historical scan data and match it later
3. Reliably populate `initialValue` for upgraded artifacts

**What Would Be Required:**

1. **Game Memory Reading** - Access actual artifact IDs from game memory (not OCR)
2. **Persistent Database** - Store every scan with timestamps
3. **Matching Algorithm** - Probabilistic matching based on:
   - Time between scans
   - Value changes consistent with rolls
   - User manually confirming matches

This is beyond the scope of an OCR-based scanner.

**Recommendation:**

**DO NOT implement `substats[].initialValue`**
- Omit field entirely from exports
- Document limitation in README
- Note that GOOD v3 schema marks this as optional anyway
- Genshin Optimizer doesn't require it for core functionality

**If future game updates provide persistent IDs via UI:**
Then we could implement this. But currently: **not feasible**.

---

### Step 5: unactivatedSubstats (MEDIUM - Text detection available!)

**What are unactivatedSubstats?**
Substats that exist but haven't been revealed yet (artifacts below level 4).

**Game Mechanic:**
- Artifacts with 3 initial substats gain a 4th at level 4
- Before level 4, the 4th substat exists but is shown grayed out
- **Key Discovery:** Unactivated substats display "(unactivated)" text on the line!

**Visual Indicators:**
1. **Primary Signal:** Text explicitly contains "(unactivated)"
2. **Secondary Signal:** Line is lighter/grayed out compared to active substats

**Detection Strategy:**

The existing substat scanner already reads all 4 substat lines. We just need to:

```csharp
private void ParseSubstats(string ocrText, out List<SubStat> activeSubstats, out List<SubStat> unactivatedSubstats)
{
    activeSubstats = new List<SubStat>();
    unactivatedSubstats = new List<SubStat>();

    string[] lines = ocrText.Split('\n');

    foreach (string line in lines)
    {
        // Check if this is an unactivated substat
        bool isUnactivated = line.Contains("(unactivated)", StringComparison.OrdinalIgnoreCase);

        // Strip "(unactivated)" text before parsing
        string cleanLine = line.Replace("(unactivated)", "").Trim();

        // Parse the substat (existing logic)
        SubStat substat = ParseSubstatLine(cleanLine);

        if (substat != null)
        {
            if (isUnactivated)
                unactivatedSubstats.Add(substat);
            else
                activeSubstats.Add(substat);
        }
    }
}
```

**OCR Preprocessing Consideration:**

Current character name preprocessing uses:
```csharp
GenshinProcesor.SetContrast(60, ref n);
GenshinProcesor.SetThreshold(200, ref n);  // High threshold for pure white text
```

**Potential Issue:** Threshold 200 is tuned for bright white text. Unactivated substats are lighter/grayed, which might get filtered out.

**Solution Options:**

1. **Use lower threshold for substat region** (recommended):
```csharp
// For substats, use lower threshold to catch grayed text
GenshinProcesor.SetThreshold(150, ref n);  // Lower than 200
```

2. **Brightness pre-check before threshold:**
```csharp
// Check if this is a lighter line (unactivated)
double avgBrightness = CalculateAverageBrightness(substatLine);
if (avgBrightness < 180)  // Lighter than normal
{
    // Apply gentler preprocessing
    GenshinProcesor.SetThreshold(120, ref n);
}
```

**Implementation Steps:**

1. Update `ArtifactScraper.ParseSubstats()` to detect "(unactivated)" text
2. Test OCR with current preprocessing settings on grayed text
3. Adjust threshold if needed (lower for lighter text)
4. Route parsed substats to correct array based on "(unactivated)" presence
5. Update `Artifact.cs` to include `UnactivatedSubstats` property

**Example Output:**
```json
{
  "substats": [
    {"key": "critRate_", "value": 3.1},
    {"key": "hp", "value": 299},
    {"key": "def_", "value": 5.8}
  ],
  "unactivatedSubstats": [
    {"key": "atk_", "value": 4.7}  // Detected via "(unactivated)" text
  ],
  "level": 0,
  "elixirCrafted": false
}
```

**Acceptance Criteria:**
- ✅ OCR successfully reads "(unactivated)" text on grayed lines
- ✅ Substats with "(unactivated)" routed to separate array
- ✅ Text stripped before parsing stat name/value
- ✅ Works with both normal and grayed text preprocessing
- ✅ Edge case: Artifacts with no unactivated substats don't include the field

---

### Step 6: Update Version to 3

**Current:**
```csharp
// GOOD.cs constructor
Format = "GOOD";
Version = 2;  // Hardcoded
```

**Change to:**
```csharp
Format = "GOOD";
Version = 3;  // Updated to support v3 fields
```

**Backwards Compatibility:**
GOOD v3 is backwards compatible with v2. Tools that expect v2 can still read v3 exports because:
- All required v2 fields are still present
- New v3 fields are optional
- Schema is additive, not breaking

---

## Implementation Priority

### Phase 2.5.1 - Quick Wins (1-2 days)
1. ✅ Add `elixirCrafted` field (detection already exists)
2. ✅ Add `unactivatedSubstats` field (text detection via "(unactivated)")
3. ✅ Update version to 3
4. ✅ Add `astralMark` with default `false` (pending research)
5. ✅ Test with Genshin Optimizer import

### Phase 2.5.2 - Enhancement Fields (Future)
6. ⏳ Research `astralMark` feature
7. ⏳ Implement `totalRolls` calculation (after initialValue)
8. ⏳ Add `substats[].initialValue` (nullable/optional)

---

## Testing Strategy

### Test Cases

**Test 1: Regular Artifact (Non-Sanctified)**
```json
{
  "setKey": "GladiatorsFinale",
  "slotKey": "flower",
  "level": 20,
  "rarity": 5,
  "mainStatKey": "hp",
  "substats": [
    {"key": "critRate_", "value": 10.5},
    {"key": "critDMG_", "value": 21.0},
    {"key": "atk_", "value": 16.0},
    {"key": "def", "value": 19}
  ],
  "elixirCrafted": false,
  "astralMark": false,
  "location": "",
  "lock": true
}
```

**Test 2: Sanctified Artifact (Elixir-Crafted)**
```json
{
  "setKey": "GoldenTroupe",
  "slotKey": "circlet",
  "level": 20,
  "rarity": 5,
  "mainStatKey": "critRate_",
  "substats": [...],
  "elixirCrafted": true,  // ← Detected via purple indicator
  "astralMark": false,
  "location": "Arlecchino",
  "lock": true
}
```

**Test 3: Low-Level Artifact**
```json
{
  "setKey": "ViridescentVenerer",
  "slotKey": "sands",
  "level": 0,
  "rarity": 5,
  "mainStatKey": "atk_",
  "substats": [
    {"key": "critRate_", "value": 3.1},
    {"key": "hp", "value": 299},
    {"key": "def_", "value": 5.8}
  ],
  "elixirCrafted": false,
  "astralMark": false,
  "location": "",
  "lock": false
  // Note: Has 3 substats, will gain 4th at level 4
  // Could include "unactivatedSubstats": [] if we implement it
}
```

### Import Testing

1. Export GOOD v3 JSON from Inventory Kamera
2. Import into Genshin Optimizer (https://frzyc.github.io/genshin-optimizer/)
3. Verify:
   - All artifacts load correctly
   - No validation errors
   - Sanctified artifacts display properly
   - New fields don't break optimizer calculations

---

## Code Changes Summary

### Files to Modify

1. **InventoryKamera/game/Artifact.cs**
   - Add `ElixirCrafted` property
   - Add `AstralMark` property (default false)
   - Add `TotalRolls` property (optional, nullable)
   - Update constructor

2. **InventoryKamera/game/SubStat.cs** (if it exists as separate class)
   - Add `InitialValue` property (nullable)

3. **InventoryKamera/scraping/ArtifactScraper.cs**
   - Pass `isSanctified` result to artifact object
   - Set `artifact.ElixirCrafted = isSanctified`

4. **InventoryKamera/data/GOOD.cs**
   - Change `Version = 2` to `Version = 3`

5. **README.md**
   - Update GOOD format version in documentation
   - List supported v3 fields
   - Note limitations (initialValue, unactivatedSubstats, astralMark)

---

## Acceptance Criteria

### Phase 2.5.1 (Required)
- [ ] `elixirCrafted` field added to Artifact class
- [ ] Sanctified detection result stored in `elixirCrafted`
- [ ] `unactivatedSubstats` field added to Artifact class
- [ ] Substat parser detects "(unactivated)" text
- [ ] OCR preprocessing tested/tuned for grayed text
- [ ] Unactivated substats routed to separate array
- [ ] `astralMark` field added (default `false`)
- [ ] Version updated to 3 in GOOD export
- [ ] Exported JSON validates against GOOD v3 schema
- [ ] Successfully imports into Genshin Optimizer
- [ ] All existing v2 functionality still works
- [ ] Unit tests for new fields

### Phase 2.5.2 (Optional/Future)
- [ ] `totalRolls` heuristic calculation (fast, good enough for most cases)
- [ ] `astralMark` detection (pending research on game feature)

### Phase 2.5.3 (Reshape Screen Integration - RECOMMENDED)
- [ ] Reshape screen navigation for 5★ level 20 artifacts
- [ ] OCR individual roll count badges (circular regions)
- [ ] UI element detection (Details button, Reshape option)
- [ ] Settings toggle: "Scan roll counts (slower)"
- [ ] Accurate `totalRolls` from individual roll sum
- [ ] Individual roll counts per substat (store in custom field)
- [ ] Determine initial substat count (3 or 4) from totalRolls

### Phase 2.5.4 (Reverse-Engineer initialValue - ENABLES PHASE 3)
- [ ] Load game data for possible roll values per stat/rarity
- [ ] Implement reverse-engineering algorithm
- [ ] Calculate `initialValue` from current value - (roll count × roll values)
- [ ] Validate against known roll value ranges
- [ ] Handle edge cases (multiple valid combinations)

**Why This Matters:**
Reverse-engineered `initialValue` unlocks **Phase 3 optimization features**:
- ✅ Genshin Optimizer integration for "should I reshape?" analysis
- ✅ In-app artifact calculator (if scan data stored in SQLite)
- ✅ AI-powered artifact recommendations (Phase 3)
- ✅ Historical tracking of artifact upgrades
- ✅ Reshape cost/benefit analysis

**Reverse-Engineering Algorithm:**
```csharp
public double? CalculateInitialValue(SubStat substat, int rollCount)
{
    if (rollCount == 0)
        return substat.Value;  // No rolls = current value IS initial value

    // Get possible roll values for this stat at this rarity
    List<double> possibleRolls = GameData.GetRollValues(substat.Key, artifact.Rarity);

    // Try to find combination of rolls that sum to (currentValue - initialValue)
    // Example: Crit Rate at 12.4%, 3 rolls, possible values: [2.7%, 3.1%, 3.5%, 3.9%]
    //   Could be: 3.1% initial + (3.5% + 3.1% + 2.7%) = 12.4%
    //   Or: 3.5% initial + (3.1% + 3.1% + 2.7%) = 12.4%

    // Brute force all combinations (feasible since max 5 rolls)
    var validCombinations = FindRollCombinations(substat.Value, rollCount, possibleRolls);

    if (validCombinations.Count == 1)
    {
        // Unambiguous - only one valid combination
        return validCombinations[0].InitialValue;
    }
    else if (validCombinations.Count > 1)
    {
        // Multiple valid combinations - use heuristic:
        // Most common initial values are lowest tier rolls (2.7%, 5.4%, etc.)
        return validCombinations.OrderBy(c => c.InitialValue).First().InitialValue;
    }
    else
    {
        // No valid combination found - data error or rounding issue
        return null;
    }
}
```

**Game Data Required:**
```json
{
  "rollValues": {
    "5star": {
      "critRate_": [2.7, 3.1, 3.5, 3.9],
      "critDMG_": [5.4, 6.2, 7.0, 7.8],
      "atk_": [4.1, 4.7, 5.3, 5.8],
      "def_": [5.1, 5.8, 6.6, 7.3],
      "hp_": [4.1, 4.7, 5.3, 5.8],
      "eleMas": [16, 19, 21, 23],
      "enerRech_": [4.5, 5.2, 5.8, 6.5],
      "hp": [209, 239, 269, 299],
      "atk": [14, 16, 18, 19],
      "def": [16, 19, 21, 23]
    },
    "4star": { /* ... */ },
    "3star": { /* ... */ }
  }
}
```

---

## Migration Notes

**For Users:**
- No breaking changes
- Old GOOD v2 exports will still work in Genshin Optimizer
- New v3 exports provide additional data for optimization
- No action required from users

**For Developers:**
- All new fields are optional
- Null/undefined handling for optional fields
- Backwards compatible with existing artifact data
- Can incrementally add detection for advanced fields

---

## Future Enhancements (Beyond Phase 2.5)

### Phase 3: Artifact Optimization Features
**Enabled by Phase 2.5.4 (initialValue + rollCount data)**

#### 3.1: SQLite Scan History Database
```sql
CREATE TABLE artifacts (
    scan_id INTEGER,
    scan_timestamp DATETIME,
    artifact_hash TEXT,  -- setKey_slotKey_mainStatKey_substat1_substat2_substat3_substat4
    set_key TEXT,
    slot_key TEXT,
    level INTEGER,
    rarity INTEGER,
    main_stat_key TEXT,
    elixir_crafted BOOLEAN,
    total_rolls INTEGER,
    location TEXT,
    locked BOOLEAN
);

CREATE TABLE substats (
    artifact_id INTEGER,
    substat_key TEXT,
    current_value REAL,
    initial_value REAL,  -- From Phase 2.5.4
    roll_count INTEGER,  -- From Phase 2.5.3
    FOREIGN KEY (artifact_id) REFERENCES artifacts(id)
);
```

**Benefits:**
- Query artifact history: "Show me all Crit Rate circlets I've ever scanned"
- Track artifact progression: "Has this artifact been upgraded since last scan?"
- Identify duplicate artifacts across scans
- Generate statistics: "Average crit value on my level 20 artifacts"

#### 3.2: In-App Reshape Calculator
```
┌─────────────────────────────────────────────────┐
│ Reshape Recommendation                          │
├─────────────────────────────────────────────────┤
│ Artifact: Gladiator's Finale - Circlet          │
│ Current Stats:                                   │
│   ⭐ CRIT Rate (main)           31.1%           │
│   • CRIT DMG    12.4% (③ rolls)                 │
│   • ATK%         5.8% (① roll)                  │
│   • DEF         19    (no rolls)                │
│   • HP          299   (no rolls)                │
│                                                  │
│ Reshape Cost: 1 Sanctifying Elixir              │
│                                                  │
│ Potential Outcomes:                              │
│   Best Case:  CRIT DMG → 28.0% (+4 rolls)       │
│   Expected:   CRIT DMG → 21.8% (+3 rolls)       │
│   Worst Case: CRIT DMG → 15.6% (+2 rolls)       │
│                                                  │
│ Recommendation:                                  │
│ ✅ RESHAPE - Low initial CRIT DMG has high      │
│    upside potential. 67% chance of gaining      │
│    at least 2 rolls to CRIT DMG.                │
│                                                  │
│ [ Reshape Now ]  [ Keep Current ]  [ Simulate ] │
└─────────────────────────────────────────────────┘
```

**Algorithm:**
```csharp
public ReshapeRecommendation AnalyzeReshape(Artifact artifact, string targetCharacter)
{
    // Get character's optimal stat priorities
    var statPriorities = CharacterDatabase.GetStatPriorities(targetCharacter);

    // Calculate current artifact value score
    double currentScore = CalculateArtifactScore(artifact, statPriorities);

    // Simulate 1000 reshape outcomes based on:
    // - initialValue for each substat (from Phase 2.5.4)
    // - Possible roll distributions (4-5 rolls total)
    // - Stat priority weights for character
    var simulations = SimulateReshapeOutcomes(artifact, statPriorities, 1000);

    // Calculate expected value
    double expectedScore = simulations.Average(s => s.Score);
    double improvementChance = simulations.Count(s => s.Score > currentScore) / 1000.0;

    return new ReshapeRecommendation
    {
        ShouldReshape = expectedScore > currentScore * 1.15, // 15% improvement threshold
        CurrentScore = currentScore,
        ExpectedScore = expectedScore,
        ImprovementChance = improvementChance,
        BestCase = simulations.Max(s => s.Score),
        WorstCase = simulations.Min(s => s.Score)
    };
}
```

#### 3.3: AI-Powered Artifact Recommendations
**Uses scan history + character roster + stat_priorities.json**

The AI can read build information from `stat_priorities.json` to provide context-aware recommendations:

```csharp
// AI has access to build descriptions and priorities
var buildInfo = StatPriorityManager.GetBuildDescription("zhongli", "shieldbot");
// Returns: "Maximum shield strength for team protection, ignore damage stats entirely"

var priorities = StatPriorityManager.GetStatPriorities("zhongli", "shieldbot");
// Returns: hp_: 10, hp: 7, enerRech_: 7, critRate_: 0...
```

**Example AI Response:**

```
"You have Zhongli equipped with artifacts optimized for burst DPS (CRIT Rate/DMG focus),
but I see you're running him in a team with Hu Tao and Yelan.

Based on your team composition and the 'shieldbot' build definition in stat_priorities.json
('Maximum shield strength for team protection, ignore damage stats entirely'), I recommend:

1. Switch Zhongli's build preset from 'burstdps' to 'shieldbot'
2. This changes priority from CRIT stats (value: 10) to HP% (value: 10)

Your best HP% artifacts for shieldbot Zhongli:
 • Tenacity Sands - HP% main, HP substat ③, ER% ②  (Score: 95/100)
 • Tenacity Goblet - HP% main, HP substat ②, ER% ③  (Score: 92/100)
 • Tenacity Circlet - HP% main, HP substat ①, DEF% ④ (Score: 78/100)

Switching to shieldbot build will increase shield strength by ~40% but reduce burst
damage by 60%. For your team (Hu Tao carry), the shield value is more important than
Zhongli's personal damage output.

Would you like me to:
1. Show all HP% artifacts for Zhongli
2. Calculate optimal 4pc Tenacity set
3. Evaluate reshape candidates for HP% circlet improvements"
```

**AI Context from stat_priorities.json:**
- Build name: "Shield Support" vs "Burst DPS Hybrid"
- Role: "Shield Support" vs "Sub DPS"
- Description: AI understands WHY stats are prioritized
- Priorities: AI can score artifacts per build

This enables the AI to:
- Recommend build switches based on team composition
- Explain trade-offs ("40% more shield, 60% less damage")
- Suggest specific artifacts from inventory
- Generate reshape priority queues per build

### Phase 3.4: Astral Mark System (TBD)
- Research game mechanics
- Implement visual detection
- Add to ScanProfile regions

---

## Questions for User

1. **Priority:** Which fields are most important for Phase 2.5.1?
   - elixirCrafted (easy, already detected) ✅
   - unactivatedSubstats (medium, text detection) ✅
   - astralMark (research needed) ❓
   - totalRolls (heuristic = easy, accurate = slow) ⏳

2. **Testing:** Do you have access to sanctified artifacts to test with?

3. **Timeline:** Is Phase 2.5 blocking Phase 1.5 (.NET 8 migration), or can they run in parallel?

4. **Genshin Version:** What game version are you currently running? (To verify which features are available)

5. **Reshape Scanning:** Should we implement Reshape screen scanning in Phase 2.5?
   - **Pros:** Accurate roll counts, enables reverse-engineering initialValue
   - **Cons:** Significantly slower (2-3s per 5★ level 20 artifact), more complex navigation
   - **Recommendation:** Implement heuristic first (Phase 2.5.2), add Reshape scanning later (Phase 2.5.3) as optional feature
