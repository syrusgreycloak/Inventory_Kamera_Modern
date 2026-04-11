# Database Update Guide

## Overview

The database update system downloads character, weapon, artifact, and material data from Dimbreath's GenshinData repository and converts it to GOOD format for use by Inventory Kamera.

## How Updates Work

### Update Pipeline (Download-Validate-Replace Pattern)

The update process follows a safe, atomic pattern to prevent data loss:

1. **Load existing data** - Current data is loaded as a baseline (or empty if force mode)
2. **Work on copy** - All processing happens on a `newData` copy, preserving original
3. **Download and process** - Fetch remote data and convert to GOOD format
4. **Validate before saving** - Comprehensive checks ensure data integrity
5. **Atomic replacement** - Only save if validation passes, otherwise keep existing data

### Validation Checks

Each data type has validation to prevent corrupt or incomplete data from being saved:

**Characters** (`ValidateCharacterData`):
- Minimum count: ≥50 characters (as of 6.5.0 there are 112+)
- Critical characters must exist: traveler, amber, kaeya, lisa
- Required fields: GOOD, Element, WeaponType
- ConstellationOrder required (except traveler who has element-specific)

**Weapons** (`ValidateWeaponData`):
- Minimum count: ≥100 weapons (as of 6.5.0 there are 244+)
- All entries must have valid GOOD names

**Artifacts** (`ValidateArtifactData`):
- Minimum count: ≥30 artifact sets (as of 6.5.0 there are 59+)
- Critical sets must exist: gladiatorsfinale, wandererstroupe, noblesseoblige
- Required fields: GOOD, artifacts (with all 5 pieces)

**Materials** (`ValidateMaterialData`):
- Minimum count: ≥50 materials (as of 6.5.0 there are 678+)
- All entries must have valid GOOD names

## Update Behavior

### Normal Update (Non-forced)

When clicking "Update Lookup Tables" without forcing:

1. Existing data is loaded and used as baseline
2. New items from remote data are added
3. Existing items remain unchanged (preserves manual fixes)
4. If download or validation fails → keeps existing data intact
5. You'll see log messages: `"Character data validation passed. Saving X characters."`

### Force Update

When clicking "Yes" to force update:

1. Starts with empty data structures (ignores existing data)
2. Rebuilds entire dataset from remote source
3. Validation still applies before saving
4. If download or validation fails → **keeps your existing data** (critical improvement)
5. Use this when you want to refresh all data or fix corrupted entries

**IMPORTANT**: Force mode no longer deletes your existing data before downloading. If the update fails for any reason (network error, validation failure, incomplete data), your existing files remain intact.

## Handling Unreleased Content

The updater gracefully handles unreleased game content (6.5.0+):

- **Character names, skills, constellations** - Skipped with warning if TextMapHash not found
- **Artifact pieces** - Skipped with warning if TextMapHash not found
- **Weapons** - Skipped with warning if TextMapHash not found
- **Materials** - Already had defensive checks

Log output example:
```
[WARN] Character hash 1234567890 not found in Mappings. It's likely unreleased.
[WARN] Artifact piece hash 9876543210 not found in Mappings. It's likely unreleased.
```

This allows updates to complete successfully even when remote data contains unreleased content, saving all released content while skipping unreleased items.

## Constellation Order Detection

The updater automatically detects constellation order (C3/C5 talent boosts) from constellation descriptions:

- **Normal Attack Carries** (C3 = auto, C5 = burst): Arlecchino, Neuvillette, Wriothesley, Lyney
  - Detected by "Normal Attack" or "Charged Attack" keywords in C3 description
- **Skill Carries** (C3 = skill, C5 = burst): Most characters
  - Detected by skill name in C3 description
- **Burst Carries** (C3 = burst, C5 = skill): Rare cases
  - Default when neither auto nor skill detected

This ensures accurate constellation data without manual entry.

## Troubleshooting

### Update Fails with Validation Error

Check the log file (`logging/InventoryKamera.debug.log`) for specific validation failures:

```
[ERROR] Validation failed: Only 45 characters found (expected at least 50)
[ERROR] Validation failed: Missing critical character 'amber'
[ERROR] Character data validation failed. Keeping existing data.
```

**Solution**: Your existing data remains intact. Check network connectivity and try again.

### Force Update Doesn't Change Existing Data

This is expected if validation fails. Check logs for specific errors. If you need to completely rebuild:

1. Ensure network connectivity to GitHub
2. Check that GenshinData repository is accessible
3. Verify TextMap.json is up to date (needed for mapping hashes to names)

### Missing Characters/Weapons After Update

**Characters/weapons marked as unreleased will be skipped.** Check logs for warnings about missing TextMapHash entries. These items will be added in future updates once they're released and TextMap data is available.

## Best Practices

1. **Regular updates**: Run updates after each major Genshin Impact version (6.0, 6.1, 6.2, etc.)
2. **Check logs**: Review `logging/InventoryKamera.debug.log` after updates for any warnings
3. **Verify counts**: After update, check that data counts are reasonable:
   - Characters: 100+
   - Weapons: 200+
   - Artifacts: 50+
   - Materials: 600+
4. **Backup**: The `reference_data/` directory contains verified baseline data
5. **Force sparingly**: Only force update when you need to fix corrupted data or want a complete refresh

## Data Files

Updated files are saved to:
- `inventorylists/characters.json` - Character data with constellation orders
- `inventorylists/weapons.json` - Weapon name mappings
- `inventorylists/artifacts.json` - Artifact sets with all 5 pieces
- `inventorylists/materials.json` - Material name mappings

All files use GOOD (Genshin Open Object Description) format for compatibility with optimizer tools.
