# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Inventory Kamera Modern scans Genshin Impact characters, weapons, artifacts and materials with OCR and exports them in GOOD (Genshin Open Object Description) JSON, the format used by Genshin Optimizer and similar tools. It captures screenshots of the game window, drives the game menus with simulated keyboard/mouse input, and reads the screenshots with Tesseract and a custom trained model for the game's font.

The app is an Avalonia UI on .NET 8. The old Windows Forms version was removed from master; it is preserved on the `history` branch and the `winforms-final` tag.

## Build and Test

```bash
# Build the app (both targets: net8.0 and net8.0-windows)
dotnet build InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj -c Debug

# Run (Windows build; asks for administrator rights)
InventoryKamera.Avalonia/bin/Debug/net8.0-windows/InventoryKamera.Avalonia.exe

# Tests
dotnet test InventoryKamera.Tests/InventoryKamera.Tests.csproj
dotnet test InventoryKamera.Infrastructure.Tests/InventoryKamera.Infrastructure.Tests.csproj
```

- The build fails with MSB3027 (file locked) while the app is running; close it first.
- The app manifest requires administrator rights (to send input to the elevated game). Running from Rider needs Rider started as administrator and the `net8.0-windows` target.
- The `net8.0` target builds the UI for macOS/Linux with stub platform services; scanning only works on `net8.0-windows`.

## Projects

- **`InventoryKamera.Core`** (net8.0, no System.Drawing): models (`Character`, `Weapon`, `Artifact`, `Material`, `Inventory`), GOOD export (`Export/GOOD.cs`), game data (`Data/DatabaseManager.cs`, `Data/GameDataSource.cs`), settings and scan profile (`Configuration/`), and the platform interfaces in `Abstractions/` (`IScreenCapture`, `IInputSimulator`, `IOcrEngine`, `IImageProcessor`, `IUserInterface`, `IScanRunner`, `IGameDataService`).
- **`InventoryKamera.Infrastructure`** (net8.0-windows): the Windows implementations and the scanner.
  - `WindowsScanRunner` / `InventoryScanner`: run a scan and the OCR worker threads.
  - `Windows/Navigation.cs`: window capture, input, aspect ratio handling.
  - `Scraping/*Scraper.cs`: one scraper per inventory type.
  - `Scraping/*Reader.cs`: small, testable readers for single fields (talent list, constellations, character level and name, material counts, locks, refinement, equipped, artifact substats). New screen-reading logic should go in a reader with tests, not inline in a scraper.
  - `Scraping/GenshinProcesor.cs`: OCR helpers and name matching (spelling is historical).
  - `OcrEnginePool.cs`: pool of Tesseract engines; disposed on app exit.
- **`InventoryKamera.Avalonia`**: MVVM UI with CommunityToolkit.Mvvm. `App.axaml.cs` sets up DI (Windows services on Windows, `Services/PlatformStubs/` elsewhere). Views: Scan, Export, Settings, About.
- **`InventoryKamera.Tests`**: ViewModel, settings, GOOD export and game data tests.
- **`InventoryKamera.Infrastructure.Tests`**: reader tests against real screenshot crops in `TestData/`, using the real trained data.

## Data Files

- **`reference_data/`**: verified game data (`characters.json`, `weapons.json`, `artifacts.json`, `materials.json`, `version.txt`), `ScanProfile.json` (scan region coordinates) and `stat_priorities.json` (planned for Milestone 3). Copied to `inventorylists/` in the build output. `Update game data` (About tab) refreshes the game data at runtime from the configured repository (default: Dimbreath's animegamedata2; changeable in Settings > Advanced).
- **`tessdata/`**: the custom trained data (`genshin_best_eng.traineddata`, `genshin_fast_09_04_21.traineddata`). If OCR accuracy suddenly degrades, check these are in the output's `tessdata/`.
- Settings are stored in `%AppData%/InventoryKamera/settings.json`.

## Scanning Architecture

Producer-consumer: scrapers navigate the game and capture screenshots on the scan thread, crop regions and enqueue them to `InventoryScanner.workerQueue`; worker threads dequeue, run OCR, validate and store results. An "END" message stops the workers. UI updates from worker threads go through `IUserInterface` (`AvaloniaUserInterface` posts to the Avalonia UI thread); never touch Avalonia controls from a worker thread.

## Important Development Notes

### Resolution and aspect ratio
The game must run at 16:9 or 16:10; ultrawide (21:9) is not supported yet. Region coordinates are proportional to the game window (reference 1920x1080 or 1280x720), either from `ScanProfile.json` or scaled in code. When adding regions, always scale from a reference resolution.

### Scan speed
Fast (scanner delay 0) is the default and is verified to match the game on a full account. UI elements can still be animating when a screenshot is taken on Fast (e.g. the lock icon fading in), so readers should tolerate partial fades rather than rely on single pixels.

### Traveler and Wanderer
Their names are user-specific. The Traveler's name is OCR'd (or taken from settings) and a character entry is cloned from the generic `traveler` template in `characters.json`; the element comes from the character screen header.

### Debugging OCR issues
1. Turn on "Log all scan screenshots" in Settings.
2. Run a scan; images go to `logging/` in the output folder (wiped at the start of each scan).
3. Inspect the crops, add a failing crop to `InventoryKamera.Infrastructure.Tests/TestData/`, and fix the reader against the test.
4. Never put the player's UID or other personal details in test data; crop it out.

### Updating reference data
After a game update, run `Update game data`, then diff the result against `reference_data/` (e.g. `python validate_constellations.py` for constellation order) before committing refreshed files. See `reference_data/README.md`.
