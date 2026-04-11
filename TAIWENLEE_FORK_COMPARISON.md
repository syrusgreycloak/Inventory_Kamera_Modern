# Comparison: Our Fork vs taiwenlee/Inventory_Kamera

## Overview

The taiwenlee fork is the actively maintained version that Genshin Optimizer points to. They're at version **1.4.2** while we're at **1.3.17**.

## Critical Findings

### 🔴 THEY HAVE THE SAME DATA LOSS BUG WE JUST FIXED

**Location**: `DatabaseManager.cs` lines 353, 548, 636, 678

```csharp
if (force) File.Delete(ListsDir + CharactersJson);  // ❌ DELETES FIRST
if (force) File.Delete(ListsDir + ArtifactsJson);   // ❌ DELETES FIRST
if (force) File.Delete(ListsDir + WeaponsJson);     // ❌ DELETES FIRST
if (force) File.Delete(ListsDir + MaterialsJson);   // ❌ DELETES FIRST
```

They have the **exact same optimistic delete anti-pattern** we just fixed. Their forced database updates will result in 0-byte files if processing fails.

**Impact**: This is a production-breaking bug in the version Genshin Optimizer recommends.

## Features They Have That We Don't

### 1. Mannequin Character Handling (CRITICAL)

**Added in**: Commits cb140ed, de56152
**Problem**: Mannequins crash the scanner because Genshin Optimizer doesn't support them
**Their fix**:
- Added `Manequin1Name` and `Manequin2Name` settings
- Skip mannequins during character scanning (`if(character.NameGOOD != "manequin")`)
- Added mannequins to valid character list to prevent errors

**Files affected**:
- `InventoryKamera/scraping/CharacterScraper.cs` - Skip mannequin scanning
- `InventoryKamera/scraping/GenshinProcesor.cs` - Add mannequins to valid characters
- `InventoryKamera/data/InventoryKamera.cs` - Assign mannequin names
- `InventoryKamera/Properties/Settings.Designer.cs` - Add mannequin name settings
- UI changes for mannequin name input fields

**Why we need this**: Without this, users with mannequins in their character roster will experience scanner crashes.

### 2. Non-Playable Character Filter (CRITICAL)

**Added in**: Commit 43c1612
**Problem**: Columbina and Ineffa (NPCs with character IDs > 10000900) cause database update errors
**Their fix**:

```csharp
if (characterID > 10000900) return; // Not playable characters
```

**Location**: `DatabaseManager.cs:383`

**Why we need this**: Prevents database corruption from NPC data that shouldn't be in the character database.

### 3. TextMap_MediumEN.json Support (CRITICAL)

**Added in**: Commit 77a589a
**Problem**: Dimbreath split TextMap into two files (TextMapEN.json and TextMap_MediumEN.json)
**Their fix**: Load both TextMap files and merge them

```csharp
var mapping = JObject.Parse(LoadJsonFromURLAsync(TextMapEnURL))
    .ToObject<Dictionary<string, string>>()
    .Where(e => !string.IsNullOrWhiteSpace(e.Value))
    .ToDictionary(i => i.Key, i => i.Value);

var mediumMapping = JObject.Parse(LoadJsonFromURLAsync(TextMapMediumEnURL))
    .ToObject<Dictionary<string, string>>()
    .Where(e => !string.IsNullOrWhiteSpace(e.Value))
    .ToDictionary(i => i.Key, i => i.Value);

foreach (var entry in mediumMapping)
{
    mapping[entry.Key] = entry.Value; // Should be no overlap
}

Mappings = new ConcurrentDictionary<string, string>(mapping);
```

**Location**: `DatabaseManager.cs:261-278`

**Why we need this**: Without this, we're missing hash mappings for newer characters/items that are only in TextMap_MediumEN.json. This could explain why we're seeing "hash not found" warnings for recent content.

### 4. Better Constellation Order Detection

**Their approach** (lines 456-485):
- Check **both C3 AND C5 independently** instead of assuming pairs
- Define `auto = "Normal Attack"` and check if constellation description contains it
- More flexible logic that doesn't assume C3/C5 are always paired

```csharp
string auto = "Normal Attack";

// Check C3
if (const3Description.Contains(auto))
    constellationOrder.Add("auto");
else if (const3Description.Contains(skill))
    constellationOrder.Add("skill");
else
    constellationOrder.Add("burst");

// Check C5
if (const5Description.Contains(auto))
    constellationOrder.Add("auto");
else if (const5Description.Contains(skill))
    constellationOrder.Add("skill");
else
    constellationOrder.Add("burst");
```

**Our approach** (lines 463-479):
- Check C3, then assume C5 based on C3 result
- Check for "Normal Attack" OR "Charged Attack" keywords
- Assumes C3/C5 are always paired (auto+burst, skill+burst, or burst+skill)

```csharp
if (const3Description.Contains("Normal Attack") || const3Description.Contains("Charged Attack"))
{
    constellationOrder.Add("auto");
    constellationOrder.Add("burst");
}
else if (const3Description.Contains(skill))
{
    constellationOrder.Add("skill");
    constellationOrder.Add("burst");
}
else
{
    constellationOrder.Add("burst");
    constellationOrder.Add("skill");
}
```

**Their approach is more robust** - it handles edge cases where C3/C5 might not follow the expected patterns.

### 5. Region Calculation Adjustments

**Added in**: Commit 8487414
**Changes**: Adjusted region calculations for character scraping
**Impact**: Improved scraping accuracy for different resolutions

### 6. Artifact Scraping Improvements

**Added in**: Commit 63ba93f
**Changes**: Improved artifact scraping reliability and logging
**Impact**: More reliable artifact scanning with better error messages

## Features We Have That They Don't

### ✅ Download-Validate-Replace Pattern (CRITICAL FIX)

**Our implementation**: Complete validation before saving any data
- `ValidateCharacterData()` - Check ≥50 chars, critical chars exist
- `ValidateArtifactData()` - Check ≥30 sets
- `ValidateWeaponData()` - Check ≥100 weapons
- `ValidateMaterialData()` - Check ≥50 materials
- Work on `newData` copy, preserve original as fallback
- Only save if validation passes

**Their situation**: Still using optimistic delete, will lose all data on failed update

### ✅ Comprehensive Validation Report

**Our documentation**:
- `constellation_validation_report.md` - 85+ characters verified
- `DATABASE_UPDATE_GUIDE.md` - Complete update system documentation
- `reference_data/` - Verified baseline data for safety

### ✅ Constellation Order Fixes (VERIFIED)

**Our fixes**: 5 constellation errors corrected:
- Arlecchino: `["auto", "burst"]` (was `["burst", "skill"]`)
- Neuvillette: `["auto", "burst"]` (was `["burst", "skill"]`)
- Wriothesley: `["auto", "burst"]` (was `["burst", "skill"]`)
- Lyney: `["auto", "burst"]` (was `["burst", "skill"]`)
- Mavuika: `["burst", "skill"]` (was `["skill", "burst"]`)

**Their situation**: They have the logic to detect normal attack carries, but we don't know if their data is correct without verification.

## Recommended Actions

### High Priority (Must Have)

1. **Port TextMap_MediumEN.json support** - We're missing hash mappings
2. **Port non-playable character filter** - Prevents NPC data corruption
3. **Port mannequin handling** - Prevents scanner crashes
4. **Contribute our data loss fix to taiwenlee** - They need this urgently

### Medium Priority (Should Have)

5. **Adopt their C3/C5 independent checking** - More robust than our paired approach
6. **Review their region calculation changes** - May improve scraping accuracy
7. **Review their artifact scraping improvements** - Better reliability

### Low Priority (Nice to Have)

8. **Version alignment** - Bump our version to match or exceed 1.4.2
9. **Merge our constellation validation report** - Share our verified data

## Merge Strategy

### Option 1: Port Individual Features (RECOMMENDED)

**Pros**:
- Keep our critical bug fix
- Cherry-pick only the features we need
- Maintain control over codebase

**Cons**:
- More manual work
- May miss hidden improvements

### Option 2: Merge Their Fork and Apply Our Fixes

**Pros**:
- Get all their improvements at once
- Easier to stay synchronized

**Cons**:
- Need to reapply our data loss fix
- May introduce regressions
- Harder to track what changed

### Option 3: Contribute to taiwenlee Fork

**Pros**:
- Join the actively maintained version
- Share our fixes with the community
- Genshin Optimizer already points there

**Cons**:
- Lose independent development
- Dependent on taiwenlee for releases

## Immediate Next Steps

1. **Port TextMap_MediumEN.json support** (fixes missing hash mappings)
2. **Port non-playable character filter** (prevents Columbina/Ineffa corruption)
3. **Port mannequin handling** (prevents scanner crashes)
4. **Test database update** to verify TextMap fixes work
5. **Open PR to taiwenlee** with our data loss fix

## File Comparison Summary

| File | Our Version | taiwenlee Version | Status |
|------|-------------|-------------------|--------|
| DatabaseManager.cs | Has validation, no TextMap_Medium, has data loss fix | Has TextMap_Medium, NPC filter, HAS DATA LOSS BUG | NEED MERGE |
| CharacterScraper.cs | No mannequin handling | Has mannequin skip logic | NEED PORT |
| GenshinProcesor.cs | No mannequin support | Has mannequin validation | NEED PORT |
| InventoryKamera.cs | No mannequin names | Assigns mannequin names | NEED PORT |
| Settings (various) | No mannequin settings | Has Manequin1Name, Manequin2Name | NEED PORT |

## Version History

**Our fork (1.3.17)**:
- Based on Andrewthe13th/Inventory_Kamera
- Added constellation validation
- Fixed data loss bug
- Added comprehensive validation

**taiwenlee fork (1.4.2)**:
- ActivelyMaintained (recommended by Genshin Optimizer)
- TextMap_MediumEN.json support
- Mannequin handling
- NPC filtering
- Region calculation improvements
- **STILL HAS DATA LOSS BUG**
