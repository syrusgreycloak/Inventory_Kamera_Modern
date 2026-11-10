# Inventory Kamera Documentation

## Overview

Inventory Kamera Modern has moved from a Windows-only WinForms application to an Avalonia app on .NET 8 (Milestones 1 and 2, complete). Next are Linux scanning (Milestone 2.5) and AI-powered team and meta analysis (Milestone 3). This directory contains the planning documents.

## Start Here

**[ROADMAP.md](ROADMAP.md)** — Milestone plan and current status, key decisions, and what changed from the previous plans.

## Milestone Plans

| Document | Contents |
|----------|----------|
| [MILESTONE_1_NET8_CORE.md](MILESTONE_1_NET8_CORE.md) | ✅ .NET 8 migration, Core library extraction, Strangler Fig refactoring |
| [MILESTONE_2_AVALONIA.md](MILESTONE_2_AVALONIA.md) | ✅ Avalonia UI, MVVM, cross-platform builds, OCR preview panel, WinForms removal |
| [MILESTONE_2_5_LINUX_SCANNING.md](MILESTONE_2_5_LINUX_SCANNING.md) | Platform-independent scanner, Linux capture/input for scanning under Wine/Proton (planned) |
| [MILESTONE_3_AI_ANALYSIS.md](MILESTONE_3_AI_ANALYSIS.md) | SQLite scan history, MCP server for Claude Desktop (not started; GOOD v3 done) |
| [QUICK_WINS.md](QUICK_WINS.md) | ✅ elixirCrafted, ScanProfile.json, Tesseract upgrade, configurable data source |

## Architecture Diagrams

PlantUML diagrams in `plantuml/` reflect the v1 architecture and the plan for Milestone 1; they have not been updated since. `CLAUDE.md` at the repository root describes the current projects.

| Diagram | Description |
|---------|-------------|
| [01-current-class-diagram.puml](plantuml/01-current-class-diagram.puml) | Current v1 class relationships |
| [02-proposed-class-diagram.puml](plantuml/02-proposed-class-diagram.puml) | Target Milestone 1 architecture |
| [05-component-diagram.puml](plantuml/05-component-diagram.puml) | Layer separation (Core/Infrastructure/UI) |

## Archived Plans

Previous phase-based planning documents are in `docs/archive/`. They contain more detailed research, UI mockups, and code examples that informed the current milestone plans.

---

*Last updated: 2026-11-10*
*Status: Milestones 1 and 2 complete, Milestone 3 not started*
