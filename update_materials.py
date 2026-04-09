#!/usr/bin/env python3
"""
Merge new materials from dvaJi/genshin-data into InventoryKamera's materials.json
Materials are spread across multiple directories in dvaJi repo.
Preserves existing validated entries, only adds missing materials.
"""

import json
import urllib.request
from typing import Dict, List, Tuple

# Material directories in dvaJi repo
MATERIAL_DIRS = [
    "talent_lvl_up_materials",
    "weapon_enhancement_material",
    "weapon_primary_materials",
    "weapon_secondary_materials",
    "common_materials",
    "elemental_stone_materials",
    "jewels_materials",
    "local_materials",
    "character_exp_material"
]

# Materials to skip
SKIP_MATERIALS = {
    "test"
}

def fetch_json(url: str) -> dict:
    """Fetch JSON from URL"""
    try:
        with urllib.request.urlopen(url) as response:
            return json.loads(response.read().decode())
    except Exception as e:
        print(f"[WARN] Failed to fetch {url}: {e}")
        return None

def normalize_key(name: str) -> str:
    """Convert name to InventoryKamera internal key format (lowercase, no spaces/underscores)"""
    # Remove spaces, hyphens, apostrophes, underscores
    normalized = name.replace(" ", "").replace("-", "").replace("'", "").replace("_", "").lower()
    return normalized

def to_good_name(name: str) -> str:
    """Convert name to GOOD format (remove spaces, keep capitalization)"""
    # Remove spaces and hyphens
    return name.replace(" ", "").replace("-", "")

def convert_dvaJi_to_IK(material_data: dict) -> dict:
    """Convert dvaJi material format to InventoryKamera format"""
    name = material_data.get("name", "")

    # Extract rarity
    rarity = material_data.get("rarity", 1)

    # Create InventoryKamera format (simple: just GOOD name)
    ik_material = {
        "GOOD": to_good_name(name)
    }

    return ik_material

def fetch_materials_from_directory(dir_name: str) -> List[Tuple[str, str]]:
    """Fetch all materials from a specific directory"""
    url = f"https://api.github.com/repos/dvaJi/genshin-data/contents/src/data/english/{dir_name}"
    contents = fetch_json(url)

    if contents is None:
        return []

    materials = []
    for item in contents:
        if item["name"].endswith(".json"):
            material_name = item["name"].replace(".json", "")
            # Skip test/unreleased materials
            if not any(skip in material_name.lower() for skip in SKIP_MATERIALS):
                materials.append((material_name, dir_name))

    return materials

def fetch_material_data(material_name: str, dir_name: str) -> dict:
    """Fetch individual material data from dvaJi"""
    url = f"https://raw.githubusercontent.com/dvaJi/genshin-data/master/src/data/english/{dir_name}/{material_name}.json"
    return fetch_json(url)

def main():
    # Load existing materials.json
    existing_file = r"C:\Users\karlp\RiderProjects\Inventory_Kamera\InventoryKamera\bin\Debug\inventorylists\materials.json"

    with open(existing_file, 'r', encoding='utf-8') as f:
        existing_materials = json.load(f)

    print(f"Loaded {len(existing_materials)} existing materials")

    # Fetch list of all materials from dvaJi (across all directories)
    print("Fetching materials from dvaJi/genshin-data...")
    all_dvaJi_materials = []

    for dir_name in MATERIAL_DIRS:
        print(f"  Checking {dir_name}...", end=" ")
        materials = fetch_materials_from_directory(dir_name)
        all_dvaJi_materials.extend(materials)
        print(f"found {len(materials)}")

    print(f"Found {len(all_dvaJi_materials)} total materials in dvaJi repository")

    # Find missing materials
    missing_materials = []
    for dvaJi_material, dir_name in all_dvaJi_materials:
        internal_key = normalize_key(dvaJi_material)
        if internal_key not in existing_materials:
            missing_materials.append((dvaJi_material, internal_key, dir_name))

    print(f"\nMissing materials: {len(missing_materials)}")
    if len(missing_materials) <= 50:  # Only print if reasonable number
        for dvaJi_name, internal_key, dir_name in missing_materials:
            print(f"  - {dvaJi_name} -> {internal_key} (from {dir_name})")
    else:
        print(f"  (Too many to list - {len(missing_materials)} total)")

    if not missing_materials:
        print("\n[OK] No missing materials to add!")
        return

    # Fetch and convert missing materials
    print(f"\nFetching data for {len(missing_materials)} missing materials...")
    added_count = 0
    failed_materials = []

    for dvaJi_name, internal_key, dir_name in missing_materials:
        try:
            material_data = fetch_material_data(dvaJi_name, dir_name)
            if material_data is None:
                failed_materials.append((dvaJi_name, "Failed to fetch"))
                continue

            ik_material = convert_dvaJi_to_IK(material_data)

            # Add to existing_materials
            existing_materials[internal_key] = ik_material
            added_count += 1

            # Only print progress every 10 items if many materials
            if added_count % 10 == 0 or len(missing_materials) <= 20:
                print(f"  [{added_count}/{len(missing_materials)}] Added {internal_key}")

        except Exception as e:
            failed_materials.append((dvaJi_name, str(e)))

    # Sort alphabetically by key (for readability)
    sorted_materials = dict(sorted(existing_materials.items()))

    # Save updated materials.json
    output_file = existing_file + ".new"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(sorted_materials, f, indent=2, ensure_ascii=False)

    print(f"\n[OK] Successfully added {added_count} new materials")
    print(f"[FILE] Saved to: {output_file}")
    print(f"\n[WARN] IMPORTANT: Review the output file before replacing materials.json!")

    if failed_materials:
        print(f"\n[FAIL] Failed to add {len(failed_materials)} materials:")
        for name, error in failed_materials[:20]:  # Show first 20 failures
            print(f"  - {name}: {error}")
        if len(failed_materials) > 20:
            print(f"  ... and {len(failed_materials) - 20} more")

if __name__ == "__main__":
    main()
