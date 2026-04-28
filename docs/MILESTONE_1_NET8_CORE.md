# Milestone 1: .NET 8 + Core Extraction

**Goal:** .NET 8 solution with a clean platform-agnostic Core library. WinForms still works as the UI throughout.

**Status:** COMPLETE (2026-04-27). All scans verified working without regressions. Post-completion fixes applied (same session): Traveler constellation crash (`ConstellationOrder: {}` stale cache + null-deref), scan speed regression (`IInputSimulator.Wait` bypassed the delay multiplier — added `SystemWaitMs`). Carry-overs to Milestone 2: ImageSharp migration (System.Drawing removal from Core), ScanProfile scraper wiring, Tesseract 5.5.2 upgrade.

---

## Overview

This milestone is the foundation for everything else. You cannot reference a .NET 8 Core library from a .NET Framework 4.7.2 project without multi-targeting pain. Migrate first, extract second.

The **Strangler Fig pattern** governs extraction: at every step the app compiles and scans correctly. No big-bang rewrites.

---

## Codebase Context

### Static Classes (Major Refactoring Challenge)

~40% of production code lives in three static classes that must be converted to instances before they can be dependency-injected:

| Class | File | Static Fields | Static Methods | Difficulty |
|-------|------|--------------|----------------|-----------|
| `Navigation` | `InventoryKamera/game/Navigation.cs` | ~15 | ~30 | Very Hard — every scraper calls it |
| `GenshinProcesor` | `InventoryKamera/scraping/GenshinProcesor.cs` | ~10 (incl. Tesseract pool) | ~50 | Very Hard — core OCR pipeline |
| `UserInterface` | `InventoryKamera/ui/UserInterface.cs` | ~40 (control refs) | ~20 | Hard — many callers |

**Strategy:** Wrap and Delegate pattern for each:
1. Create instance class implementing the target interface
2. Instance initially delegates all calls to the existing static methods
3. Gradually move logic from static → instance
4. Delete empty static class

This avoids a big-bang rewrite — every intermediate state compiles and runs.

### Dependencies to Replace

| Current | Problem | Replacement |
|---------|---------|-------------|
| `System.Drawing` (GDI+) | Windows-only | `SixLabors.ImageSharp` |
| `Accord.Imaging 3.8.0` | Abandoned 2018, built on System.Drawing | ImageSharp equivalents |
| `Tesseract 5.2.0` | Known threading deadlocks | `TesseractOCR 5.5.2` (pending validation — see Step 1.3) |

**Note:** Accord.Imaging must be replaced alongside System.Drawing — they are tightly coupled and both must go together.

---

## Step 1.1: .NET 8 Migration ✓ COMPLETE

**Files to modify:**
- `InventoryKamera/InventoryKamera.csproj` — convert to SDK-style, `net8.0-windows`
- `InventoryKamera/packages.config` — convert all entries to `PackageReference`
- `InventoryKamera/App.config` — review for removed .NET Framework sections
- `InventoryKamera/ui/main/MainForm.cs` — fix `Thread.Abort()` (required, not optional)

### InventoryKamera.csproj — SDK-style format

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <ApplicationIcon>InventoryKamera.ico</ApplicationIcon>
    <AssemblyName>InventoryKamera</AssemblyName>
    <RootNamespace>InventoryKamera</RootNamespace>
    <Platforms>x64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Tesseract" Version="5.2.0" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="NLog" Version="5.3.4" />
    <PackageReference Include="NHotkey.WindowsForms" Version="3.0.0" />
    <PackageReference Include="InputSimulator" Version="1.0.4" />
    <PackageReference Include="Octokit" Version="13.0.1" />
    <PackageReference Include="Accord.Imaging" Version="3.8.0" />
    <!-- Remove Accord.Imaging when GenshinProcesor is refactored in Step 1.2 -->
  </ItemGroup>

  <!-- tessdata, inventorylists, and other content copy rules carry over unchanged -->
</Project>
```

### Required fix: Thread.Abort()

`Thread.Abort()` throws `PlatformNotSupportedException` on .NET 8. In `MainForm.cs`, find the stop-scan code (around line 92) and replace:

```csharp
// BEFORE — broken on .NET 8
scannerThread.Abort();

// AFTER — cooperative cancellation
_cancellationTokenSource?.Cancel();
```

Add a `CancellationTokenSource` field to `MainForm`:
```csharp
private CancellationTokenSource _cancellationTokenSource;
```

Initialize it when starting a scan:
```csharp
_cancellationTokenSource = new CancellationTokenSource();
```

Pass the token into the scan pipeline and check `cancellationToken.IsCancellationRequested` in scraper loops. Full async/await conversion is **not** required in this step — cooperative cancellation is sufficient.

### Build verification

```
"L:/Programs/JetBrains/Rider/tools/MSBuild/Current/Bin/amd64/MSBuild.exe" "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.sln" -p:Configuration=Debug -nologo -v:minimal
```

Expected: `InventoryKamera.exe` built with zero errors. The missing `InventoryKameraWPF.csproj` warning (`MSB3202`) is expected and harmless.

Run the app and complete a scan. Export GOOD JSON. Verify artifact/weapon/character counts match a pre-migration baseline.

---

## Step 1.2: Create Core Library (Incremental Extraction)

### New project: InventoryKamera.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <!-- No windows suffix — this must be platform-agnostic -->
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>
</Project>
```

Extract in this order. **Verify build + scan after each group** before moving to the next.

### Group A: Models (trivial — no dependencies)

Move to `InventoryKamera.Core/Models/`:
- `Character.cs` — verify: no `System.Drawing` or `System.Windows.Forms` using statements
- `Weapon.cs`
- `Artifact.cs`
- `Material.cs`
- `Inventory.cs`
- `OCRImageCollection.cs`

Move to `InventoryKamera.Core/Export/`:
- `GOOD.cs` — pure Newtonsoft.Json serialization, no platform dependencies

Add project reference to `InventoryKamera.csproj`:
```xml
<ProjectReference Include="..\InventoryKamera.Core\InventoryKamera.Core.csproj" />
```

### Group B: Data layer (easy)

Move to `InventoryKamera.Core/Data/`:
- `DatabaseManager.cs` — already an instance class; uses `HttpClient` + Newtonsoft.Json only

### Group C: Abstractions (define interfaces in Core)

Create `InventoryKamera.Core/Abstractions/`:

```csharp
// IScreenCapture.cs
public interface IScreenCapture
{
    Bitmap CaptureRegion(Rectangle region);
    Bitmap CaptureWindow();
    Size GetWindowSize();
    Point GetWindowPosition();
}

// IInputSimulator.cs
public interface IInputSimulator
{
    void Click();
    void SetCursor(int x, int y);
    void KeyPress(VirtualKeyCode key);
    void ScrollDown(int amount);
}

// IImageProcessor.cs
public interface IImageProcessor
{
    Bitmap Crop(Bitmap source, Rectangle region);
    Color GetPixel(Bitmap image, int x, int y);
    Bitmap ApplyGrayscale(Bitmap image);
    Bitmap ApplyThreshold(Bitmap image, int threshold);
    Bitmap ApplyContrast(Bitmap image, double factor);
}

// IOcrEngine.cs
public interface IOcrEngine
{
    string Recognize(Bitmap image, PageSegMode mode = PageSegMode.SingleLine);
}

// IUserInterface.cs
public interface IUserInterface
{
    void SetWeaponMax(int count);
    void IncrementWeapon(int count);
    void SetArtifactMax(int count);
    void IncrementArtifact(int count);
    void SetCharacterMax(int count);
    void IncrementCharacter(int count);
    void SetMaterialMax(int count);
    void IncrementMaterial(int count);
    void UpdatePreview(Bitmap image);
    void AddLogMessage(string message);
    void SetStatus(string message);
}
```

Note: `Bitmap`, `Rectangle`, `Color`, `Size`, `Point` here refer to `System.Drawing` types initially. They will be replaced by `ImageSharp` types when System.Drawing is removed later.

### Group D: GenshinProcesor de-static (hard)

`GenshinProcesor.cs` is a ~1000-line static class containing:
- Tesseract engine pool (8 engines in a `ConcurrentBag`)
- Game data dictionaries (weapons, characters, artifacts, materials)
- 50+ image processing static methods

Split into three instance classes using Wrap and Delegate:

**`InventoryKamera.Core/Ocr/OcrEnginePool.cs`** — implements `IOcrEngine`, manages Tesseract engine pool

**`InventoryKamera.Core/ImageProcessing/ImageProcessor.cs`** — implements `IImageProcessor`, image filter methods

**`InventoryKamera.Core/Data/GameDataService.cs`** — holds the game data dictionaries loaded from `inventorylists/*.json`

Initial implementation: each new class's methods delegate to the existing static methods on `GenshinProcesor`. Remove static methods from `GenshinProcesor` one by one as they are moved into the new classes.

### Group E: Navigation de-static (hard)

`Navigation.cs` is a ~500-line static class with:
- Window detection (P/Invoke to `user32.dll`)
- Screen capture (`Graphics.CopyFromScreen`)
- Input simulation (`InputSimulator` library)
- Aspect ratio detection and coordinate helpers

Create `InventoryKamera.Infrastructure/Windows/`:

```csharp
// WindowsScreenCapture.cs
public class WindowsScreenCapture : IScreenCapture
{
    public Bitmap CaptureWindow()
    {
        return Navigation.CaptureWindow(); // delegate initially
    }

    public Bitmap CaptureRegion(Rectangle region)
    {
        return Navigation.CaptureWindow(region); // delegate initially
    }

    public Size GetWindowSize() => Navigation.GetSize();
    public Point GetWindowPosition() => Navigation.GetPosition();
}

// WindowsInputSimulator.cs
public class WindowsInputSimulator : IInputSimulator
{
    public void Click() => Navigation.Click();
    public void SetCursor(int x, int y) => Navigation.SetCursor(x, y);
    public void KeyPress(VirtualKeyCode key) => Navigation.KeyPress(key);
    public void ScrollDown(int amount) => Navigation.ScrollDown(amount);
}
```

Migrate methods from `Navigation` into the instance classes incrementally.

### Group F: Scrapers refactored (medium)

Update scraper constructors to accept injected interfaces instead of calling statics:

```csharp
// Before
public class ArtifactScraper
{
    public void Scan()
    {
        var card = Navigation.GetItemCard();        // static
        GenshinProcesor.SetContrast(60, ref card);  // static
    }
}

// After
public class ArtifactScraper
{
    private readonly IScreenCapture _screen;
    private readonly IOcrEngine _ocr;
    private readonly IImageProcessor _imageProcessor;

    public ArtifactScraper(IScreenCapture screen, IOcrEngine ocr, IImageProcessor imageProcessor)
    {
        _screen = screen;
        _ocr = ocr;
        _imageProcessor = imageProcessor;
    }
}
```

### Group G: UserInterface de-static (medium)

Create `InventoryKamera/UI/WinFormsUserInterface.cs`:

```csharp
public class WinFormsUserInterface : IUserInterface
{
    private readonly MainForm _form;

    public WinFormsUserInterface(MainForm form) => _form = form;

    public void SetWeaponMax(int count)
    {
        _form.BeginInvoke((MethodInvoker)delegate
        {
            UserInterface.SetWeapon_Max(count); // delegate to static initially
        });
    }

    public void IncrementWeapon(int count)
    {
        _form.BeginInvoke((MethodInvoker)delegate
        {
            UserInterface.IncrementWeapon(count);
        });
    }

    // ... implement all IUserInterface members similarly
}
```

### Step 1.2 Verification

After each group:
1. Build: zero errors
2. Run: full scan completes without errors
3. GOOD JSON output: artifact/weapon/character counts unchanged from pre-migration baseline

---

## Step 1.3: Tesseract Evaluation (Parallel Track)

**Goal:** Determine whether to migrate from `Tesseract 5.2.0` to `TesseractOCR 5.5.2`.

Create a throwaway console app (`TesseractEval/`) to validate:
- `genshin_best_eng.traineddata` loads without errors
- 8 concurrent engines can each process an image without deadlock (stress test with 500+ items)
- OCR output on 10 sample screenshots matches Tesseract 5.2.0 output within 1% accuracy

**If validation passes:** Update `PackageReference` to `TesseractOCR 5.5.2`, update engine pool initialization, run a full scan.

**If validation fails:** Keep `Tesseract 5.2.0`. Add `SemaphoreSlim(4, 4)` to limit concurrent OCR calls in the engine pool. Document the decision in QUICK_WINS.md.

---

## Step 1.4: ScanProfile.json (Parallel Track)

**Goal:** Externalize all hard-coded scan region coordinates so users can adjust them without recompiling.

**File to create:** `InventoryKamera/inventorylists/ScanProfile.json`

**Class to create:** `InventoryKamera.Core/Configuration/ScanProfileManager.cs`

Profile structure (excerpt — all region values are relative 0.0–1.0 ratios of the game window):

```json
{
  "version": "1.0",
  "profiles": {
    "16:9": {
      "name": "16:9 Standard (1600x900, 1920x1080, etc.)",
      "aspectRatio": 1.7778,
      "artifacts": {
        "card":     { "x": 0.7250, "y": 0.1556, "w": 0.2635, "h": 0.7778 },
        "name":     { "x": 0.0911, "y": 0.0614, "w": 0.6719, "h": 0.0965 },
        "substats": { "x": 0.0911, "y": 0.4216, "w": 0.8097, "h": 0.1841 },
        "mainStat": { "x": 0.0750, "y": 0.1850, "w": 0.2500, "h": 0.0450 },
        "level":    { "x": 0.0750, "y": 0.3400, "w": 0.1500, "h": 0.0350 },
        "rarity":   { "x": 0.0750, "y": 0.1300, "w": 0.4000, "h": 0.0400 }
      },
      "weapons": {
        "card":       { "x": 0.7250, "y": 0.1556, "w": 0.2635, "h": 0.7778 },
        "name":       { "x": 0.0911, "y": 0.0614, "w": 0.6719, "h": 0.0965 },
        "refinement": { "x": 0.0610, "y": 0.4210, "w": 0.0650, "h": 0.0330 }
      },
      "navigation": {
        "weaponTab":   { "x": 0.300, "y": 0.049, "w": 0.080, "h": 0.040 },
        "artifactTab": { "x": 0.350, "y": 0.043, "w": 0.080, "h": 0.040 },
        "materialTab": { "x": 0.400, "y": 0.043, "w": 0.080, "h": 0.040 }
      }
    },
    "16:10": {
      "name": "16:10 (1680x1050, 1920x1200, etc.)",
      "aspectRatio": 1.6000
    }
  }
}
```

`ScanProfileManager` loads this at startup, detects the game window aspect ratio, and returns the matching profile. Scrapers read regions from the profile instead of hard-coded constants.

**Scope limit:** No conditional rules engine. The sanctified artifact coordinate shift stays as a code `if` statement in `ArtifactScraper`.

---

## Milestone 1 Complete When

- [x] App builds targeting `net8.0-windows` with zero errors
- [x] `Thread.Abort()` replaced (cooperative cancellation via `volatile bool`; full `CancellationTokenSource` carry-over to M2)
- [x] `InventoryKamera.Core` project has no `System.Windows.Forms` references
- [ ] `InventoryKamera.Core` project has no `System.Drawing` references ← **CARRY-OVER to M2**: replace with ImageSharp alongside Accord.Imaging removal
- [x] Models, GOOD export, and DatabaseManager extracted to Core
- [x] Interfaces defined: `IScreenCapture`, `IOcrEngine`, `IImageProcessor`, `IInputSimulator`, `IUserInterface`
- [x] WinForms project references Core via `ProjectReference` and uses injected implementations
- [x] Full scan verified: all inventory types complete without early termination (2026-04-27)
- [ ] `ScanProfile.json` loaded and used for all region coordinates ← **CARRY-OVER to M2**: JSON and ScanProfileManager exist; scraper wiring deferred
- [ ] Tesseract 5.5.2 upgrade ← **CARRY-OVER to M2**: evaluation passed (build succeeds); actual upgrade deferred

---

*See `docs/archive/PHASE_1.5_PLAN.md` for the original detailed plan (superseded by this document).*
*Last updated: 2026-04-13*
