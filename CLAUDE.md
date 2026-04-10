# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Inventory Kamera is a Windows Forms (.NET Framework 4.7.2) desktop application that scans Genshin Impact inventory data using OCR (Optical Character Recognition). It captures screenshots of the game window and uses Tesseract OCR with custom trained data to extract information about characters, weapons, artifacts, and materials. The scanned data is exported in GOOD (Genshin Open Object Description) format, a JSON-based standard compatible with popular Genshin Impact optimizer tools.

## Build Commands

### Building the Project

**IMPORTANT:** This project requires Rider's MSBuild (version 18.4.0+) to compile correctly. The codebase uses C# 7.3 features (string interpolation with `$"..."`) that the standard .NET Framework 4.0 MSBuild does not support. Rider's MSBuild includes the Roslyn compiler which handles these features.

```bash
# Build in Debug configuration (use dash switches to avoid Git bash path translation)
"L:/Programs/JetBrains/Rider/tools/MSBuild/Current/Bin/amd64/MSBuild.exe" "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.sln" -p:Configuration=Debug -nologo -v:minimal

# Build in Release configuration
"L:/Programs/JetBrains/Rider/tools/MSBuild/Current/Bin/amd64/MSBuild.exe" "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.sln" -p:Configuration=Release -nologo -v:minimal

# Build for specific platform (x64, x86, or AnyCPU)
"L:/Programs/JetBrains/Rider/tools/MSBuild/Current/Bin/amd64/MSBuild.exe" "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.sln" -p:Configuration=Release -p:Platform=x64 -nologo -v:minimal
```

**Note:** The build will show `error MSB3202` about missing `InventoryKameraWPF.csproj` but this is expected - the main `InventoryKamera.exe` compiles successfully.

### Running the Application
```bash
# Run from Debug build
./InventoryKamera/bin/Debug/InventoryKamera.exe

# Run from Release build
./InventoryKamera/bin/Release/InventoryKamera.exe
```

### Restoring NuGet Packages
```bash
nuget restore InventoryKamera.sln
```

## Architecture Overview

### Core Components

**Data Flow Pipeline:**
1. `Navigation` - Captures game window and simulates keyboard/mouse input
2. `Scraper` classes - Extract regions from screenshots and queue them for OCR
3. `ImageProcessorWorker` threads - Process OCR queue using Tesseract engines
4. `InventoryKamera` class - Aggregates results and manages worker threads
5. `GOOD` class - Exports data in GOOD JSON format

### Key Directories

- **`InventoryKamera/data/`** - Core data structures and export logic
  - `InventoryKamera.cs` - Main orchestration class, manages multi-threaded OCR processing
  - `DatabaseManager.cs` - Loads/updates reference data from local JSON files and GitHub
  - `GOOD.cs` - Exports scanned data to GOOD JSON format
  - `Inventory.cs` - Container for weapons, artifacts, and materials
  - `OCRImageCollection.cs` - Wrapper for queuing images to worker threads

- **`InventoryKamera/game/`** - Game data models and navigation
  - `Navigation.cs` - Window capture, input simulation, aspect ratio handling
  - `Character.cs`, `Weapon.cs`, `Artifact.cs` - Game object models with GOOD serialization

- **`InventoryKamera/scraping/`** - OCR scanning implementations
  - `Scraper.cs` - Base OCR functionality, manages Tesseract engine pool
  - `WeaponScraper.cs`, `ArtifactScraper.cs`, `CharacterScraper.cs`, `MaterialScraper.cs` - Specialized scanners for each inventory type

- **`InventoryKamera/ui/`** - Windows Forms UI
  - `MainForm.cs` - Main application window, scan controls, settings
  - `UserInterface.cs` - Static helper for thread-safe UI updates from worker threads

### Multi-Threading Architecture

The application uses a producer-consumer pattern:
- **Main thread (UI thread):** Navigation and screenshot capture
- **Scraper threads:** Capture screenshots, crop regions, enqueue to `workerQueue`
- **Image processor workers (2-3 threads):** Dequeue items, run OCR, validate and store results

Worker threads are spawned in `InventoryKamera.GatherData()` and process items from the shared `Queue<OCRImageCollection>`. The queue is thread-safe and workers use `TryDequeue()` to retrieve work items. A special "END" message signals workers to terminate after weapon/artifact scanning completes.

### OCR Engine Pool

`Scraper.cs` maintains a `ConcurrentBag<TesseractEngine>` with 8 pre-initialized engines to avoid initialization overhead. Workers borrow an engine, perform OCR, and return it to the pool. Custom trained data files in `tessdata/` improve accuracy for Genshin Impact's UI font.

### Reference Data

The `inventorylists/` directory contains JSON files mapping item names to GOOD format identifiers:
- `characters.json` - Character data with constellation order and weapon type
- `weapons.json`, `artifacts.json` - Item name mappings
- `materials.json`, `devmaterials.json`, `allmaterials.json` - Material name mappings

These files are loaded at startup and can be auto-updated from Dimbreath's GenshinData GitHub repository via `DatabaseManager`.

### Game Integration

The scanner requires:
- Genshin Impact running in 16:9 or 16:10 resolution (windowed or fullscreen)
- Game language set to English
- Paimon menu open before starting scan

`Navigation.Initialize()` finds the game process (GenshinImpact.exe or YuanShen.exe), captures window dimensions, and verifies aspect ratio. The `InputSimulator` library sends keyboard inputs to navigate menus while `Graphics.CopyFromScreen()` captures specific regions.

### Validation and Error Logging

Each scanned item is validated (e.g., `Weapon.IsValid()` checks name, rarity, level, refinement). Invalid items trigger detailed logging to `./logging/weapons/`, `./logging/artifacts/`, etc., with cropped images and metadata for debugging. Users can enable "Log All Screenshots" to capture every scan attempt.

## Important Development Notes

### Resolution and Aspect Ratio Handling
All region coordinates in scrapers are calculated as ratios of the game window dimensions (see `Navigation.CaptureWindow()` and region calculations in scraper classes). When adding new regions, always use proportional coordinates based on a reference resolution (typically 1280x720 or 1920x1080).

### Traveler Character Handling
The Traveler character's name is user-specific. `CharacterScraper.ScanMainCharacterName()` OCRs the name from the character screen, then `Scraper.AddTravelerToCharacterList()` creates a character entry by cloning the generic "traveler" template from `characters.json`.

### Thread Safety
UI updates from worker threads must use `UserInterface` static methods, which internally use `Control.BeginInvoke()`. Never call WinForms controls directly from worker threads.

### Tesseract Language Files
The custom trained data files (`genshin_best_eng.traineddata`, `genshin_fast_09_04_21.traineddata`) are critical for accuracy. These files are copied to the output directory during build (see `.csproj` `<None>` elements). If OCR accuracy degrades, check that these files are present in the `tessdata/` directory.

## Common Workflows

### Adding Support for a New Character
1. Update `inventorylists/characters.json` with character data (name, constellation order, weapon type)
2. The scanner should automatically detect the character during the next scan
3. Test by scanning the character screen with the new character

### Debugging OCR Issues
1. Enable "Log All Screenshots" in the UI
2. Run a scan to populate `./logging/` with cropped images
3. Examine the saved images to identify problematic regions
4. Adjust region coordinates in the relevant scraper class
5. Consider updating Tesseract trained data if text recognition is consistently failing

### Updating Reference Data
The "Update Lookup Tables" option in the UI runs `DatabaseManager.UpdateAll()`, which:
1. Fetches latest data from Dimbreath's GenshinData repository
2. Parses JSON to extract item names and metadata
3. Converts to GOOD format and saves to `inventorylists/`
4. Should be run after each major Genshin Impact version update