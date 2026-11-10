# Inventory Kamera — Modernization Roadmap

**Product vision:** Evolve from a scanner/exporter into a cross-platform tool that helps players make team-level and meta decisions — things single-character optimizers don't do.

**Status (2026-11-10):** Milestones 1 and 2 are complete; the Avalonia app is on master and the WinForms app is retired. Milestone 3 has not started. See [Where we are](#where-we-are).

---

## Three Milestones

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

*As of 2026-11-10*

**Done beyond the original plan** (scanner accuracy and speed, verified on a full account at Fast speed against the game and another exporter):

- Talent levels read from one screenshot of the Talents tab (falls back to clicking); constellation tab read as it opens. Character scans went from about 10m 44s to 7m 02s for 94 characters.
- Small, tested readers for single fields: character level and name, constellations, talents, material counts (label strip), locks (red padlock), refinement, equipped, elixir-crafted, substats.
- GOOD v3 export (`unactivatedSubstats`, `elixirCrafted`), constellation C3/C5 order derived from game data, configurable game data source.

**Open, not scheduled:**

- Run the macOS/Linux build on those platforms (scanning shows a "Windows only" warning there).
- Visual region editor for `ScanProfile.json` (Milestone 2 stretch goal).
- Ultrawide (21:9) support: express regions relative to the left, right or centre edge, scaled by height.
- Material count OCR: digit template matching instead of Tesseract, if count misreads return.

**Next:** Milestone 3.

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

*Last updated: 2026-11-10*
