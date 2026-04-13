# Phase 1.5: Core Library Extraction & Modernization

**Status:** Planning
**Target Completion:** Before Phase 2 (Avalonia UI)
**Goal:** Extract business logic from WinForms, prepare for cross-platform support

---

## Overview

Phase 1.5 bridges the gap between the current WinForms implementation and the future Avalonia cross-platform UI. The primary goal is to extract core scanning logic into a platform-agnostic library while maintaining the existing WinForms UI as a working reference implementation.

This phase focuses on **technical debt reduction** and **architectural cleanup** without introducing new user-facing features.

---

## Motivation

### Current Pain Points

1. **Platform Lock-in:** System.Drawing.Common (Windows-only, deprecated for cross-platform)
2. **Tight Coupling:** OCR logic intertwined with WinForms UI thread
3. **Hard-coded Coordinates:** Scan regions defined in code, difficult to support new resolutions
4. **Tesseract Issues:** Multithreading deadlocks with current package (5.2.0)
5. **Testing Difficulty:** Business logic can't be unit tested without UI

### Benefits of Phase 1.5

- ✅ Core logic testable without UI
- ✅ Cross-platform image processing ready (Windows, macOS, Linux)
- ✅ Scan profiles externalized (easier to support Steam Deck, 4K, etc.)
- ✅ Cleaner separation of concerns (SOLID principles)
- ✅ Foundation for Phase 2 Avalonia UI
- ✅ Existing WinForms UI continues working during migration

---

## Architecture Goals

### Target Structure

```
InventoryKamera.sln
├── InventoryKamera.Core/              # New: Platform-agnostic business logic (.NET 8)
│   ├── Models/                        # Character, Weapon, Artifact, Inventory, GOOD
│   ├── Scanning/                      # OCR logic, scrapers (interface-based)
│   ├── Data/                          # DatabaseManager, reference data loading
│   ├── Configuration/                 # ScanProfile, DataSourceConfig
│   └── Abstractions/                  # Interfaces for platform-specific services
│       ├── IScreenCapture.cs
│       ├── IInputSimulator.cs
│       ├── IImageProcessor.cs
│       └── IOcrEngine.cs
│
├── InventoryKamera.Infrastructure/    # New: Platform implementations (.NET 8)
│   ├── Windows/                       # WindowsScreenCapture, WindowsInput
│   ├── ImageSharp/                    # ImageSharpProcessor
│   └── Tesseract/                     # TesseractOcrEngine wrapper
│
├── InventoryKamera/                   # Existing: WinForms UI (.NET 8 - Windows target)
│   └── (References Core + Infrastructure, adapts to new interfaces)
│
└── InventoryKamera.Tests/             # New: Unit tests (.NET 8)
    ├── Scanning.Tests/
    ├── Data.Tests/
    └── TestData/                      # Sample screenshots, mock game data
```

---

## Phase 1.5 Tasks

### Task 1: Research & Prototyping

**Goal:** Validate technology choices before committing to migration.

#### 1.1 ImageSharp Evaluation
- [ ] Create prototype: Replace System.Drawing with ImageSharp in ArtifactScraper
- [ ] Benchmark performance: Image loading, cropping, color detection
- [ ] Test on Windows (ensure parity with current implementation)
- [ ] Validate: Can ImageSharp handle 1600x900 16:9 screenshots efficiently?

**Success Criteria:** ImageSharp prototype matches current accuracy within 1%

#### 1.2 Tesseract Package Evaluation ⚠️ CRITICAL

**Primary candidate:** `TesseractOCR 5.5.2` (more actively maintained fork with better async support)

**Validation checklist (Week 1 priority):**
- [ ] Loads `genshin_best_eng.traineddata` without errors
- [ ] OCR accuracy matches `Tesseract 5.2.0` baseline (within 1%)
- [ ] Multithreading stability: 8 engine pool, 2-3 workers, 100+ items
- [ ] No deadlocks during overnight stress test (500+ artifacts)
- [ ] Cross-platform: Verify on Windows first, macOS/Linux in Phase 2
- [ ] API compatibility: Document migration path from 5.2.0 to 5.5.2

**Fallback options if TesseractOCR 5.5.2 fails:**
- **Option B:** Stay with `Tesseract 5.2.0` but serialize OCR calls (lock/semaphore)
- **Option C:** Try `Tesseract.Net.SDK` (unified wrapper)
- **Option D:** Hybrid approach - use 5.2.0 single-threaded

**Success Criteria:** Eliminates 30-second timeout issues, maintains or improves accuracy

**Risk:** Highest-risk item in Phase 1.5. If validation fails, may impact timeline.

#### 1.3 Dependency Injection Setup
- [ ] Prototype `Microsoft.Extensions.DependencyInjection` in console app
- [ ] Test service registration for IScreenCapture, IOcrEngine
- [ ] Validate DI works with WinForms (adapter pattern may be needed)

---

### Task 2: External Configuration

**Goal:** Move hard-coded data to JSON configuration files.

#### 2.1 Create ScanProfile.json

Externalize all scan region coordinates:

```json
{
  "version": "1.0",
  "profiles": {
    "16:9": {
      "name": "16:9 Standard (1600x900, 1920x1080, etc.)",
      "aspectRatio": 1.7778,
      "artifacts": {
        "card": { "x": 0.7250, "y": 0.1556, "width": 0.2635, "height": 0.7778 },
        "name": { "x": 0.0911, "y": 0.0614, "width": 0.6719, "height": 0.0965 },
        "substats": { "x": 0.0911, "y": 0.4216, "width": 0.8097, "height": 0.1841 },
        "sanctifyIndicator": { "x": 0.40, "y": 0.3333, "width": 0.20, "height": 0.0526 },
        "sanctifyShift": 0.0520
      },
      "weapons": {
        "card": { "x": 0.7250, "y": 0.1556, "width": 0.2635, "height": 0.7778 },
        "name": { "x": 0.0911, "y": 0.0614, "width": 0.6719, "height": 0.0965 },
        "refinement": { "x": 0.061, "y": 0.421, "width": 0.065, "height": 0.033 }
      },
      "characters": { /* ... */ },
      "navigation": {
        "weaponTab": {
          "x": 0.300, "y": 0.049,
          "width": 0.080, "height": 0.040,
          "description": "Weapon inventory tab"
        },
        "artifactTab": {
          "x": 0.350, "y": 0.043,
          "width": 0.080, "height": 0.040,
          "description": "Artifact inventory tab"
        },
        "materialTab": {
          "x": 0.400, "y": 0.043,
          "width": 0.080, "height": 0.040,
          "description": "Material inventory tab"
        },
        "sortDropdown": {
          "x": 0.180, "y": 0.944,
          "width": 0.100, "height": 0.050,
          "description": "Sort order dropdown"
        }
      }
    },
    "16:10": { /* ... */ },
    "steam-deck": {
      "name": "Steam Deck (1280x800)",
      "aspectRatio": 1.6,
      "inheritsFrom": "16:10"
    }
  }
}
```

**Benefits:**
- Users can create custom profiles for unsupported resolutions (ultrawide, 4K, etc.)
- Easier to debug coordinate issues (edit JSON, no recompile)
- Community can maintain resolution profiles independently
- Trial-and-error workflow: Edit JSON → Scan → Check screenshots in `./logging/` → Refine

**User Workflow for Custom Resolutions:**
1. Enable "Log All Screenshots" in UI
2. Run a scan with default profile (likely misaligned)
3. Open `./logging/weapons/weapon0/refinement/refinement.png` (or artifact screenshots)
4. See what's actually being captured
5. Edit `ScanProfile.json` coordinates in text editor (Notepad++, VS Code, etc.)
6. Rerun scan and verify screenshots improve
7. Iterate until regions capture correctly

**Explicitly NOT in Phase 1.5:**
- ❌ Visual region configuration tools
- ❌ GUI editor for profiles
- ❌ CLI utilities for region testing

**Rationale:** Phase 2 will have full visual configuration tool integrated with Avalonia UI. Building intermediate WinForms/CLI tools would be throwaway work. Manual JSON editing is sufficient to unblock users and validate the profile system design.

#### 2.2 ScanProfile Loader
- [ ] Create `ScanProfileManager` class
- [ ] Load profiles from `ScanProfile.json` at startup
- [ ] Detect game resolution and select appropriate profile
- [ ] Fallback to closest aspect ratio if exact match not found
- [ ] Validate profile schema on load
- [ ] Log which profile was selected (help users verify correct profile loaded)

#### 2.3 Navigation Region Logging

**Goal:** Extend navigation to use box regions instead of blind coordinate clicks, with visual feedback.

**Current Problem:**
Navigation methods use hardcoded point coordinates:
```csharp
int buttonX = (int)(385 / 1280.0 * GetWidth());
int buttonY = (int)(35  / 720.0 * GetHeight());
SetCursor(buttonX, buttonY);
Click();
```

This fails on ultrawide/custom resolutions where menu buttons are in different positions.

**Solution:**
Define navigation regions as **boxes** (like OCR regions), click the **center** of the box:

```csharp
public static void SelectWeaponInventory()
{
    var region = profile.Navigation.WeaponTab;

    // Calculate center of box
    int centerX = (int)((region.X + region.Width / 2.0) * GetWidth());
    int centerY = (int)((region.Y + region.Height / 2.0) * GetHeight());

    // Capture box region before clicking (for visual feedback)
    if (Properties.Settings.Default.LogScreenshots)
    {
        CaptureNavigationRegion(region, "weaponTab");
    }

    SetCursor(centerX, centerY);
    Click();
}
```

**Visual Feedback:**
Save navigation box screenshots to `./logging/menus/`:
```
./logging/menus/
├── weaponTab.png           ← User sees: "Did it capture the weapon tab button?"
├── artifactTab.png         ← Shows what box was around artifact tab
├── materialTab.png
├── sortDropdown.png
└── inventoryButton.png
```

**User Workflow (Same as OCR regions):**
1. Enable "Log All Screenshots"
2. Run scan → Navigation regions saved to `./logging/menus/`
3. Open PNGs: "Did the box capture the button?"
4. Edit `ScanProfile.json` navigation box coordinates
5. Rerun scan → Verify PNGs show correctly positioned boxes
6. Iterate until clicks hit the right targets

**Benefits:**
- ✅ **Visual confirmation** - See what was clicked, not blind trust
- ✅ **Margin for error** - 80px box around 50px button, click center
- ✅ **Same workflow** - Edit JSON → Check PNGs → Refine (like OCR regions)
- ✅ **Ultrawide friendly** - Users adjust box position for their layout
- ✅ **No throwaway code** - This is visual feedback logging, not tooling
- ✅ **Phase 2 ready** - Visual editor can show/edit these boxes

**Implementation Tasks:**
- [ ] Update Navigation.cs methods to read from ScanProfile.json
- [ ] Implement CaptureNavigationRegion() helper
- [ ] Modify all navigation methods (SelectWeaponInventory, SelectArtifactInventory, etc.)
- [ ] Add navigation region capture to logging directory structure
- [ ] Document navigation region coordinate system in comments

**Navigation Regions to Externalize:**
- Inventory tabs: Weapon, Artifact, Material, Character Development
- Paimon menu buttons: Inventory, Character, Map, etc.
- Sort dropdown: Location and click position
- Confirmation buttons: OK, Cancel, etc.
- Item grid: Starting position for first item

---

### Task 3: Core Library Extraction

**Goal:** Extract business logic from WinForms into .NET 8 class library.

#### 3.1 Create InventoryKamera.Core Project
- [ ] New .NET 8 class library
- [ ] Define abstractions (interfaces) for platform-specific services
- [ ] No dependencies on System.Drawing, System.Windows.Forms, or Windows-specific APIs

#### 3.2 Extract Models (Low Risk)
**Move without modification:**
- `Character.cs`, `Weapon.cs`, `Artifact.cs`, `Material.cs`
- `Inventory.cs`, `GOOD.cs`
- `OCRImageCollection.cs`

**Refactor if needed:**
- Remove WinForms-specific JSON converters (Newtonsoft.Json → System.Text.Json?)

#### 3.3 Extract Data Layer (Medium Risk)
**Move with minimal changes:**
- `DatabaseManager.cs` - No UI dependencies, pure HTTP + JSON parsing
- `GenshinProcessor.cs` - Minor: Replace Bitmap with IImage abstraction

**Challenges:**
- `GenshinProcessor.CopyBitmap()` uses System.Drawing.Bitmap
- **Solution:** Define `IImageProcessor` interface, implement in Infrastructure layer

#### 3.4 Extract Scanning Logic (High Risk)
**Complex migration:**
- `Scraper.cs` (base class)
- `ArtifactScraper.cs`, `WeaponScraper.cs`, `CharacterScraper.cs`, `MaterialScraper.cs`

**Challenges:**
1. Tesseract engine pool management
2. Bitmap → IImage abstraction
3. Region calculations (need ScanProfile loaded)
4. Thread-safe UI updates (need callback/event pattern)

**Strategy:**
```csharp
// Before (WinForms coupled):
var card = Navigation.GetItemCard();
using (var page = engine.Process(card)) { /* ... */ }

// After (abstracted):
var card = await _screenCapture.CaptureRegionAsync(profile.Artifacts.Card);
using (var result = await _ocrEngine.RecognizeAsync(card)) { /* ... */ }
```

#### 3.5 Abstract Platform Services

**Define interfaces in Core:**

```csharp
public interface IScreenCapture
{
    Task<IImage> CaptureRegionAsync(Rectangle region);
    Task<IImage> CaptureFullScreenAsync();
    Size GetGameWindowSize();
}

public interface IInputSimulator
{
    void SendKey(KeyCode key);
    void SendMouseClick(int x, int y);
    void ScrollDown();
}

public interface IImageProcessor
{
    IImage Crop(IImage source, Rectangle region);
    Color GetPixel(IImage image, int x, int y);
    IImage ApplyGrayscale(IImage image);
    IImage ApplyContrast(IImage image, double factor);
}

public interface IOcrEngine
{
    Task<OcrResult> RecognizeAsync(IImage image, OcrOptions options = null);
}

public interface IImage : IDisposable
{
    int Width { get; }
    int Height { get; }

    // Hot path: Direct pixel access (sanctified detection, color checks)
    Color GetPixel(int x, int y);

    // Cold path: For OCR engine (avoid allocations in hot path)
    byte[] GetBytes();
    Stream GetStream(); // Alternative for Tesseract
}
```

**Implement in Infrastructure layer:**
- `WindowsScreenCapture` - Uses Graphics.CopyFromScreen for now
- `ImageSharpProcessor` - Wraps ImageSharp operations
- `TesseractOcrEngine` - Wraps Tesseract (whichever package we choose)
- `ImageSharpImage` - Implements IImage using Image<Rgba32>

---

### Task 4: Infrastructure Layer

**Goal:** Platform-specific implementations of Core abstractions.

#### 4.1 Windows Implementation
- [ ] `WindowsScreenCapture` - Graphics.CopyFromScreen wrapper
- [ ] `WindowsInputSimulator` - Uses WindowsInput library (existing)
- [ ] Can reuse existing Navigation.cs logic initially

#### 4.2 ImageSharp Implementation
- [ ] `ImageSharpProcessor` - Implements IImageProcessor
- [ ] `ImageSharpImage` - Wraps Image<Rgba32>, implements IImage
- [ ] Handle color space conversions (Rgba32 ↔ Bitmap for WinForms UI preview)

#### 4.3 Tesseract Wrapper
- [ ] `TesseractOcrEngine` - Implements IOcrEngine
- [ ] Engine pool management (move from Scraper.cs)
- [ ] Thread-safe engine borrowing/returning
- [ ] Handle native library loading (libtesseract.so/.dylib/.dll)

---

### Task 5: Adapt WinForms UI

**Goal:** Update existing WinForms app to use Core library (proof of concept).

#### 5.1 Migrate WinForms to .NET 8
- [ ] Update project file to `<TargetFramework>net8.0-windows</TargetFramework>`
- [ ] Add `<UseWindowsForms>true</UseWindowsForms>`
- [ ] Test WinForms compatibility (.NET 8 supports WinForms on Windows)
- [ ] Add project reference to InventoryKamera.Core
- [ ] Add project reference to InventoryKamera.Infrastructure

**Rationale:** Avoid multi-targeting friction between .NET Framework 4.7.2 and .NET 8. Microsoft officially supports WinForms on .NET 8 with Windows-specific target. This eliminates API compatibility issues and allows use of modern C# features throughout.

#### 5.2 Dependency Injection Adapter
WinForms doesn't have built-in DI. Create adapter:

```csharp
public static class ServiceProviderFactory
{
    public static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        // Register Core services
        services.AddSingleton<DatabaseManager>();
        services.AddSingleton<ScanProfileManager>();

        // Register Infrastructure services
        services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
        services.AddSingleton<IInputSimulator, WindowsInputSimulator>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IOcrEngine, TesseractOcrEngine>();

        // Register scrapers (now using interfaces)
        services.AddTransient<IArtifactScraper, ArtifactScraper>();
        services.AddTransient<IWeaponScraper, WeaponScraper>();

        return services.BuildServiceProvider();
    }
}
```

#### 5.3 Update MainForm
- [ ] Initialize ServiceProvider in Form_Load
- [ ] Replace direct instantiation with DI
- [ ] Keep UI update logic (UserInterface static class) for now
- [ ] Test: Full scan should work identically to before

---

### Task 6: Error Handling & Cancellation

**Goal:** Robust error handling and graceful cancellation throughout scanning pipeline.

#### 6.1 CancellationToken Propagation
- [ ] Add `CancellationToken` parameter to all async methods
- [ ] Thread CancellationToken through entire pipeline:
  - `ScanOrchestrator.ScanAllAsync(options, progress, cancellationToken)`
  - `IArtifactScraper.ScanArtifactsAsync(cancellationToken)`
  - `IOcrEngine.RecognizeAsync(image, options, cancellationToken)`
- [ ] Respect cancellation in tight loops (after each item)
- [ ] Clean up resources (dispose images, return OCR engines to pool) on cancellation

**Example:**
```csharp
public async Task<Inventory> ScanAllAsync(
    ScanOptions options,
    IProgress<ScanProgress> progress,
    CancellationToken cancellationToken = default)
{
    foreach (var category in options.Categories)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ScanCategoryAsync(category, progress, cancellationToken);
    }
}
```

#### 6.2 Error Recovery Strategies
- [ ] **OCR Timeout:** Skip item, log error, continue scan (existing behavior, keep)
- [ ] **Game Window Lost:** Pause scan, prompt user to restore game, resume when ready
- [ ] **Navigation Failure:** Retry 3 times with exponential backoff, then skip item
- [ ] **Validation Failure:** Log to `./logging/`, save debug screenshots, continue scan
- [ ] **Critical Errors:** Save partial results, allow user to resume later

#### 6.3 Progress Reporting with Errors
- [ ] Extend `ScanProgress` to include error information:
```csharp
public class ScanProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string CurrentItem { get; set; }
    public ScanStatus Status { get; set; } // Scanning, Paused, Error, Complete
    public List<ScanError> Errors { get; set; } // Accumulated errors
}

public class ScanError
{
    public int ItemId { get; set; }
    public string ItemName { get; set; }
    public string ErrorMessage { get; set; }
    public ScanErrorType Type { get; set; } // Timeout, Validation, Navigation, OCR
}
```

#### 6.4 Graceful Shutdown
- [ ] On cancellation: Finish current OCR task, don't start new items
- [ ] Dispose all borrowed OCR engines
- [ ] Save partial scan results (allow user to resume or export what was scanned)
- [ ] Return to Idle state

**See also:** `docs/plantuml/12-error-handling-flow.puml` for detailed flow diagram

---

### Task 7: Testing Infrastructure

**Goal:** Enable unit testing of business logic.

#### 7.1 Create Test Project
- [ ] New .NET 8 xUnit project
- [ ] Reference InventoryKamera.Core (not Infrastructure)
- [ ] Create mock implementations of abstractions

#### 7.2 Mock Implementations
```csharp
public class MockScreenCapture : IScreenCapture
{
    private readonly string _testDataPath;

    public Task<IImage> CaptureRegionAsync(Rectangle region)
    {
        // Load pre-captured test image from disk
        var image = Image.Load<Rgba32>($"{_testDataPath}/artifact_card.png");
        return Task.FromResult<IImage>(new ImageSharpImage(image));
    }
}
```

#### 7.3 Test Cases
- [ ] **DatabaseManager:** Test parsing GenshinData JSON
- [ ] **ArtifactScraper:** Test sanctified detection with mock images
- [ ] **ScanProfileManager:** Test profile loading and aspect ratio matching
- [ ] **GOOD Export:** Test JSON serialization matches expected format

---

## Migration Strategy

### Incremental Approach (Recommended)

**Week 1-2: Research & Prototyping**
- ImageSharp proof of concept
- Tesseract package evaluation
- Decision: Which packages to use?

**Week 3-4: Configuration Externalization**
- Create ScanProfile.json
- Implement ScanProfileManager
- Test with existing WinForms app (load profiles, but still use old code)

**Week 5-6: Core Library Foundation**
- Create InventoryKamera.Core project
- Extract models (low risk)
- Define all abstractions (interfaces)

**Week 7-8: Infrastructure Implementations**
- Implement Windows services
- Implement ImageSharp processor
- Implement Tesseract wrapper

**Week 9-10: Scraper Migration**
- Refactor Scraper.cs to use abstractions
- Migrate ArtifactScraper (most complex)
- Migrate WeaponScraper, CharacterScraper, MaterialScraper
- Add CancellationToken support throughout

**Week 11-12: WinForms Adapter**
- Migrate WinForms to .NET 8 (net8.0-windows)
- Wire up DI in MainForm
- Test full scan workflow
- Compare output with pre-migration version (should be identical)

**Week 13-14: Error Handling & Testing**
- Implement error recovery strategies
- Add graceful cancellation support
- Write unit tests
- Create test data set (captured screenshots)

**Week 15-16: Polish & Documentation**
- Bug fixes, performance tuning
- Update architecture diagrams
- API documentation
- Migration guide for contributors

---

## Success Criteria

### Phase 1.5 Complete When:
- [ ] Core library has zero dependencies on System.Windows.Forms or System.Drawing
- [ ] All business logic uses abstractions (IScreenCapture, IOcrEngine, etc.)
- [ ] Scan regions loaded from ScanProfile.json (no hard-coded coordinates)
- [ ] WinForms migrated to .NET 8 (net8.0-windows target)
- [ ] WinForms UI works identically to before (same accuracy, same output)
- [ ] CancellationToken support throughout async pipeline
- [ ] Error recovery strategies implemented (timeout, validation, navigation failures)
- [ ] Unit tests cover core scanning logic with >70% code coverage
- [ ] Documentation updated (architecture diagrams, API docs, migration guide)

### Non-Goals for Phase 1.5:
- ❌ Cross-platform UI (that's Phase 2)
- ❌ macOS/Linux implementations (Windows only for now)
- ❌ New features (focus on refactoring only)
- ❌ Performance improvements (maintain current performance)

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| ImageSharp accuracy differs from System.Drawing | High | Medium | Extensive testing, side-by-side comparison, fallback to Bitmap conversion if needed |
| Tesseract package change breaks OCR | High | Medium | Evaluate packages in prototype, test with all trained data files |
| Performance regression | Medium | Low | Benchmark before/after, profile hot paths |
| WinForms DI adapter too complex | Low | Medium | Keep it simple, manual instantiation is fine |
| Breaking existing WinForms UI | High | Low | Keep old code in git, incremental migration with tests |

---

## Dependencies & Tools

### Required NuGet Packages (Core)
- `Newtonsoft.Json` - JSON parsing (decision: keep for GOOD format compatibility)
- `Microsoft.Extensions.DependencyInjection` - DI container
- `Microsoft.Extensions.Logging` - Logging abstraction (or keep NLog - TBD)

### Required NuGet Packages (Infrastructure)
- `SixLabors.ImageSharp` - Cross-platform image processing
- `TesseractOCR 5.5.2` - OCR engine (pending Week 1 validation)
- `WindowsInput` - Windows input simulation (existing)

### Development Tools
- Visual Studio 2022 / Rider 2024.3+
- .NET 8 SDK
- PlantUML / Mermaid - Architecture diagrams
- BenchmarkDotNet - Performance testing

---

## Architectural Decisions

### Decided

1. **JSON Library: Newtonsoft.Json** ✅
   - **Decision:** Keep Newtonsoft.Json
   - **Rationale:** GOOD format has polymorphic types and custom converters that work reliably with Newtonsoft.Json. System.Text.Json has subtle behavioral differences that could break GOOD export compatibility. Not worth the risk during structural refactor.
   - **Future:** Can migrate to System.Text.Json in a later phase if needed

2. **WinForms Target: .NET 8** ✅
   - **Decision:** Migrate WinForms from .NET Framework 4.7.2 to .NET 8 (Windows target)
   - **Rationale:** Eliminates multi-targeting friction between Core (.NET 8) and WinForms. Microsoft officially supports WinForms on .NET 8 with `net8.0-windows` target. Allows use of modern APIs throughout.
   - **Timeline:** Include in Week 11-12 (WinForms Adapter task)

3. **Tesseract Package: TesseractOCR 5.5.2** ⚠️
   - **Decision:** Migrate to TesseractOCR 5.5.2 (pending Week 1 validation)
   - **Rationale:** More actively maintained fork with better async support. Should eliminate known deadlock issues in Tesseract 5.2.0.
   - **Fallback:** If validation fails, serialize OCR calls or stay with 5.2.0
   - **Risk:** High - requires thorough validation with custom trained data

### Open Questions

1. **Logging:** Keep NLog or switch to Microsoft.Extensions.Logging?
   - **Pro NLog:** Existing, powerful, already configured
   - **Pro MEL:** Standard, works with DI, cross-platform
   - **Decision needed by:** Week 5 (Core Library Foundation)

2. **Async/Await:** Add async throughout, or keep synchronous?
   - Current code is mostly synchronous with background threads
   - Async would be cleaner but requires more refactoring
   - **Decision needed by:** Week 9 (Scraper Migration)

---

## Next Steps

1. **Review this plan** with stakeholders (you!)
2. **Create architecture diagrams** (PlantUML)
3. **Set up prototype branch** for ImageSharp + Tesseract testing
4. **Start Week 1-2 research** when ready to begin implementation

---

**Last Updated:** 2026-04-07
**Author:** Phase 1.5 Planning Team
**Status:** Draft - Awaiting Review
