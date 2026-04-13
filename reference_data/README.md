# Reference Data Backup

This directory contains known-good reference data files for Inventory Kamera.

## Purpose

These files serve as:
1. **Baseline for validation** - Diff against downloaded data to detect issues
2. **Backup of verified data** - Fall back to these if download/update fails
3. **Testing reference** - Use for unit tests without relying on external sources

## Files

### characters.json
**Version:** 6.4.0 (Apr 2026)
**Status:** ✅ Manually verified against genshin.gg
**Validation:** 85+ characters checked, 5 constellation errors fixed
**Last Updated:** 2026-04-11

**Critical Fix:** ConstellationOrder for normal attack carries (Arlecchino, Neuvillette, Wriothesley, Lyney) corrected from `["burst", "skill"]` to `["auto", "burst"]`.

Key verified data:
- ConstellationOrder for all playable characters through 6.4
- ConstellationName (constellation system names)
- Element assignments
- WeaponType classifications

### weapons.json
**Version:** 6.4.0
**Last Updated:** 2026-04-09

Weapon name mappings for OCR → GOOD format conversion.

### artifacts.json
**Version:** 6.4.0
**Last Updated:** 2026-04-10

Artifact set and piece name mappings, including:
- Set names (GOOD format)
- Piece slot mappings
- Sanctified artifact flags

### materials.json
**Version:** 6.4.0
**Last Updated:** 2026-04-09

Character/weapon ascension material mappings.

## Usage

### Validation After Database Update

```bash
# After running "Update Lookup Tables" in the app, diff the files:
diff reference_data/characters.json InventoryKamera/bin/Debug/inventorylists/characters.json

# Check for unexpected ConstellationOrder changes:
python validate_constellations.py
```

### Restoring Known-Good Data

If database update fails or produces corrupt data:

```bash
cp reference_data/*.json InventoryKamera/bin/Debug/inventorylists/
```

## Update Policy

Update these reference files when:
1. Major game version releases (e.g., 6.5, 7.0)
2. Validation detects and fixes errors in downloaded data
3. Manual verification confirms data accuracy

Always include validation evidence (validation report, test results) when updating.

## Validation History

- **2026-04-11:** Initial validation of characters.json
  - Verified 85+ characters against genshin.gg constellation data
  - Fixed 5 ConstellationOrder errors (5.4% error rate)
  - See: `constellation_validation_report.md`
