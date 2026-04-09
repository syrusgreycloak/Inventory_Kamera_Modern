#!/usr/bin/env python3
"""
Merge new weapons from dvaJi/genshin-data into InventoryKamera's weapons.json
Preserves existing validated entries, only adds missing weapons.
"""

import json
import urllib.request
import re
from typing import Dict, List

# Weapon type mapping: dvaJi string -> InventoryKamera integer
WEAPON_TYPE_MAP = {
    "Sword": 0,
    "Claymore": 1,
    "Polearm": 2,
    "Bow": 3,
    "Catalyst": 4
}

# Weapons to skip (unreleased, test weapons)
SKIP_WEAPONS = {
    "test", "prototype"
}

def fetch_json(url: str) -> dict:
    """Fetch JSON from URL"""
    with urllib.request.urlopen(url) as response:
        return json.loads(response.read().decode())

def normalize_key(name: str) -> str:
    """Convert name to InventoryKamera internal key format (lowercase, no spaces/underscores)"""
    # Remove spaces, hyphens, apostrophes, underscores
    normalized = name.replace(" ", "").replace("-", "").replace("'", "").replace("_", "").lower()
    return normalized

def to_good_name(name: str) -> str:
    """Convert name to GOOD format (remove spaces, keep capitalization)"""
    # Remove spaces and hyphens
    return name.replace(" ", "").replace("-", "")

def convert_dvaJi_to_IK(weapon_data: dict) -> dict:
    """Convert dvaJi weapon format to InventoryKamera format"""
    name = weapon_data.get("name", "")

    # Extract weapon type (it's an object with "id" and "name" fields)
    weapon_type_obj = weapon_data.get("weapon_type", {})
    weapon_type_name = weapon_type_obj.get("name", "") if isinstance(weapon_type_obj, dict) else ""

    # Extract rarity
    rarity = weapon_data.get("rarity", 1)

    # Create InventoryKamera format
    ik_weapon = {
        "GOOD": to_good_name(name),
        "WeaponType": WEAPON_TYPE_MAP.get(weapon_type_name, 0),
        "Rarity": rarity
    }

    return ik_weapon

def fetch_weapon_list() -> List[str]:
    """Fetch list of all weapon files from dvaJi repository"""
    url = "https://api.github.com/repos/dvaJi/genshin-data/contents/src/data/english/weapons"
    contents = fetch_json(url)

    # Extract weapon names from filenames
    weapons = []
    for item in contents:
        if item["name"].endswith(".json"):
            weapon_name = item["name"].replace(".json", "")
            # Skip test/unreleased weapons
            if not any(skip in weapon_name.lower() for skip in SKIP_WEAPONS):
                weapons.append(weapon_name)

    return weapons

def fetch_weapon_data(weapon_name: str) -> dict:
    """Fetch individual weapon data from dvaJi"""
    url = f"https://raw.githubusercontent.com/dvaJi/genshin-data/master/src/data/english/weapons/{weapon_name}.json"
    return fetch_json(url)

def main():
    # Load existing weapons.json
    existing_file = r"C:\Users\karlp\RiderProjects\Inventory_Kamera\InventoryKamera\bin\Debug\inventorylists\weapons.json"

    with open(existing_file, 'r', encoding='utf-8') as f:
        existing_weapons = json.load(f)

    print(f"Loaded {len(existing_weapons)} existing weapons")

    # Fetch list of all weapons from dvaJi
    print("Fetching weapon list from dvaJi/genshin-data...")
    all_dvaJi_weapons = fetch_weapon_list()
    print(f"Found {len(all_dvaJi_weapons)} weapons in dvaJi repository")

    # Find missing weapons
    missing_weapons = []
    for dvaJi_weapon in all_dvaJi_weapons:
        internal_key = normalize_key(dvaJi_weapon)
        if internal_key not in existing_weapons:
            missing_weapons.append((dvaJi_weapon, internal_key))

    print(f"\nMissing weapons: {len(missing_weapons)}")
    for dvaJi_name, internal_key in missing_weapons:
        print(f"  - {dvaJi_name} -> {internal_key}")

    if not missing_weapons:
        print("\n[OK] No missing weapons to add!")
        return

    # Fetch and convert missing weapons
    print(f"\nFetching data for {len(missing_weapons)} missing weapons...")
    added_count = 0
    failed_weapons = []

    for dvaJi_name, internal_key in missing_weapons:
        try:
            print(f"  Fetching {dvaJi_name}...", end=" ")
            weapon_data = fetch_weapon_data(dvaJi_name)
            ik_weapon = convert_dvaJi_to_IK(weapon_data)

            # Add to existing_weapons
            existing_weapons[internal_key] = ik_weapon
            added_count += 1
            print(f"[OK] Added as '{internal_key}'")
        except Exception as e:
            print(f"[FAIL] {e}")
            failed_weapons.append((dvaJi_name, str(e)))

    # Sort alphabetically by key (for readability)
    sorted_weapons = dict(sorted(existing_weapons.items()))

    # Save updated weapons.json
    output_file = existing_file + ".new"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(sorted_weapons, f, indent=2, ensure_ascii=False)

    print(f"\n[OK] Successfully added {added_count} new weapons")
    print(f"[FILE] Saved to: {output_file}")
    print(f"\n[WARN] IMPORTANT: Review the output file before replacing weapons.json!")

    if failed_weapons:
        print(f"\n[FAIL] Failed to add {len(failed_weapons)} weapons:")
        for name, error in failed_weapons:
            print(f"  - {name}: {error}")

if __name__ == "__main__":
    main()
