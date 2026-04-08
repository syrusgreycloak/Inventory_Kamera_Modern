#!/usr/bin/env python3
"""
Merge new characters from dvaJi/genshin-data into InventoryKamera's characters.json
Preserves existing validated entries, only adds missing characters.
"""

import json
import urllib.request
import re
from typing import Dict, List

# Weapon type mapping: dvaJi string → InventoryKamera integer
WEAPON_TYPE_MAP = {
    "Sword": 0,
    "Claymore": 1,
    "Polearm": 2,
    "Bow": 3,
    "Catalyst": 4
}

# Characters to skip (NPCs, test characters, unreleased, traveler variants)
SKIP_CHARACTERS = {
    "manekin", "manekina", "kate", "test",
    # Skip individual traveler elements (we have a unified traveler entry)
    "traveler_anemo", "traveler_geo", "traveler_electro", "traveler_dendro", "traveler_hydro", "traveler_pyro"
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

def to_camel_case(name: str) -> str:
    """Convert name to GOOD CamelCase format"""
    # Remove spaces and hyphens, capitalize each word
    words = re.split(r'[\s\-]', name)
    return ''.join(word.capitalize() for word in words if word)

def convert_dvaJi_to_IK(char_data: dict) -> dict:
    """Convert dvaJi character format to InventoryKamera format"""
    name = char_data.get("name", "")

    # Extract element (it's an object with "id" and "name" fields)
    element_obj = char_data.get("element", {})
    element_id = element_obj.get("id", "") if isinstance(element_obj, dict) else ""

    # Extract weapon_type (also an object with "id" and "name" fields)
    weapon_obj = char_data.get("weapon_type", {})
    weapon_name = weapon_obj.get("name", "") if isinstance(weapon_obj, dict) else ""

    # Create InventoryKamera format
    ik_char = {
        "GOOD": to_camel_case(name),
        "ConstellationName": [char_data.get("constellations", [{}])[0].get("name", "")],
        "ConstellationOrder": ["skill", "burst"],  # Default, may need manual verification
        "Element": [element_id],
        "WeaponType": WEAPON_TYPE_MAP.get(weapon_name, 0)
    }

    return ik_char

def fetch_character_list() -> List[str]:
    """Fetch list of all character files from dvaJi repository"""
    url = "https://api.github.com/repos/dvaJi/genshin-data/contents/src/data/english/characters"
    contents = fetch_json(url)

    # Extract character names from filenames
    characters = []
    for item in contents:
        if item["name"].endswith(".json"):
            char_name = item["name"].replace(".json", "")
            if char_name not in SKIP_CHARACTERS:
                characters.append(char_name)

    return characters

def fetch_character_data(char_name: str) -> dict:
    """Fetch individual character data from dvaJi"""
    url = f"https://raw.githubusercontent.com/dvaJi/genshin-data/master/src/data/english/characters/{char_name}.json"
    return fetch_json(url)

def main():
    # Load existing characters.json
    existing_file = r"C:\Users\karlp\RiderProjects\Inventory_Kamera\InventoryKamera\bin\Debug\inventorylists\characters.json"

    with open(existing_file, 'r', encoding='utf-8') as f:
        existing_chars = json.load(f)

    print(f"Loaded {len(existing_chars)} existing characters")

    # Fetch list of all characters from dvaJi
    print("Fetching character list from dvaJi/genshin-data...")
    all_dvaJi_chars = fetch_character_list()
    print(f"Found {len(all_dvaJi_chars)} characters in dvaJi repository")

    # Find missing characters
    missing_chars = []
    for dvaJi_char in all_dvaJi_chars:
        internal_key = normalize_key(dvaJi_char)
        if internal_key not in existing_chars:
            missing_chars.append((dvaJi_char, internal_key))

    print(f"\nMissing characters: {len(missing_chars)}")
    for dvaJi_name, internal_key in missing_chars:
        print(f"  - {dvaJi_name} -> {internal_key}")

    if not missing_chars:
        print("\n[OK] No missing characters to add!")
        return

    # Fetch and convert missing characters
    print(f"\nFetching data for {len(missing_chars)} missing characters...")
    added_count = 0
    failed_chars = []

    for dvaJi_name, internal_key in missing_chars:
        try:
            print(f"  Fetching {dvaJi_name}...", end=" ")
            char_data = fetch_character_data(dvaJi_name)
            ik_char = convert_dvaJi_to_IK(char_data)

            # Add to existing_chars
            existing_chars[internal_key] = ik_char
            added_count += 1
            print(f"[OK] Added as '{internal_key}'")
        except Exception as e:
            print(f"[FAIL] {e}")
            failed_chars.append((dvaJi_name, str(e)))

    # Special case: Add pyro to traveler's elements if not present
    if "traveler" in existing_chars:
        traveler = existing_chars["traveler"]
        if "pyro" not in traveler["Element"]:
            print("\n[FIRE] Adding pyro element to traveler...")
            traveler["Element"].insert(1, "pyro")  # Insert after electro

        # Add pyro constellation order if not present (default to match other elements)
        if isinstance(traveler.get("ConstellationOrder"), dict):
            if "pyro" not in traveler["ConstellationOrder"]:
                traveler["ConstellationOrder"]["pyro"] = ["skill", "burst"]
                print("   Added pyro constellation order")

    # Sort alphabetically by key (for readability)
    sorted_chars = dict(sorted(existing_chars.items()))

    # Save updated characters.json
    output_file = existing_file + ".new"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(sorted_chars, f, indent=2, ensure_ascii=False)

    print(f"\n[OK] Successfully added {added_count} new characters")
    print(f"[FILE] Saved to: {output_file}")
    print(f"\n[WARN] IMPORTANT: Review the output file before replacing characters.json!")
    print(f"   Especially check ConstellationOrder (defaulted to ['skill', 'burst'])")

    if failed_chars:
        print(f"\n[FAIL] Failed to add {len(failed_chars)} characters:")
        for name, error in failed_chars:
            print(f"  - {name}: {error}")

if __name__ == "__main__":
    main()
