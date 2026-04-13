#!/usr/bin/env python3
"""
Constellation Validation Script for Inventory Kamera
Verifies ConstellationOrder data in characters.json against genshin.gg
"""

import json
import requests
import re
import time
from typing import Dict, List, Tuple

# Mapping of talent names to talent types
TALENT_TYPE_KEYWORDS = {
    'auto': ['normal attack', 'basic attack', 'charged attack'],
    'skill': ['elemental skill'],
    'burst': ['elemental burst']
}

def normalize_character_name(name: str) -> str:
    """Convert internal name to genshin.gg URL format"""
    # Handle special cases
    special_cases = {
        'hutao': 'hu-tao',
        'yaemiko': 'yae-miko',
        'raidenshogun': 'raiden',
        'kujousara': 'kujou-sara',
        'kaedeharakazuha': 'kazuha',
        'sangonomiyakokomi': 'kokomi',
        'kamisatoayaka': 'ayaka',
        'kamisatoayato': 'ayato',
        'shikanoinheizou': 'heizou',
        'aratakiitto': 'itto',
        'kukishinobu': 'kuki-shinobu',
        'yunjin': 'yun-jin',
        'lanyan': 'lan-yan',
        'yumemizukimizuki': 'mizuki'
    }

    name_lower = name.lower()
    if name_lower in special_cases:
        return special_cases[name_lower]

    # Default: just lowercase
    return name_lower

def fetch_constellation_data(character_name: str) -> Tuple[str, str]:
    """
    Fetch C3 and C5 constellation data from genshin.gg
    Returns: (c3_talent_type, c5_talent_type) as 'auto', 'skill', or 'burst'
    """
    url = f"https://genshin.gg/characters/{normalize_character_name(character_name)}/"

    try:
        response = requests.get(url, timeout=10)
        if response.status_code != 200:
            return (None, None)

        content = response.text.lower()

        # Find C3 and C5 constellation descriptions
        c3_pattern = r'constellation 3[^<]*?increases the level of ([^<]+?) by 3'
        c5_pattern = r'constellation 5[^<]*?increases? the level of ([^<]+?) by 3'

        c3_match = re.search(c3_pattern, content, re.IGNORECASE | re.DOTALL)
        c5_match = re.search(c5_pattern, content, re.IGNORECASE | re.DOTALL)

        c3_type = None
        c5_type = None

        if c3_match:
            c3_talent = c3_match.group(1).lower()
            if 'normal attack' in c3_talent or 'basic attack' in c3_talent:
                c3_type = 'auto'
            elif 'elemental skill' in c3_talent or any(x in c3_talent for x in ['skill', 'press', 'hold']):
                c3_type = 'skill'
            elif 'elemental burst' in c3_talent or 'burst' in c3_talent:
                c3_type = 'burst'

        if c5_match:
            c5_talent = c5_match.group(1).lower()
            if 'normal attack' in c5_talent or 'basic attack' in c5_talent:
                c5_type = 'auto'
            elif 'elemental skill' in c5_talent or any(x in c5_talent for x in ['skill', 'press', 'hold']):
                c5_type = 'skill'
            elif 'elemental burst' in c5_talent or 'burst' in c5_talent:
                c5_type = 'burst'

        return (c3_type, c5_type)

    except Exception as e:
        print(f"  Error fetching {character_name}: {e}")
        return (None, None)

def main():
    # Load characters.json
    with open('InventoryKamera/bin/Debug/inventorylists/characters.json', 'r', encoding='utf-8') as f:
        characters = json.load(f)

    errors = []
    verified = []
    skipped = []

    print("Validating constellation data...\n")

    for char_key, char_data in characters.items():
        # Skip Traveler (special case with element-specific constellations)
        if char_key.lower() == 'traveler':
            print(f"[SKIP] {char_key}: Traveler has element-specific constellations")
            skipped.append(char_key)
            continue

        # Skip characters without ConstellationOrder
        if 'ConstellationOrder' not in char_data or not isinstance(char_data['ConstellationOrder'], list):
            print(f"[SKIP] {char_key}: No ConstellationOrder data")
            skipped.append(char_key)
            continue

        current_order = char_data['ConstellationOrder']
        good_name = char_data.get('GOOD', char_key)

        print(f"Checking {good_name}... ", end='', flush=True)

        # Fetch from web
        c3_actual, c5_actual = fetch_constellation_data(char_key)

        if c3_actual is None or c5_actual is None:
            print(f"[SKIP] Could not fetch data")
            skipped.append(char_key)
            time.sleep(0.5)  # Rate limiting
            continue

        actual_order = [c3_actual, c5_actual]

        if current_order == actual_order:
            print(f"[OK] {current_order}")
            verified.append(char_key)
        else:
            print(f"[ERROR] Current: {current_order}, Should be: {actual_order}")
            errors.append({
                'character': char_key,
                'good_name': good_name,
                'current': current_order,
                'correct': actual_order
            })

        time.sleep(0.5)  # Rate limiting to be respectful

    # Print summary
    print("\n" + "="*80)
    print("VALIDATION SUMMARY")
    print("="*80)
    print(f"Total characters: {len(characters)}")
    print(f"Verified correct: {len(verified)}")
    print(f"Errors found: {len(errors)}")
    print(f"Skipped: {len(skipped)}")

    if errors:
        print("\n" + "="*80)
        print("ERRORS REQUIRING FIXES")
        print("="*80)
        for error in errors:
            print(f"\n{error['character']} ({error['good_name']}):")
            print(f"  Current: {error['current']}")
            print(f"  Correct: {error['correct']}")
            print(f"  Change: {error['current']} → {error['correct']}")

    # Generate fix JSON
    if errors:
        print("\n" + "="*80)
        print("JSON PATCHES TO APPLY")
        print("="*80)
        for error in errors:
            print(f'  "{error["character"]}": {{"ConstellationOrder": {error["correct"]}}},')

if __name__ == '__main__':
    main()
