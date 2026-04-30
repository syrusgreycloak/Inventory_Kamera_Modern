using System;
using System.Collections.Generic;

namespace InventoryKamera.Configuration
{
    /// <summary>
    /// Strongly-typed settings POCO for InventoryKamera.
    /// Mirrors every key in the WinForms Properties.Settings (Settings.Designer.cs).
    /// Default values match the WinForms defaults so migrating users see no behavioral change.
    /// </summary>
    public class ScanSettings
    {
        // ------------------------------------------------------------------
        // Scan toggles
        // ------------------------------------------------------------------

        /// <summary>Whether to scan weapons. Default: true.</summary>
        public bool ScanWeapons { get; set; } = true;

        /// <summary>Whether to scan artifacts. Default: true.</summary>
        public bool ScanArtifacts { get; set; } = true;

        /// <summary>Whether to scan characters. Default: true.</summary>
        public bool ScanCharacters { get; set; } = true;

        /// <summary>Whether to scan materials. Default: true.</summary>
        public bool ScanMaterials { get; set; } = true;

        /// <summary>Whether to scan character development items. Default: true.</summary>
        public bool ScanCharDevItems { get; set; } = true;

        // ------------------------------------------------------------------
        // Minimum rarity / level filters
        // ------------------------------------------------------------------

        /// <summary>Minimum weapon star rarity to include. WinForms default: 3.</summary>
        public int MinimumWeaponRarity { get; set; } = 3;

        /// <summary>Minimum weapon level to include. WinForms default: 1.</summary>
        public int MinimumWeaponLevel { get; set; } = 1;

        /// <summary>Minimum artifact star rarity to include. WinForms default: 4.</summary>
        public int MinimumArtifactRarity { get; set; } = 4;

        /// <summary>Minimum artifact level to include. WinForms default: 0.</summary>
        public int MinimumArtifactLevel { get; set; } = 0;

        // ------------------------------------------------------------------
        // Equipment inclusion
        // ------------------------------------------------------------------

        /// <summary>Include equipped weapons in scan output. Default: true.</summary>
        public bool EquipWeapons { get; set; } = true;

        /// <summary>Include equipped artifacts in scan output. Default: true.</summary>
        public bool EquipArtifacts { get; set; } = true;

        // ------------------------------------------------------------------
        // Timing
        // ------------------------------------------------------------------

        /// <summary>Extra delay (milliseconds) between scanner steps. Default: 0.</summary>
        public int ScannerDelay { get; set; } = 0;

        // ------------------------------------------------------------------
        // Output
        // ------------------------------------------------------------------

        /// <summary>Directory path where exported GOOD JSON is saved. Default: empty string (prompts).</summary>
        public string OutputPath { get; set; } = string.Empty;

        // ------------------------------------------------------------------
        // Hotkeys (VK codes; will switch to platform-neutral Key in Chunk 7)
        // ------------------------------------------------------------------

        /// <summary>Virtual-key code for opening the inventory menu. Default: 66 ('B').</summary>
        public int InventoryKey { get; set; } = 66;

        /// <summary>Virtual-key code for opening the character screen. Default: 67 ('C').</summary>
        public int CharacterKey { get; set; } = 67;

        /// <summary>Virtual-key code for equipment slot 1 shortcut. Default: 1.</summary>
        public int Slot1Key { get; set; } = 1;

        // ------------------------------------------------------------------
        // Update / versioning state
        // ------------------------------------------------------------------

        /// <summary>Date of last lookup-table update check. Default: DateTime.MinValue.</summary>
        public DateTime LastUpdateCheck { get; set; } = DateTime.MinValue;

        /// <summary>Whether an application upgrade is available. Default: true (prompt on first run).</summary>
        public bool UpgradeNeeded { get; set; } = true;

        /// <summary>Last fetched remote version string. Default: "0.0.0.0".</summary>
        public string RemoteVersion { get; set; } = "0.0.0.0";

        // ------------------------------------------------------------------
        // Diagnostics
        // ------------------------------------------------------------------

        /// <summary>Whether to log all scan screenshots to ./logging/. Default: false.</summary>
        public bool LogScreenshots { get; set; } = false;

        // ------------------------------------------------------------------
        // Named entity overrides (user-customisable in-game names)
        // ------------------------------------------------------------------

        /// <summary>In-game name of the Traveler character (user-specific). Default: empty.</summary>
        public string TravelerName { get; set; } = string.Empty;

        /// <summary>In-game name of the Wanderer character. Default: "wanderer".</summary>
        public string WandererName { get; set; } = "wanderer";

        /// <summary>Display name for mannequin preset slot 1. Default: "Manequin1".</summary>
        public string Manequin1Name { get; set; } = "Manequin1";

        /// <summary>Display name for mannequin preset slot 2. Default: "Manequin2".</summary>
        public string Manequin2Name { get; set; } = "Manequin2";

        // ------------------------------------------------------------------
        // Process detection
        // ------------------------------------------------------------------

        /// <summary>
        /// List of Genshin Impact executable base names (without .exe) to look for.
        /// Default: GenshinImpact, YuanShen.
        /// </summary>
        public List<string> Executables { get; set; } = new List<string>
        {
            "GenshinImpact",
            "YuanShen"
        };
    }
}
