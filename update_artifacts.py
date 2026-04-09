#!/usr/bin/env python3
"""
Merge new artifact sets from dvaJi/genshin-data into InventoryKamera's artifacts.json
Preserves existing validated entries, only adds missing artifact sets.
"""

import json
import urllib.request
import re
from typing import Dict, List

# Artifacts to skip (test sets, unreleased)
SKIP_ARTIFACTS = {
    "test"
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
    """Convert name to GOOD format (remove spaces, hyphens, apostrophes)"""
    # Remove spaces, hyphens, and apostrophes
    return name.replace(" ", "").replace("-", "").replace("'", "")

def convert_dvaJi_to_IK(artifact_data: dict) -> dict:
    """Convert dvaJi artifact format to InventoryKamera format"""
    name = artifact_data.get("name", "")

    # Extract max rarity
    max_rarity = artifact_data.get("max_rarity", 5)

    # Create InventoryKamera format
    ik_artifact = {
        "GOOD": to_good_name(name),
        "Rarity": max_rarity
    }

    return ik_artifact

def fetch_artifact_list() -> List[str]:
    """Fetch list of all artifact files from dvaJi repository"""
    url = "https://api.github.com/repos/dvaJi/genshin-data/contents/src/data/english/artifacts"
    contents = fetch_json(url)

    # Extract artifact names from filenames
    artifacts = []
    for item in contents:
        if item["name"].endswith(".json"):
            artifact_name = item["name"].replace(".json", "")
            # Skip test/unreleased artifacts
            if not any(skip in artifact_name.lower() for skip in SKIP_ARTIFACTS):
                artifacts.append(artifact_name)

    return artifacts

def fetch_artifact_data(artifact_name: str) -> dict:
    """Fetch individual artifact data from dvaJi"""
    url = f"https://raw.githubusercontent.com/dvaJi/genshin-data/master/src/data/english/artifacts/{artifact_name}.json"
    return fetch_json(url)

def main():
    # Load existing artifacts.json
    existing_file = r"C:\Users\karlp\RiderProjects\Inventory_Kamera\InventoryKamera\bin\Debug\inventorylists\artifacts.json"

    with open(existing_file, 'r', encoding='utf-8') as f:
        existing_artifacts = json.load(f)

    print(f"Loaded {len(existing_artifacts)} existing artifact sets")

    # Fetch list of all artifacts from dvaJi
    print("Fetching artifact list from dvaJi/genshin-data...")
    all_dvaJi_artifacts = fetch_artifact_list()
    print(f"Found {len(all_dvaJi_artifacts)} artifact sets in dvaJi repository")

    # Find missing artifacts
    missing_artifacts = []
    for dvaJi_artifact in all_dvaJi_artifacts:
        internal_key = normalize_key(dvaJi_artifact)
        if internal_key not in existing_artifacts:
            missing_artifacts.append((dvaJi_artifact, internal_key))

    print(f"\nMissing artifact sets: {len(missing_artifacts)}")
    for dvaJi_name, internal_key in missing_artifacts:
        print(f"  - {dvaJi_name} -> {internal_key}")

    if not missing_artifacts:
        print("\n[OK] No missing artifact sets to add!")
        return

    # Fetch and convert missing artifacts
    print(f"\nFetching data for {len(missing_artifacts)} missing artifact sets...")
    added_count = 0
    failed_artifacts = []

    for dvaJi_name, internal_key in missing_artifacts:
        try:
            print(f"  Fetching {dvaJi_name}...", end=" ")
            artifact_data = fetch_artifact_data(dvaJi_name)
            ik_artifact = convert_dvaJi_to_IK(artifact_data)

            # Add to existing_artifacts
            existing_artifacts[internal_key] = ik_artifact
            added_count += 1
            print(f"[OK] Added as '{internal_key}'")
        except Exception as e:
            print(f"[FAIL] {e}")
            failed_artifacts.append((dvaJi_name, str(e)))

    # Sort alphabetically by key (for readability)
    sorted_artifacts = dict(sorted(existing_artifacts.items()))

    # Save updated artifacts.json
    output_file = existing_file + ".new"
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(sorted_artifacts, f, indent=2, ensure_ascii=False)

    print(f"\n[OK] Successfully added {added_count} new artifact sets")
    print(f"[FILE] Saved to: {output_file}")
    print(f"\n[WARN] IMPORTANT: Review the output file before replacing artifacts.json!")

    if failed_artifacts:
        print(f"\n[FAIL] Failed to add {len(failed_artifacts)} artifact sets:")
        for name, error in failed_artifacts:
            print(f"  - {name}: {error}")

if __name__ == "__main__":
    main()
