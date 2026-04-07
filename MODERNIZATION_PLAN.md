# Inventory Kamera Modernization Plan

This document outlines the planned improvements to Inventory Kamera to improve maintainability, resilience, and user experience.

## Background

The current application has proven fragile when external dependencies change:
- Hard-coded GitHub repository URLs that broke when the data source moved from `Dimbreath/GenshinData` to `DimbreathBot/AnimeGameData`
- No graceful handling of missing or unreleased game data
- Fixed-size WinForms UI that's Windows-only and cramped

This plan addresses these issues in two phases.

---

## Phase 1: Configurable Data Sources

**Goal:** Make the application resilient to future data source changes without requiring recompilation.

### Current State
All data source URLs are hard-coded constants in `DatabaseManager.cs`:
```csharp
private const string CharactersURL = "https://raw.githubusercontent.com/DimbreathBot/AnimeGameData/master/ExcelBinOutput/AvatarExcelConfigData.json";
private const string MappingsURL = "https://raw.githubusercontent.com/DimbreathBot/AnimeGameData/master/TextMap/TextMapEN.json";
// ... etc
```

### Proposed Changes

#### 1.1 Create Configuration File
- Add `datasources.json` configuration file with default URLs
- Structure:
  ```json
  {
    "repository": {
      "baseUrl": "https://raw.githubusercontent.com/DimbreathBot/AnimeGameData/master",
      "endpoints": {
        "characters": "/ExcelBinOutput/AvatarExcelConfigData.json",
        "constellations": "/ExcelBinOutput/AvatarTalentExcelConfigData.json",
        "skills": "/ExcelBinOutput/AvatarSkillExcelConfigData.json",
        "artifacts": "/ExcelBinOutput/DisplayItemExcelConfigData.json",
        "artifactsCodex": "/ExcelBinOutput/ReliquaryCodexExcelConfigData.json",
        "setArtifacts": "/ExcelBinOutput/ReliquaryExcelConfigData.json",
        "weapons": "/ExcelBinOutput/WeaponExcelConfigData.json",
        "materials": "/ExcelBinOutput/MaterialExcelConfigData.json",
        "mappings": "/TextMap/TextMapEN.json"
      }
    },
    "fallbackRepositories": [
      {
        "name": "Sycamore0 Fork",
        "baseUrl": "https://raw.githubusercontent.com/Sycamore0/GenshinData/master"
      }
    ]
  }
  ```

#### 1.2 Configuration Manager
- Create `DataSourceConfigManager` class to:
  - Load configuration from `datasources.json`
  - Provide URL builder methods
  - Validate configuration on load
  - Support runtime configuration reload

#### 1.3 Settings UI Integration
- Add "Data Sources" tab in Options menu
- Allow users to:
  - View current repository URL
  - Switch to fallback repository
  - Manually enter custom repository URL
  - Test connection to repository
  - Reset to defaults

#### 1.4 Fallback Logic
- Implement automatic fallback when primary repository fails:
  1. Try primary repository
  2. On 404/timeout, try fallback repositories in order
  3. Log which repository succeeded
  4. Remember last successful repository for next run

### Benefits
- ✅ Users can fix broken updates themselves by changing configuration
- ✅ Community can maintain forks with updated URLs
- ✅ No recompilation needed when data sources change
- ✅ Graceful degradation when primary source is unavailable

### Implementation Estimate
- Small scope, can be done within existing .NET Framework 4.7.2 WinForms app
- No UI framework changes required
- Backwards compatible with existing installations

---

## Phase 2: UI Modernization & Cross-Platform Support

**Goal:** Create a modern, cross-platform desktop application with better UX.

### Current State
- Windows Forms (.NET Framework 4.7.2)
- Fixed window size (non-resizable)
- Cramped layout with absolute positioning
- Windows-only
- Dated visual design

### Target State
- Cross-platform desktop app (Windows, macOS, Linux)
- Resizable, responsive layouts
- Modern UI design
- Better user experience and accessibility

### Technology Options

#### Option A: Avalonia UI (Recommended)
**Pros:**
- Cross-platform (Windows, macOS, Linux)
- XAML-based (similar to WPF, easier migration from WinForms concepts)
- Good performance
- Active development and community
- Supports .NET 6+

**Cons:**
- Steeper learning curve than MAUI for desktop
- Smaller ecosystem than WPF

#### Option B: .NET MAUI
**Pros:**
- Official Microsoft framework
- Cross-platform (Windows, macOS, iOS, Android)
- XAML-based
- Good tooling in Visual Studio

**Cons:**
- Desktop support still maturing
- Heavier runtime
- More mobile-focused

**Recommendation:** Avalonia UI for better desktop focus and Linux support

### Proposed Changes

#### 2.1 Project Migration
- **New Project Structure:**
  ```
  InventoryKamera.sln
  ├── InventoryKamera.Core/          # Shared business logic (target: .NET 8)
  │   ├── Data/                      # DatabaseManager, GOOD export
  │   ├── Scanning/                  # OCR, scrapers
  │   └── Models/                    # Character, Weapon, Artifact
  ├── InventoryKamera.Avalonia/      # New Avalonia UI (target: .NET 8)
  │   ├── Views/                     # XAML views
  │   ├── ViewModels/                # MVVM view models
  │   └── Services/                  # Platform-specific services
  └── InventoryKamera.Legacy/        # Current WinForms app (maintenance only)
  ```

- Migrate to **.NET 8** (current LTS)
- Extract business logic to `.Core` library
- Implement new UI in `.Avalonia` project
- Keep legacy WinForms app for reference/fallback

#### 2.2 Core Library Extraction
**Extract from WinForms dependencies:**
- `DatabaseManager` - No UI dependencies
- `Scraper`, `ArtifactScraper`, `CharacterScraper`, etc. - Pure logic
- `GOOD`, `Inventory` - Data models
- `Navigation` - Will need platform abstraction for screen capture

**Requires platform abstraction:**
- Screen capture (Windows: `Graphics.CopyFromScreen`, cross-platform: SkiaSharp or Avalonia)
- Input simulation (Windows: `WindowsInput`, cross-platform: platform-specific implementations)
- Window detection (process enumeration, focus management)

#### 2.3 New UI Design

**Main Window Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Inventory Kamera                          [─][□][×] │
├─────────────────────────────────────────────────────┤
│ ┌─────────┐                                         │
│ │  Scan   │  Scan Settings:                         │
│ │ Settings│  ☑ Characters  ☑ Weapons                │
│ │         │  ☑ Artifacts   ☑ Materials              │
│ │  Game   │  Min Rarity: [3★ ▼]                     │
│ │ Status  │                                          │
│ │         │  Scan Speed: [Normal ──●────── Fast]    │
│ │ Output  │                                          │
│ │         │  Output: [C:\...\GenshinData  [...] ]   │
│ │ Options │                                          │
│ └─────────┘  [Scan Inventory]                       │
│                                                      │
│ ┌──────────────────────────────────────────────┐   │
│ │ Scan Progress                                 │   │
│ │ Characters: 45/87  ████████░░░░░░░  52%      │   │
│ │ Weapons:    123/245 ████████░░░░░░░ 50%      │   │
│ │                                               │   │
│ │ [Preview of current item being scanned]      │   │
│ └──────────────────────────────────────────────┘   │
│                                                      │
│ Console Output:                                      │
│ ┌──────────────────────────────────────────────┐   │
│ │ [2026-04-07 12:45] Starting scan...          │   │
│ │ [2026-04-07 12:45] Scanning characters...    │   │
│ │ [2026-04-07 12:45] Found: Hu Tao (C1, 90)    │   │
│ └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**Improvements:**
- Resizable window with responsive layout
- Proper spacing and padding
- Real-time scan progress with visual feedback
- Modern progress bars
- Console/log output area
- Tabbed interface for advanced settings
- Light/dark mode support

#### 2.4 MVVM Architecture
- Implement proper separation of concerns
- ViewModels handle business logic
- Views are purely declarative (XAML)
- Commands for user actions
- Observable properties for data binding

#### 2.5 Platform-Specific Implementations

**Windows:**
- Use existing `WindowsInput` for input simulation
- `Graphics.CopyFromScreen` for screen capture
- Process enumeration for finding Genshin Impact

**macOS:**
- CGWindowListCreateImage for screen capture
- Core Graphics for input simulation
- Process enumeration via NSRunningApplication

**Linux:**
- X11/Wayland screen capture
- xdotool or similar for input simulation
- Process enumeration via /proc

Implement these as platform services with common interfaces.

#### 2.6 Enhanced Features (Nice-to-have)

- **Scan history:** Track past scans, diff between scans
- **Data visualization:** Charts showing character/weapon distribution
- **Export formats:** Support additional formats beyond GOOD (CSV, Excel, etc.)
- **Scan profiles:** Save different scan configurations
- **Automation:** Schedule periodic scans
- **Cloud sync:** Optional backup to cloud storage

### Migration Strategy

1. **Phase 2.1:** Extract core logic to `.Core` library
2. **Phase 2.2:** Create basic Avalonia UI with main workflow
3. **Phase 2.3:** Implement platform abstractions for Windows
4. **Phase 2.4:** Feature parity with WinForms version
5. **Phase 2.5:** Add Linux/macOS support
6. **Phase 2.6:** Enhanced features and polish
7. **Phase 2.7:** Beta testing with community
8. **Phase 2.8:** Release v2.0

### Benefits
- ✅ Linux users (Steam Deck!) can use the tool
- ✅ macOS users can use the tool
- ✅ Modern, responsive UI
- ✅ Better UX and accessibility
- ✅ Easier to maintain (MVVM, proper separation)
- ✅ Future-proof (.NET 8+)

### Implementation Estimate
- Major undertaking, essentially a rewrite
- 3-6 months for feature parity with careful planning
- Suitable for a new major version (v2.0)

---

## Branch Strategy

```
master (main development)
├── fix/defensive-mapping-checks (current - bug fixes for v1.x)
├── feat/configurable-data-sources (Phase 1)
└── modernize/avalonia-ui (Phase 2)
    ├── feat/core-library-extraction
    ├── feat/avalonia-ui-foundation
    ├── feat/mvvm-implementation
    ├── feat/platform-abstractions
    └── feat/cross-platform-support
```

---

## Success Criteria

### Phase 1 Success
- [ ] Users can change data source URLs without recompiling
- [ ] Application automatically falls back to alternative sources
- [ ] Settings UI allows testing connection to repositories
- [ ] No breaking changes to existing functionality

### Phase 2 Success
- [ ] Application runs on Windows, macOS, and Linux
- [ ] All existing features work in new UI
- [ ] Window is resizable with proper responsive layout
- [ ] Scan workflow is smoother and more intuitive
- [ ] Performance is equal to or better than WinForms version
- [ ] Community feedback is positive

---

## Community Impact

### For Users
- **Phase 1:** More reliable updates, self-service fixes
- **Phase 2:** Cross-platform support, better UX, modern design

### For Steam Deck/Linux Users
- Currently cannot use the tool (Wine might work but is unreliable)
- Phase 2 enables native Linux support
- Genshin Impact runs on Steam Deck via Proton
- This would be the first native inventory scanner for Linux

### For Contributors
- Phase 1: Minimal barrier to entry, same tech stack
- Phase 2: Modern architecture, cleaner codebase, easier to contribute

---

## Next Steps

1. ✅ Complete current bug fixes (defensive checks, URL updates, enable update menu)
2. ✅ Submit PR to upstream project
3. **Start Phase 1:** Create issue for configurable data sources
4. Gather community feedback on Phase 2 plans
5. Begin Phase 1 implementation in separate branch
