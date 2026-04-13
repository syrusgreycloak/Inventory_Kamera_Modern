# Inventory Kamera Documentation

## Overview

Inventory Kamera is evolving from a Windows-only WinForms application to a cross-platform tool with AI-powered team and meta analysis. This directory contains the current planning documents.

## Start Here

**[ROADMAP.md](ROADMAP.md)** — Three-milestone modernization plan, key decisions, and what changed from the previous plans.

## Milestone Plans

| Document | Contents |
|----------|----------|
| [MILESTONE_1_NET8_CORE.md](MILESTONE_1_NET8_CORE.md) | .NET 8 migration, Core library extraction, Strangler Fig refactoring |
| [MILESTONE_2_AVALONIA.md](MILESTONE_2_AVALONIA.md) | Avalonia UI, MVVM, cross-platform builds, OCR preview panel |
| [MILESTONE_3_AI_ANALYSIS.md](MILESTONE_3_AI_ANALYSIS.md) | SQLite scan history, MCP server for Claude Desktop, GOOD v3 |
| [QUICK_WINS.md](QUICK_WINS.md) | elixirCrafted, ScanProfile.json, Tesseract evaluation — can be done now |

## Architecture Diagrams

PlantUML diagrams in `plantuml/` reflect the v1 architecture. They will be updated during Milestone 1.

| Diagram | Description |
|---------|-------------|
| [01-current-class-diagram.puml](plantuml/01-current-class-diagram.puml) | Current v1 class relationships |
| [02-proposed-class-diagram.puml](plantuml/02-proposed-class-diagram.puml) | Target Milestone 1 architecture |
| [05-component-diagram.puml](plantuml/05-component-diagram.puml) | Layer separation (Core/Infrastructure/UI) |

## Archived Plans

Previous phase-based planning documents are in `docs/archive/`. They contain more detailed research, UI mockups, and code examples that informed the current milestone plans.

---

*Last updated: 2026-04-12*
*Status: Planning complete, Milestone 1 not yet started*
