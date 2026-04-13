using System.Collections.Generic;
using Newtonsoft.Json;

namespace InventoryKamera.Configuration
{
    public class ScanRegion
    {
        [JsonProperty("x")] public double X { get; set; }
        [JsonProperty("y")] public double Y { get; set; }
        [JsonProperty("w")] public double W { get; set; }
        [JsonProperty("h")] public double H { get; set; }
    }

    public class ScanPoint
    {
        [JsonProperty("x")] public double X { get; set; }
        [JsonProperty("y")] public double Y { get; set; }
    }

    public class NavigationCoordinates
    {
        [JsonProperty("weaponInventoryTab")] public ScanPoint WeaponInventoryTab { get; set; }
        [JsonProperty("artifactInventoryTab")] public ScanPoint ArtifactInventoryTab { get; set; }
        [JsonProperty("materialTab")] public ScanPoint MaterialTab { get; set; }
        [JsonProperty("charDevTab")] public ScanPoint CharDevTab { get; set; }
        [JsonProperty("characterTab")] public ScanPoint CharacterTab { get; set; }
        [JsonProperty("firstItem")] public ScanPoint FirstItem { get; set; }
        [JsonProperty("uidMask")] public ScanRegion UidMask { get; set; }
    }

    public class ArtifactScanCoordinates
    {
        [JsonProperty("card")] public ScanRegion Card { get; set; }
        [JsonProperty("name")] public ScanRegion Name { get; set; }
        [JsonProperty("mainStat")] public ScanRegion MainStat { get; set; }
        [JsonProperty("mainStatValue")] public ScanRegion MainStatValue { get; set; }
        [JsonProperty("level")] public ScanRegion Level { get; set; }
        [JsonProperty("substats")] public ScanRegion Substats { get; set; }
        [JsonProperty("equippedCharacter")] public ScanRegion EquippedCharacter { get; set; }
        [JsonProperty("lock")] public ScanRegion Lock { get; set; }
        [JsonProperty("rarity")] public ScanRegion Rarity { get; set; }
    }

    public class WeaponScanCoordinates
    {
        [JsonProperty("card")] public ScanRegion Card { get; set; }
        [JsonProperty("name")] public ScanRegion Name { get; set; }
        [JsonProperty("level")] public ScanRegion Level { get; set; }
        [JsonProperty("refinement")] public ScanRegion Refinement { get; set; }
        [JsonProperty("equippedCharacter")] public ScanRegion EquippedCharacter { get; set; }
        [JsonProperty("lock")] public ScanRegion Lock { get; set; }
        [JsonProperty("rarity")] public ScanRegion Rarity { get; set; }
    }

    public class CharacterScanCoordinates
    {
        [JsonProperty("name")] public ScanRegion Name { get; set; }
        [JsonProperty("level")] public ScanRegion Level { get; set; }
        [JsonProperty("constellation")] public ScanRegion Constellation { get; set; }
        [JsonProperty("talentCard")] public ScanRegion TalentCard { get; set; }
        [JsonProperty("talent1")] public ScanRegion Talent1 { get; set; }
        [JsonProperty("talent2")] public ScanRegion Talent2 { get; set; }
        [JsonProperty("talent3")] public ScanRegion Talent3 { get; set; }
        [JsonProperty("equippedWeapon")] public ScanRegion EquippedWeapon { get; set; }
    }

    public class AspectRatioProfile
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("aspectRatio")] public double AspectRatio { get; set; }
        [JsonProperty("navigation")] public NavigationCoordinates Navigation { get; set; }
        [JsonProperty("artifacts")] public ArtifactScanCoordinates Artifacts { get; set; }
        [JsonProperty("weapons")] public WeaponScanCoordinates Weapons { get; set; }
        [JsonProperty("characters")] public CharacterScanCoordinates Characters { get; set; }
    }

    public class ScanProfileFile
    {
        [JsonProperty("version")] public string Version { get; set; }
        [JsonProperty("profiles")] public Dictionary<string, AspectRatioProfile> Profiles { get; set; }
    }
}
