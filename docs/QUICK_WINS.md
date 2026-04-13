# Quick Wins

These improvements are independent of the 3-milestone sequence and can be done at any time.

---

## 1. GOOD v3: elixirCrafted (1-2 hours)

Detection already exists in `IsSanctified()`. Just needs wiring to the export.

**Files:**
- `InventoryKamera/game/Artifact.cs` — add property
- `InventoryKamera/scraping/ArtifactScraper.cs` — set property after detection
- `InventoryKamera/data/GOOD.cs` — bump version to 3

**Changes:**

In `Artifact.cs`, add the property:
```csharp
[JsonProperty("elixirCrafted")]
public bool ElixirCrafted { get; internal set; }
```

In `ArtifactScraper.CatalogueFromBitmapsAsync`, after the existing sanctified check:
```csharp
bool isSanctified = IsSanctified(card);
// ... existing coordinate adjustment logic ...
artifact.ElixirCrafted = isSanctified;  // ADD THIS LINE
```

In `GOOD.cs` constructor:
```csharp
Version = 3;  // was 2
```

**Test:** Scan a sanctified (elixir-crafted) artifact. Verify `"elixirCrafted": true` appears in GOOD JSON output for that artifact.

---

## 2. ScanProfile.json (1-2 days)

Externalize all hard-coded scan region coordinates so users can adjust them without recompiling.

See [Milestone 1, Step 1.4](MILESTONE_1_NET8_CORE.md) for full implementation details including the JSON structure and `ScanProfileManager` class design.

**When to do this:** Can be started now against the .NET Framework version — just ensure coordinates load correctly at startup. The profile manager moves to Core in Milestone 1.

---

## 3. Tesseract 5.5.2 Evaluation (1-2 days)

Validate that `TesseractOCR 5.5.2` fixes the threading deadlocks present in `Tesseract 5.2.0`.

See [Milestone 1, Step 1.3](MILESTONE_1_NET8_CORE.md) for the validation checklist (traineddata loading, concurrent engine stress test, accuracy comparison).

**Fallback if validation fails:** Add `SemaphoreSlim(4, 4)` to limit concurrent OCR calls in the existing engine pool. Keep `Tesseract 5.2.0`.

---

## 4. Configurable Data Sources

Move hard-coded GitHub URLs in `DatabaseManager.cs` to a `datasources.json` config file. Users pointing to a different fork (e.g., a community-maintained version) can do so without recompiling.

**Files:**
- Create: `InventoryKamera/inventorylists/datasources.json`
- Modify: `InventoryKamera/data/DatabaseManager.cs`

**New config file:**
```json
{
  "sources": {
    "characters": "https://raw.githubusercontent.com/Dimbreath/GenshinData/master/ExcelBinOutput/AvatarExcelConfigData.json",
    "weapons":    "https://raw.githubusercontent.com/Dimbreath/GenshinData/master/ExcelBinOutput/WeaponExcelConfigData.json",
    "artifacts":  "https://raw.githubusercontent.com/Dimbreath/GenshinData/master/ExcelBinOutput/ReliquaryExcelConfigData.json",
    "materials":  "https://raw.githubusercontent.com/Dimbreath/GenshinData/master/ExcelBinOutput/MaterialExcelConfigData.json"
  }
}
```

**Changes in `DatabaseManager.cs`:**
```csharp
private Dictionary<string, string> LoadDataSources()
{
    string path = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
        "inventorylists",
        "datasources.json");

    if (!File.Exists(path))
        return GetDefaultSources();

    return JsonConvert.DeserializeObject<DataSourcesConfig>(File.ReadAllText(path)).Sources;
}
```

**Scope:** Load URLs from JSON, fall back to hardcoded defaults if the file is missing or malformed. No UI for editing — a text editor is sufficient.

---

*Last updated: 2026-04-12*
