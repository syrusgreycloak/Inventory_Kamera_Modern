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
├── InventoryKamera/                   # Existing: WinForms UI (.NET Framework 4.7.2)
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

#### 1.2 Tesseract Package Evaluation
Test three options:
- [ ] **Option A:** Stay with `Tesseract 5.2.0` (known multithreading issues)
- [ ] **Option B:** Migrate to `TesseractOCR 5.5.2` (different API, needs testing)
- [ ] **Option C:** Try `Tesseract.Net.SDK` (unified wrapper)

**Test Plan:**
1. OCR accuracy with custom trained data files (`genshin_best_eng.traineddata`)
2. Multithreading stability (8 engine pool, 2-3 worker threads)
3. Cross-platform compatibility (Windows first, macOS/Linux later)

**Success Criteria:** New package eliminates 30-second timeout issues, maintains accuracy

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
      "weapons": { /* ... */ },
      "characters": { /* ... */ }
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
- Users can create custom profiles for unsupported resolutions
- Easier to debug coordinate issues (edit JSON, no recompile)
- Community can maintain resolution profiles independently

#### 2.2 ScanProfile Loader
- [ ] Create `ScanProfileManager` class
- [ ] Load profiles from `ScanProfile.json` at startup
- [ ] Detect game resolution and select appropriate profile
- [ ] Fallback to closest aspect ratio if exact match not found
- [ ] Validate profile schema on load

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
    byte[] GetBytes(); // For passing to OCR
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

#### 5.1 Reference New Libraries
- [ ] Add project reference to InventoryKamera.Core
- [ ] Add project reference to InventoryKamera.Infrastructure
- [ ] Keep .NET Framework 4.7.2 target (for now)

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

### Task 6: Testing Infrastructure

**Goal:** Enable unit testing of business logic.

#### 6.1 Create Test Project
- [ ] New .NET 8 xUnit project
- [ ] Reference InventoryKamera.Core (not Infrastructure)
- [ ] Create mock implementations of abstractions

#### 6.2 Mock Implementations
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

#### 6.3 Test Cases
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

**Week 11-12: WinForms Adapter**
- Wire up DI in MainForm
- Test full scan workflow
- Compare output with pre-migration version (should be identical)

**Week 13-14: Testing & Polish**
- Write unit tests
- Create test data set (captured screenshots)
- Bug fixes, performance tuning

---

## Success Criteria

### Phase 1.5 Complete When:
- [ ] Core library has zero dependencies on System.Windows.Forms or System.Drawing
- [ ] All business logic uses abstractions (IScreenCapture, IOcrEngine, etc.)
- [ ] Scan regions loaded from ScanProfile.json (no hard-coded coordinates)
- [ ] WinForms UI works identically to before (same accuracy, same output)
- [ ] Unit tests cover core scanning logic
- [ ] Documentation updated (architecture diagrams, API docs)

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
- `System.Text.Json` - JSON parsing (replace Newtonsoft.Json?)
- `Microsoft.Extensions.DependencyInjection` - DI container
- `Microsoft.Extensions.Logging` - Logging abstraction (replace NLog?)

### Required NuGet Packages (Infrastructure)
- `SixLabors.ImageSharp` - Cross-platform image processing
- `Tesseract` / `TesseractOCR` / `Tesseract.Net.SDK` - OCR (decision needed)
- `WindowsInput` - Windows input simulation (existing)

### Development Tools
- Visual Studio 2022 / Rider 2024.3+
- .NET 8 SDK
- PlantUML / Mermaid - Architecture diagrams
- BenchmarkDotNet - Performance testing

---

## Open Questions

1. **JSON Library:** Stick with Newtonsoft.Json or migrate to System.Text.Json?
   - **Pro System.Text.Json:** Modern, faster, built-in
   - **Pro Newtonsoft.Json:** Existing, compatible with GOOD format generators

2. **Logging:** Keep NLog or switch to Microsoft.Extensions.Logging?
   - **Pro NLog:** Existing, powerful, already configured
   - **Pro MEL:** Standard, works with DI, cross-platform

3. **Tesseract Package:** Which one?
   - Needs prototype testing to decide

4. **Async/Await:** Add async throughout, or keep synchronous?
   - Current code is mostly synchronous with background threads
   - Async would be cleaner but requires more refactoring

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
