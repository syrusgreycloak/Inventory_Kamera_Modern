# Inventory Kamera — Modernization Roadmap

**Product vision:** Evolve from a scanner/exporter into a cross-platform tool that helps players make team-level and meta decisions — things single-character optimizers don't do.

**Status (2026-11-10):** Milestones 1 and 2 are complete; the Avalonia app is on master and the WinForms app is retired. Milestone 3 has not started. See [Where we are](#where-we-are).

---

## Milestones

### Milestone 1: .NET 8 + Core Extraction
*Foundation — enables everything else*

**Complete (2026-04-27).** Migrate from .NET Framework 4.7.2 to .NET 8. Extract business logic from the WinForms project into a platform-agnostic Core library. WinForms remains the working UI throughout.

**Why first:** Cannot create a .NET 8 Core library without first migrating the WinForms project. `Thread.Abort()` used in MainForm.cs is not supported on .NET 8 and must be fixed.

→ [Full plan](MILESTONE_1_NET8_CORE.md)

---

### Milestone 2: Avalonia UI + Cross-Platform
*Visible payoff — users on all platforms*

**Complete (2026-11-10).** New Avalonia UI sharing the Core library. WinForms remained available until Avalonia reached feature parity, then was removed from master (kept on the `history` branch and the `winforms-final` tag). Windows-only scanning; the macOS/Linux build compiles but hasn't been run on those platforms.

→ [Full plan](MILESTONE_2_AVALONIA.md)

---

### Milestone 2.5: Linux Scanning
*Scan from Linux while the game runs under Wine/Proton*

**Planned.** Make the scanner platform-independent (move it to a `net8.0` project, port OCR preprocessing from System.Drawing to ImageSharp, route the last direct Windows calls through the interfaces), then add Linux screen capture, input and window lookup (X11/XTest, or uinput). Independent of Milestone 3; the first step is worth doing anyway.

→ [Full plan](MILESTONE_2_5_LINUX_SCANNING.md)

---

### Milestone 3: Scan History + AI Analysis
*Product differentiation*

**Not started.** SQLite database for scan history (reference data stays as JSON). MCP server exposes the database to Claude Desktop for team/meta analysis that single-character optimizers cannot do.

→ [Full plan](MILESTONE_3_AI_ANALYSIS.md)

---

## Quick Wins (Any Time)

All four are done:

- ✅ **GOOD v3 `elixirCrafted`** — exported (2026-11-10, `ElixirReader`)
- ✅ **ScanProfile.json** — scrapers read region coordinates from it
- ✅ **Tesseract 5.5.2** — evaluated and upgraded (`TesseractOCR 5.5.2`)
- ✅ **Configurable data sources** — done as a Settings > Advanced option instead of `datasources.json`

→ [Quick wins details](QUICK_WINS.md)

---

## Where We Are

*As of 2026-11-12*

**Done beyond the original plan** (scanner accuracy and speed, verified on a full account at Fast speed against the game and another exporter):

- Talent levels read from one screenshot of the Talents tab (falls back to clicking); constellation tab read as it opens. Character scans went from about 10m 44s to 7m 02s for 94 characters.
- Small, tested readers for single fields: character level and name, constellations, talents, material counts (label strip), locks (red padlock), refinement, equipped, elixir-crafted, substats.
- GOOD v3 export (`unactivatedSubstats`, `elixirCrafted`), constellation C3/C5 order derived from game data, configurable game data source.

**Open, not scheduled:**

- Run the Linux build (UI only) on the CachyOS laptop; scanning on Linux is Milestone 2.5.
- Visual region editor for `ScanProfile.json` (Milestone 2 stretch goal).
- Ultrawide (21:9) support: express regions relative to the left, right or centre edge, scaled by height.
- Material count OCR: digit template matching instead of Tesseract, if count misreads return.

**Released:** v2.0.0 and v2.0.1 (2026-11-11), v2.1.0 (2026-11-12).

**Version 2.1: scan order and navigation** (released as v2.1.0, verified with full scans)

- ✅ **Inventory in one visit, characters last.** The scan opens the inventory once and goes tab to tab in the game's order: Weapons → Artifacts → Character Dev Items → Materials, then Characters, then links equipped weapons and artifacts. It used to go back to the Paimon menu and reopen the inventory for every category, with characters between artifacts and Character Dev Items to overlap artifact OCR; a logged full scan showed no backlog (last artifact read 0.4 s after capture). Same export as the old order, about 20 s faster.
- ✅ **Direct tab switching.** Each tab starts at the top the first time it's opened after entering the inventory (checked in-game). After clicking a tab, the scan reads its title (`InventoryTabReader`); on a mismatch it reopens the inventory (Escape → B) so every tab starts at the top again.
- ✅ **Start from almost any screen.** No need to open the Paimon menu first: the scan checks for it (`PaimonMenuReader`, the profile card's "Adventure Rank"/"World Level" labels) and presses Escape up to three times until it appears. Tested from the open world, the menu, Party Setup, the character screen, Battle Pass, the map and shops.
- ✅ **UI in scan order.** Checkboxes and progress bars: Weapons, Artifacts, Character Dev Items, Materials, Characters.
- ✅ **Smaller release zip.** English resources only, no 32-bit OCR libraries: 3 folders instead of 17, 202 MB instead of 224 MB unzipped.
- ✅ **Fixes found on the way:** OCR workers finishing set the scan's cancel flag (the scan stopped after Character Dev Items once characters moved last); every scan after the first in an app session captured the wrong screen area (window position doubled).
- ⏳ **ImageSharp 3.2**: not in 2.1. Waits for the free Six Labors community key (fixes the 2026-10-07 advisories; 3.2 needs the key for Release builds).

**After that:** Milestone 2.5 step 1 (platform-independent scanner), then Milestone 3. They're independent, so the order can change.

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| .NET 8 migration is Step 1 | Eliminates multi-targeting friction; `Thread.Abort()` must be fixed anyway |
| SQLite for scan history only | Reference data (`inventorylists/*.json`) is 27-50KB, loaded once, git-diffable — no benefit from SQLite |
| Avalonia over MAUI | Better desktop focus, better Linux support |
| CommunityToolkit.Mvvm over ReactiveUI | Simpler, right size for this project |
| Keep Newtonsoft.Json | GOOD format has complex serialization; not worth migrating during structural refactor |
| Phase 3 is the product vision | Team/meta analysis is the real differentiator from existing tools |

---

## What Changed From Previous Plans

The previous 7 phase documents are archived in `docs/archive/`. Key changes:

- Phase 1.6 (SQLite for all data) narrowed to scan history only
- Phase 1.5 .NET 8 migration moved from Week 11-12 to Step 1
- Conditional region shift engine removed (over-designed; handle in code)
- Week-by-week schedule replaced with milestone deliverables
- Phase 3 recognized as product vision, not scope creep

---

*Last updated: 2026-11-12*
