# Inventory Kamera — Modernization Roadmap

**Product vision:** Evolve from a scanner/exporter into a cross-platform tool that helps players make team-level and meta decisions — things single-character optimizers don't do.

**Status:** Planning complete, implementation not yet started.

---

## Three Milestones

### Milestone 1: .NET 8 + Core Extraction
*Foundation — enables everything else*

Migrate from .NET Framework 4.7.2 to .NET 8. Extract business logic from the WinForms project into a platform-agnostic Core library. WinForms remains the working UI throughout.

**Why first:** Cannot create a .NET 8 Core library without first migrating the WinForms project. `Thread.Abort()` used in MainForm.cs is not supported on .NET 8 and must be fixed.

→ [Full plan](MILESTONE_1_NET8_CORE.md)

---

### Milestone 2: Avalonia UI + Cross-Platform
*Visible payoff — users on all platforms*

New Avalonia UI sharing the Core library. WinForms remains available until Avalonia reaches full feature parity, then is retired. Windows-only scanning initially; macOS/Linux scanning added in sub-phases.

→ [Full plan](MILESTONE_2_AVALONIA.md)

---

### Milestone 3: Scan History + AI Analysis
*Product differentiation*

SQLite database for scan history (reference data stays as JSON). MCP server exposes the database to Claude Desktop for team/meta analysis that single-character optimizers cannot do.

→ [Full plan](MILESTONE_3_AI_ANALYSIS.md)

---

## Quick Wins (Any Time)

These are independent of the milestones and can be done at any point:

- **GOOD v3 `elixirCrafted`** — detection already exists in `IsSanctified()`, just needs wiring to the export
- **ScanProfile.json** — externalize hard-coded scan region coordinates
- **Tesseract 5.5.2 evaluation** — validate newer package fixes threading issues
- **Configurable data sources** — move hard-coded GitHub URLs to `datasources.json`

→ [Quick wins details](QUICK_WINS.md)

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

*Last updated: 2026-04-12*
