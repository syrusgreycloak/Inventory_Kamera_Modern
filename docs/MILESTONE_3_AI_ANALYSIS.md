# Milestone 3: Scan History + AI Analysis

**Goal:** SQLite scan history database enables cross-scan artifact tracking and AI-powered team/meta analysis via MCP server — the differentiator from single-character optimizers.

**Prerequisite:** Milestone 2 complete (Avalonia UI stable, Core library clean).

**Status:** Not started.

---

## Product Vision

This milestone is why Inventory Kamera is more than a GOOD exporter. Online optimizers (Genshin Optimizer, etc.) optimize one character at a time. They cannot:

- Allocate artifacts across an entire 30+ character roster
- Evaluate team-level synergies ("should Raiden's ER be at 240% given this specific team?")
- Recommend build switches based on current meta context
- Show the gap between current and optimal artifact sets across all your characters at once

An MCP server exposing the scan history database lets users query this via Claude Desktop using their existing subscription — no embedded chat UI to build or maintain.

---

## Data Storage Decision

| Data | Storage | Reason |
|------|---------|--------|
| `inventorylists/*.json` (characters, weapons, artifacts, materials) | **Keep as JSON** | ~700 static entries, loaded once at startup into dictionaries, git-diffable, community-editable via PR — no benefit from SQLite |
| Scan results, artifact history, team compositions | **New SQLite DB** | Grows over time, needs cross-scan queries, benefits from indexes and relationships |

**Do not migrate reference data to SQLite.** That solves a problem that doesn't exist.

---

## Phase 3.1: Scan History Database

### New project: InventoryKamera.Database.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11" />
    <ProjectReference Include="..\InventoryKamera.Core\InventoryKamera.Core.csproj" />
  </ItemGroup>
</Project>
```

### Schema

```csharp
// One record per scan run
public class ScanSession
{
    public int Id { get; set; }
    public DateTime ScannedAt { get; set; }
    public string GameVersion { get; set; }     // e.g. "6.5"
    public string AppVersion { get; set; }      // e.g. "1.4.0"
    public ICollection<ScannedArtifact> Artifacts { get; set; }
    public ICollection<ScannedWeapon> Weapons { get; set; }
    public ICollection<ScannedCharacter> Characters { get; set; }
}

// Artifact snapshot per scan
public class ScannedArtifact
{
    public int Id { get; set; }
    public int ScanSessionId { get; set; }
    public string SetKey { get; set; }        // GOOD key, e.g. "CrimsonWitchOfFlames"
    public string SlotKey { get; set; }       // flower / plume / sands / goblet / circlet
    public string MainStatKey { get; set; }   // GOOD stat key, e.g. "critRate_"
    public int Level { get; set; }
    public int Rarity { get; set; }
    public bool ElixirCrafted { get; set; }
    public bool Locked { get; set; }
    public string Location { get; set; }      // Equipped character GOOD name, or empty
    public string SubstatsJson { get; set; }  // JSON array: [{"key":"critRate_","value":3.5},...]
    public string ArtifactHash { get; set; }  // setKey_slot_mainStatKey_substatKeys (for cross-scan matching)
    public ScanSession ScanSession { get; set; }
}

// Weapon snapshot per scan
public class ScannedWeapon
{
    public int Id { get; set; }
    public int ScanSessionId { get; set; }
    public string Key { get; set; }           // GOOD key, e.g. "StaffOfHoma"
    public int Level { get; set; }
    public int Refinement { get; set; }
    public bool Locked { get; set; }
    public string Location { get; set; }
    public ScanSession ScanSession { get; set; }
}

// Character snapshot per scan
public class ScannedCharacter
{
    public int Id { get; set; }
    public int ScanSessionId { get; set; }
    public string Key { get; set; }           // GOOD key, e.g. "HuTao"
    public int Level { get; set; }
    public int Constellation { get; set; }
    public int AscensionPhase { get; set; }
    public ScanSession ScanSession { get; set; }
}
```

### Database location

```csharp
private static string GetDatabasePath() =>
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "InventoryKamera",
        "scan_history.db"
    );
```

### Saving after a scan

After the GOOD JSON export completes, also persist to SQLite:

```csharp
public class ScanHistoryService
{
    private readonly ScanHistoryDbContext _dbContext;

    public async Task SaveScanAsync(Inventory inventory, string gameVersion, string appVersion)
    {
        var session = new ScanSession
        {
            ScannedAt = DateTime.UtcNow,
            GameVersion = gameVersion,
            AppVersion = appVersion
        };

        foreach (var artifact in inventory.Artifacts)
        {
            session.Artifacts.Add(new ScannedArtifact
            {
                SetKey = artifact.SetKey,
                SlotKey = artifact.GearSlot,
                MainStatKey = artifact.MainStat,
                Level = artifact.Level,
                Rarity = artifact.Rarity,
                ElixirCrafted = artifact.ElixirCrafted,
                Locked = artifact.Locked,
                Location = artifact.EquippedCharacter ?? "",
                SubstatsJson = JsonConvert.SerializeObject(artifact.SubStats),
                ArtifactHash = ComputeArtifactHash(artifact)
            });
        }

        // Similar for weapons and characters...

        _dbContext.ScanSessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    private static string ComputeArtifactHash(Artifact a) =>
        $"{a.SetKey}_{a.GearSlot}_{a.MainStat}_{string.Join("_", a.SubStats.Select(s => s.Key).OrderBy(k => k))}";
}
```

---

## Phase 3.2: MCP Server

### New project: InventoryKamera.Mcp.csproj

A lightweight .NET console app implementing the Model Context Protocol. It exposes the scan history database to Claude Desktop, allowing natural language queries about your inventory.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.1.0-preview.*" />
    <ProjectReference Include="..\InventoryKamera.Database\InventoryKamera.Database.csproj" />
  </ItemGroup>
</Project>
```

### Core MCP tools

```csharp
[McpServerTool]
[Description("Query your scanned artifact inventory. Returns matching artifacts with stats.")]
public async Task<string> QueryInventory(
    [Description("Filter by set name, slot, main stat, or equipped character. Leave empty for all.")] string filter = null)

[McpServerTool]
[Description("Find the best artifacts from your inventory for a specific character and role.")]
public async Task<string> FindBestArtifacts(
    [Description("Character GOOD name, e.g. HuTao, RaidenShogun")] string characterName,
    [Description("Character role, e.g. 'main DPS', 'support', 'healer', 'shielder'")] string role)

[McpServerTool]
[Description("Analyze artifact allocation across a team. Identifies conflicts and recommends optimizations.")]
public async Task<string> AnalyzeTeam(
    [Description("Up to 4 character GOOD names, comma-separated, e.g. HuTao,Yelan,ZhongLi,Furina")] string characters)

[McpServerTool]
[Description("List your scan history sessions.")]
public async Task<string> ListSessions()

[McpServerTool]
[Description("Compare two scan sessions to find new, removed, or changed items.")]
public async Task<string> CompareSessions(
    [Description("ID of the earlier scan session")] int sessionId1,
    [Description("ID of the more recent scan session")] int sessionId2)
```

### Claude Desktop configuration

Document this in the project README for users:

```json
{
  "mcpServers": {
    "inventory-kamera": {
      "command": "C:\\Program Files\\InventoryKamera\\InventoryKamera.Mcp.exe"
    }
  }
}
```

Once configured, users can ask Claude Desktop questions like:
- "What are my best artifacts for a shielder Zhongli build?"
- "I'm building a Hu Tao / Yelan / Zhongli / Furina team — are my artifacts optimally distributed?"
- "What has changed in my inventory since last month?"
- "Which characters are missing their best-in-slot artifacts?"

---

## Phase 3.3: GOOD v3 Fields

These improve the quality of data available to AI analysis.

### elixirCrafted (quick win — detection already exists)

`ArtifactScraper.IsSanctified()` already detects the purple elixir indicator. Just needs wiring:

In `InventoryKamera/game/Artifact.cs`:
```csharp
[JsonProperty("elixirCrafted")]
public bool ElixirCrafted { get; internal set; }
```

In `ArtifactScraper.CatalogueFromBitmapsAsync()`, after the existing sanctified detection:
```csharp
bool isSanctified = IsSanctified(card);
// existing coordinate shift logic...
artifact.ElixirCrafted = isSanctified;  // ADD THIS
```

In `GOOD.cs`:
```csharp
Version = 3;  // was 2
```

### unactivatedSubstats (medium — text detection)

5-star artifacts below level 4 show unactivated substats with `"(unactivated)"` text. Detect in the substat OCR output:

```csharp
private void ParseSubstats(string ocrText,
    out List<SubStat> activeSubstats, out List<SubStat> unactivatedSubstats)
{
    activeSubstats = new List<SubStat>();
    unactivatedSubstats = new List<SubStat>();

    foreach (string line in ocrText.Split('\n'))
    {
        bool isUnactivated = line.Contains("(unactivated)", StringComparison.OrdinalIgnoreCase);
        string cleanLine = line.Replace("(unactivated)", "").Trim();
        SubStat substat = ParseSubstatLine(cleanLine);

        if (substat != null)
        {
            if (isUnactivated) unactivatedSubstats.Add(substat);
            else activeSubstats.Add(substat);
        }
    }
}
```

Add `UnactivatedSubstats` to `Artifact.cs`:
```csharp
[JsonProperty("unactivatedSubstats")]
public List<SubStat> UnactivatedSubstats { get; internal set; } = new();
```

### totalRolls (optional, uses Reshape screen)

- **Level 0-3:** `totalRolls = 0` (no upgrades applied)
- **Level 20, Reshape scanning enabled:** Navigate to Reshape screen, OCR roll count badges (①②③④⑤), sum them
- **Other levels:** Omit field (cannot determine without Reshape screen)

Reshape scanning is opt-in due to ~2-3 second overhead per eligible artifact.

---

## Milestone 3 Complete When

- [ ] Scan results saved to SQLite after each scan completes
- [ ] MCP server starts and registers tools with Claude Desktop
- [ ] `query_inventory` returns current artifact/weapon/character data
- [ ] `analyze_team` returns artifact allocation recommendations for a 4-character team
- [ ] `elixirCrafted` field exported in GOOD v3 JSON
- [ ] GOOD version updated to 3
- [ ] Claude Desktop MCP configuration documented in project README

---

*See `docs/archive/PHASE_3_AI_ASSISTANT.md` for the original MCP server design.*
*See `docs/archive/PHASE_1.6_DATA_STORAGE.md` for the earlier (broader) SQLite proposal.*
*See `docs/archive/PHASE_2.5_GOOD_V3.md` for detailed GOOD v3 field research.*
*Last updated: 2026-04-12*
