# Milestone 2 — Avalonia UI Implementation Plan

> **For agentic workers:** This plan is **chunked for dispatch**. Each chunk is a self-contained unit of work scoped to one fresh agent invocation (Sonnet or Haiku, noted per chunk). Chunks have explicit dependencies — execute them in dependency order. After each chunk, verify by building and running, then commit before dispatching the next.
>
> Each chunk lists files to touch, the architectural shape, verification steps, and a recommended model. When a chunk is dispatched, the receiving agent should expand it into TDD-style step-by-step work using `superpowers:executing-plans`. If a chunk seems too large mid-execution, the agent should ask for a sub-plan.

**Goal:** Replace the WinForms UI with a cross-platform Avalonia 11 + CommunityToolkit.Mvvm app sharing `InventoryKamera.Core`. WinForms is retired after parity. Windows-only scanning at first; macOS/Linux builds compile and show a platform warning.

**Architecture:** `Core` (already exists) holds models, abstractions, scan profile, GOOD export, and game data services. A new `Infrastructure` project holds Windows-specific implementations of the abstractions. The new `Avalonia` project hosts views, view models, and UI services and consumes both via DI. Strangler Fig: WinForms keeps working until Avalonia reaches parity.

**Tech Stack:** Avalonia 11.2.x, CommunityToolkit.Mvvm 8.3.x, Microsoft.Extensions.DependencyInjection 8.0.x, SixLabors.ImageSharp 3.1.x, TesseractOCR 5.5.2 (already migrated), xUnit (existing test project).

**Solution layout at the end of M2:**

```
InventoryKamera.sln
├── InventoryKamera.Core/             # net8.0 — exists
├── InventoryKamera.Infrastructure/   # net8.0-windows — extracted in chunk 2
├── InventoryKamera.Avalonia/         # net8.0 — new
└── InventoryKamera.Tests/            # net8.0 — exists
# (InventoryKamera/ WinForms — removed in chunk 12)
```

---

## Repository State Summary (as of 2026-04-29, branch `feat/avalonia-implementation`)

What is **already done** that the milestone doc lists as carry-over:
- ✅ TesseractOCR 5.5.2 — already in `InventoryKamera.csproj`
- ✅ ScanProfile scraper wiring — `IScanProfileService` is wired into all four scrapers
- ✅ Core abstractions: `IScreenCapture`, `IInputSimulator`, `IImageProcessor`, `IOcrEngine`, `IUserInterface`, `IGameDataService`
- ✅ Windows infrastructure impls live in `InventoryKamera/Infrastructure/` (still inside the WinForms project)
- ✅ Models, DatabaseManager, GOOD, GameDataService extracted to Core

What is **still pending** (drives this plan):
- ❌ ImageSharp migration — Core still imports `System.Drawing` types in interfaces (`IScreenCapture`, `IImageProcessor`, `IOcrEngine`, `IUserInterface`, `OCRImageCollection`, `Rect.cs`); `System.Drawing.Common` is in `InventoryKamera.Core.csproj`
- ❌ `Infrastructure` is not yet its own project — Windows impls are in `InventoryKamera/Infrastructure/` next to WinForms code
- ❌ No Avalonia project, no view models, no Avalonia user interface adapter
- ❌ `MainForm` still uses `Thread` + a non-volatile `bool running` for cancellation (M1 doc says cooperative cancellation moved to a `volatile bool`; full `CancellationTokenSource` is M2 work)
- ❌ No cross-platform settings store — `Properties.Settings.Default` is .NET Framework-style and not portable

---

## Chunk Dependency Graph

```
Chunk 1 (ImageSharp migration) ──┐
                                 ▼
Chunk 2 (Infrastructure + Avalonia scaffold) ──┐
                                               ▼
                       ┌────────── Chunk 3 (Settings service) ──────┐
                       ▼                                            ▼
              Chunk 4 (ScanViewModel) ── Chunk 6 (AvaloniaUserInterface)
                       │                                            │
                       ▼                                            │
              Chunk 5 (ScanView.axaml) ◀───────────────────────────┘
                       │
        ┌──────────────┼─────────────────┐
        ▼              ▼                 ▼
Chunk 7 (Settings)  Chunk 8 (Export+About)  Chunk 9 (Cross-platform)
        └──────────────┴─────────────────┘
                       ▼
              Chunk 10 (ViewModel tests)
                       ▼
        Chunk 11 (Deprecate WinForms) → Chunk 12 (Remove WinForms)
```

Chunks 7, 8, 9 can run in parallel after chunk 6 lands (independent files, independent VM trees).

---

## Conventions for Every Chunk

- **Branch:** `feat/avalonia-implementation` (already created). Do not branch off; commit directly so each chunk lands in sequence.
- **Build command (after each chunk):**
  ```
  "L:/Programs/JetBrains/Rider/tools/MSBuild/Current/Bin/amd64/MSBuild.exe" "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.sln" -p:Configuration=Debug -nologo -v:minimal
  ```
  Expected: 0 errors. The pre-existing `MSB3202` warning about `InventoryKameraWPF.csproj` is harmless and unrelated to this work.
- **Run-time verification:** Until chunk 5 ships, regression-test by running the **WinForms** app (`./InventoryKamera/bin/x64/Debug/InventoryKamera.exe`) and completing a full scan. After chunk 5, verify the **Avalonia** app (`./InventoryKamera.Avalonia/bin/Debug/net8.0/InventoryKamera.Avalonia.exe`).
- **Test command:**
  ```
  dotnet test "C:/Users/karlp/RiderProjects/Inventory_Kamera/InventoryKamera.Tests/InventoryKamera.Tests.csproj"
  ```
- **Commit cadence:** One commit per logical sub-step within a chunk (the agent should keep the WinForms app running at every commit boundary until chunk 11).
- **TDD:** Every behavioral change must be driven by a failing test where feasible. View XAML and DI registration are exempt; image conversion, ViewModel commands, settings persistence, and platform stubs are not.

---

# CHUNK 1 — ImageSharp migration in Core

**Recommended model:** Sonnet
**Depends on:** none (this is the M1 carry-over)
**Blocks:** chunks 2+
**Approximate scope:** ~25 files, ~1,500 LOC touched (mostly mechanical type swaps and a real `ImageSharpProcessor` impl)

## Goal

Remove `System.Drawing.Common` from `InventoryKamera.Core` so the Core library is fully cross-platform. Replace `System.Drawing.Bitmap`, `Rectangle`, `Color`, `Size`, `Point` in Core abstractions and models with `SixLabors.ImageSharp` types.

## Architectural decision

The boundary becomes:

| Layer | Image type |
|-------|-----------|
| Core interfaces (`IScreenCapture`, `IImageProcessor`, `IOcrEngine`, `IUserInterface`, `OCRImageCollection`) | `SixLabors.ImageSharp.Image<Rgba32>` |
| Core geometry (`Rect`, `IntRange`) | ImageSharp `Rectangle`, `Point`, `Size`, `Color` |
| Infrastructure → screen capture | GDI+ capture internally → convert to `Image<Rgba32>` before returning |
| WinForms UserInterface adapter | converts `Image<Rgba32>` → `System.Drawing.Bitmap` for display in PictureBox controls |

The WinForms app keeps working: the conversion sits in `WinFormsUserInterface` and `WindowsScreenCapture`. The cost is two extra conversions per scan iteration; this is acceptable for the bridge phase and goes away when WinForms is removed in chunk 12.

## Files

**Modify (Core):**
- `InventoryKamera.Core/InventoryKamera.Core.csproj` — remove `<PackageReference Include="System.Drawing.Common" />`. Keep `SixLabors.ImageSharp 3.1.12`.
- `InventoryKamera.Core/Abstractions/IScreenCapture.cs` — replace `System.Drawing.Bitmap` returns with `Image<Rgba32>`; replace `System.Drawing.Size` with `SixLabors.ImageSharp.Size`.
- `InventoryKamera.Core/Abstractions/IImageProcessor.cs` — full type swap. Returns `Image<Rgba32>`. `Color` becomes `SixLabors.ImageSharp.PixelFormats.Rgba32`. `Rectangle` becomes `SixLabors.ImageSharp.Rectangle`. (`Crop`, `GetPixelColor`, `SetGrayscale`, `SetInvert`, `SetContrast`, `SetGamma`, `SetThreshold`, `FilterColors`, `ResizeImage`, `ScaleImage`, `KirschEdgeDetect`, `PreProcessImage`, `GetAverageColor`, `ClosestColor`, `CompareColors`, `CompareBitmapsFast`.)
- `InventoryKamera.Core/Abstractions/IOcrEngine.cs` — `AnalyzeText(Image<Rgba32> bitmap, ...)`.
- `InventoryKamera.Core/Abstractions/IUserInterface.cs` — every `Bitmap` parameter becomes `Image<Rgba32>`.
- `InventoryKamera.Core/Models/OCRImageCollection.cs` — internal bitmap dictionary keyed on `Image<Rgba32>`.
- `InventoryKamera.Core/Models/Rect.cs` — drop `System.Drawing` interop or convert it to ImageSharp `Rectangle` interop.

**Modify (still inside `InventoryKamera/`):**
- `InventoryKamera/Infrastructure/WindowsScreenCapture.cs` — `BitmapToImageSharp` helper; capture with GDI+ then return `Image<Rgba32>`.
- `InventoryKamera/Infrastructure/WinFormsImageProcessor.cs` — adopt the new signatures; if it cannot be made fully ImageSharp-clean today, convert in/out at every method boundary (slow but correct).
- `InventoryKamera/Infrastructure/OcrEnginePool.cs` — `Pix.LoadFromMemory` from an ImageSharp PNG-encoded `MemoryStream` (Tesseract takes `Pix`; ImageSharp encodes to PNG bytes cleanly).
- `InventoryKamera/scraping/*.cs` — any `Bitmap` field/parameter not covered by an interface call site becomes `Image<Rgba32>`. Most are already behind `_imageProcessor.*` so this is mostly type-name swaps and `using` updates.
- `InventoryKamera/scraping/GenshinProcesor.cs` — the static helpers that scrapers still call must be migrated alongside scraper sites. Where a scraper still calls a `GenshinProcesor` static, change the static to take/return `Image<Rgba32>`.
- `InventoryKamera/data/InventoryKamera.cs` — bitmap fields used for the OCR queue.
- `InventoryKamera/ui/UserInterface.cs` — keep `System.Drawing.Bitmap` here (PictureBox needs it) — the conversion happens in `WinFormsUserInterface` before delegating.
- `InventoryKamera/ui/WinFormsUserInterface.cs` — replace `Bitmap` params with `Image<Rgba32>`. Implement `ImageSharpToBitmap` helper (encode to PNG `MemoryStream`, construct `System.Drawing.Bitmap` from the stream). All existing `BeginInvoke` patterns stay.

**Create:**
- `InventoryKamera.Core/Imaging/ImageSharpExtensions.cs` — small helper for `Crop(this Image<Rgba32> src, Rectangle r)` if needed; keeps Core self-contained.
- `InventoryKamera.Tests/ImageConversionTests.cs` — round-trip a known PNG fixture: GDI+ Bitmap → Image<Rgba32> → Bitmap, assert pixel equality on a 4×4 sample area.

## Verification

1. Build solution — 0 errors.
2. `dotnet test` — all existing tests still pass; new `ImageConversionTests` pass.
3. Run WinForms app, complete a full scan with weapons + artifacts + characters + materials. GOOD JSON output must match a baseline scan from before the chunk (counts and key fields).
4. Smoke check: Core project no longer references `System.Drawing.Common` (verify by reading `obj/project.assets.json` or by running `dotnet list InventoryKamera.Core/InventoryKamera.Core.csproj package`).

## Notes for the agent

- The `ImageSharpProcessor` is **not** required to fully replace `WinFormsImageProcessor` in this chunk — keep `WinFormsImageProcessor` around but make it ImageSharp-typed at its public API. Internally it can still convert and use GDI+ ops temporarily. A pure ImageSharp implementation is a follow-up if desired but not gating.
- Tesseract `Pix.LoadFromMemory` accepts a PNG byte array. Encode `Image<Rgba32>` via `image.SaveAsPng(memoryStream)`.
- Beware `Color.FromArgb(...)`-style call sites — these become `new Rgba32(r, g, b, a)`.
- Avoid touching tessdata, scrapers' algorithmic logic, or scan profile JSON in this chunk. Type swaps and conversions only.

---

# CHUNK 2 — Extract Infrastructure project + Avalonia scaffold + DI + ViewModelBase + theme

**Recommended model:** Sonnet
**Depends on:** chunk 1
**Blocks:** chunks 3+

## Goal

Set up everything an Avalonia app needs to start and show an empty-but-themed shell window: project files, package references, DI container, MVVM toolkit base class, app bootstrap, a placeholder `MainWindow` with tab containers wired to placeholder view models for Scan/Settings/Export/About. The Genshin-themed dark palette is applied. The WinForms app is untouched and still runs.

## Architectural decisions

- **Single Infrastructure project, `net8.0-windows`** for Phase 2.0. Cross-platform stubbing comes in chunk 9.
- **Avalonia targets `net8.0`** (no `-windows`). It references `Core` always; it references `Infrastructure` only when building on Windows (MSBuild condition).
- **DI container:** `Microsoft.Extensions.DependencyInjection`, configured in `App.axaml.cs`. View models are resolved by `Locator` — a small `ServiceProvider` wrapper exposed as a static for XAML data templates.
- **MVVM base:** `CommunityToolkit.Mvvm` via `[ObservableProperty]` and `[RelayCommand]` source generators. No hand-rolled `INotifyPropertyChanged`.
- **Theme:** Fluent base + `GenshinTheme.axaml` resource overrides for the Genshin palette in the milestone doc.

## Files

**Create — `InventoryKamera.Infrastructure/`:**
- `InventoryKamera.Infrastructure.csproj` — `net8.0-windows`, references `Core`. Package refs needed for the moved files (TesseractOCR, InputSimulator, etc.).
- Move (do **not** copy) every file from `InventoryKamera/Infrastructure/` to `InventoryKamera.Infrastructure/`:
  - `WindowsScreenCapture.cs`, `WindowsInputSimulator.cs`, `OcrEnginePool.cs`, `WinFormsImageProcessor.cs`, `NullUserInterface.cs`, `GameDataService.cs` (the `InfraGameDataService` wrapper).
- Update `using` statements where needed (root namespace stays `InventoryKamera.Infrastructure`).
- Update `InventoryKamera/InventoryKamera.csproj` to add `<ProjectReference Include="..\InventoryKamera.Infrastructure\InventoryKamera.Infrastructure.csproj" />`.
- Update `InventoryKamera.sln` to include the new project.

**Create — `InventoryKamera.Avalonia/`:**
- `InventoryKamera.Avalonia.csproj`:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <OutputType>WinExe</OutputType>
      <TargetFramework>net8.0</TargetFramework>
      <Nullable>disable</Nullable>
      <ImplicitUsings>disable</ImplicitUsings>
      <ApplicationIcon>..\InventoryKamera\Item_Special_Kamera.ico</ApplicationIcon>
      <ApplicationManifest>app.manifest</ApplicationManifest>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="Avalonia" Version="11.2.3" />
      <PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
      <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
      <PackageReference Include="Avalonia.Diagnostics" Version="11.2.3" Condition="'$(Configuration)' == 'Debug'" />
      <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
      <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
      <PackageReference Include="NLog" Version="5.3.4" />
      <ProjectReference Include="..\InventoryKamera.Core\InventoryKamera.Core.csproj" />
      <ProjectReference Include="..\InventoryKamera.Infrastructure\InventoryKamera.Infrastructure.csproj"
                        Condition="$([MSBuild]::IsOSPlatform('Windows'))" />
    </ItemGroup>
    <ItemGroup>
      <None Include="..\InventoryKamera\inventorylists\**\*" Link="inventorylists\%(RecursiveDir)%(Filename)%(Extension)">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Include="..\InventoryKamera\tessdata\**\*" Link="tessdata\%(RecursiveDir)%(Filename)%(Extension)">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
    </ItemGroup>
  </Project>
  ```
- `app.manifest` — copy from WinForms, ensure `dpiAware` is set.
- `Program.cs` — Avalonia entry point with `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`. Set up NLog identically to `InventoryKamera/Program.cs`.
- `App.axaml` + `App.axaml.cs` — declares Fluent theme + GenshinTheme; `OnFrameworkInitializationCompleted` builds the DI container and resolves `MainWindow`.
- `Locator.cs` — exposes `static IServiceProvider Services { get; }` for XAML `DataTemplate` resolution.
- `Views/MainWindow.axaml` + `.axaml.cs` — chrome window with `TabControl` bound to `Tabs` collection on `MainViewModel`; tab DataTemplates resolve to ScanView / SettingsView / ExportView / AboutView.
- `Views/ScanView.axaml`, `Views/SettingsView.axaml`, `Views/ExportView.axaml`, `Views/AboutView.axaml` — **placeholders only**: a single `TextBlock` "Scan view goes here" etc. Real content arrives in chunks 5/7/8.
- `ViewModels/ViewModelBase.cs` — `public abstract class ViewModelBase : ObservableObject` (CommunityToolkit base).
- `ViewModels/MainViewModel.cs` — `[ObservableProperty] object selectedTab` + a `Tabs` array of view models resolved from DI.
- `ViewModels/ScanViewModel.cs`, `SettingsViewModel.cs`, `ExportViewModel.cs`, `AboutViewModel.cs` — empty subclasses of `ViewModelBase` for now.
- `Assets/Styles/GenshinTheme.axaml` — `<Styles>` with the palette from the milestone doc:
  ```xml
  <Style.Resources>
    <Color x:Key="PrimaryBg">#1e2328</Color>
    <Color x:Key="SecondaryBg">#2d3139</Color>
    <Color x:Key="Accent">#4A90E2</Color>
    <Color x:Key="TextPrimary">#ECE5D8</Color>
    <Color x:Key="TextSecondary">#A69F92</Color>
    <Color x:Key="Success">#5AB55E</Color>
    <Color x:Key="Warning">#F9B959</Color>
    <Color x:Key="Error">#E74C3C</Color>
  </Style.Resources>
  ```
  Plus `Window`, `TabItem`, `Button`, `TextBlock` style overrides using the resources.

**Modify:**
- `InventoryKamera.sln` — add the two new projects.
- `InventoryKamera/data/InventoryKamera.cs` — update `using InventoryKamera.Infrastructure;` namespace if needed (no logic change).
- Delete `InventoryKamera/Infrastructure/` after the move (the .csproj `<Compile Remove>` rules from the SDK style mean files just disappear from the WinForms project).

## Verification

1. Solution builds — 0 errors.
2. `./InventoryKamera/bin/x64/Debug/InventoryKamera.exe` still runs and a full scan still works.
3. `dotnet run --project InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj` launches a window with four tabs and the dark Genshin palette. Each tab shows its placeholder text.

## Notes

- Avalonia's `<Application.Styles>` order matters — Fluent base must be **before** GenshinTheme so overrides win.
- `Avalonia.Diagnostics` enables F12 dev tools in Debug builds; very useful for chunks 5/7.
- Do not attempt scanning yet — `ScanViewModel` is empty.

---

# CHUNK 3 — Cross-platform settings service

**Recommended model:** Sonnet
**Depends on:** chunk 2
**Blocks:** chunks 4, 7

## Goal

Replace the .NET-Framework-style `Properties.Settings.Default` (driven by `JsonUserSettingsProvider`) with a portable JSON-backed settings service that works on Windows, macOS, and Linux. The Avalonia app reads/writes settings through `ISettingsService`. The WinForms app is left untouched (it can finish out its life on the existing `Properties.Settings`).

## Architectural decisions

- **Storage path:** `Environment.GetFolderPath(SpecialFolder.ApplicationData)` + `/InventoryKamera/settings.json`. Cross-platform-safe.
- **Service in Core** (so future tools can read settings too). Implementation is Newtonsoft-JSON-backed.
- **Strongly typed model.** No magic strings, no dictionary indirection. `ScanSettings` POCO with default values.
- **Notification:** `ISettingsService.SettingsChanged` event for view models to subscribe to. View models that own settings UI write through a typed property and the service auto-saves on change (debounced).
- **Migration:** First Avalonia launch reads the existing `user.config` (or the WinForms-Settings JSON) if present and copies values into the new file. Documented in the README; not a UI affordance.

## Files

**Create:**
- `InventoryKamera.Core/Configuration/ScanSettings.cs` — POCO with all current `Properties.Settings.Default.*` keys: `ScanWeapons`, `ScanArtifacts`, `ScanCharacters`, `ScanMaterials`, `ScanCharDevItems`, `MinimumWeaponRarity`, `MinimumWeaponLevel`, `MinimumArtifactRarity`, `MinimumArtifactLevel`, `EquipWeapons`, `EquipArtifacts`, `ScannerDelay`, `OutputPath`, `InventoryKey`, `CharacterKey`, `Slot1Key`, `LastUpdateCheck`, `UpgradeNeeded`. Use ints for keys (VK codes), strings for paths.
- `InventoryKamera.Core/Configuration/ISettingsService.cs` — `ScanSettings Current { get; }`, `void Save()`, `event EventHandler SettingsChanged`.
- `InventoryKamera.Core/Configuration/JsonSettingsService.cs` — file path resolved via injected `Func<string>` (so tests can stub a temp dir). Atomic save: write to `.tmp`, then `File.Move(..., overwrite: true)`. Debounce 500 ms via `System.Threading.Timer`.
- `InventoryKamera.Tests/SettingsServiceTests.cs` — round-trip save/load; corrupt-file recovery; defaults applied when file missing; `SettingsChanged` fires on save.

**Modify:**
- `InventoryKamera.Avalonia/App.axaml.cs` — register `ISettingsService` as a singleton in DI.
- `InventoryKamera.Avalonia/Program.cs` — ensure settings dir exists before DI bootstrap.

## Verification

1. `dotnet test` — settings tests pass.
2. Run Avalonia app once → `%AppData%/InventoryKamera/settings.json` is created with defaults.
3. Edit the file by hand, change `OutputPath`, restart Avalonia app → settings service exposes the edited value.
4. Atomic-write test: kill the process during a debounced save → file is intact (use a unit test with a fault-injecting `IFileSystem` if you want full TDD; otherwise visual check).

## Notes

- Do **not** wire any real consumers (ScanViewModel etc.) in this chunk. That's chunks 4, 7. This chunk only delivers the service + tests + DI registration.
- Keep `JsonSettingsService` Newtonsoft-based (consistent with the rest of the codebase). No System.Text.Json.

---

# CHUNK 4 — ScanViewModel

**Recommended model:** Sonnet
**Depends on:** chunks 1, 2, 3
**Blocks:** chunks 5, 6

## Goal

Implement the orchestration view model that drives the scan workflow. It owns the cancellation token, the OCR preview state, the per-category progress counters, the scrollable timestamped console, and the Start / Stop / Manual-Export commands. It does **not** depend on any view; bindings come in chunk 5.

## Architectural decisions

- **Cancellation:** `CancellationTokenSource` — passed into `InventoryKamera.GatherData` (which currently uses `volatile bool b_threadCancel`). The data-layer change is minimal — add an overload that accepts a `CancellationToken` and checks it instead of (or alongside) the volatile flag. Keep the volatile flag as a safety net during transition.
- **Thread offloading:** `Task.Run` instead of `new Thread`. The result task is awaited inside the `[RelayCommand]` so the UI re-enables Start when the scan finishes.
- **Console:** `ObservableCollection<ConsoleEntry>` where `ConsoleEntry { DateTime Timestamp; string Message; LogLevel Level }`. Add an `NLogTarget` that pushes to the collection on the UI thread.
- **OCR preview:** properties `CurrentItemName`, `CurrentItemStats` (string[]), `CurrentScreenshot` (Avalonia `Bitmap`), `IsCurrentItemValid` (bool?), `ValidationMessage` (string). The `IUserInterface` adapter (chunk 6) sets these.
- **Counters:** `[ObservableProperty]` `WeaponsScanned`, `WeaponsTotal`, `ArtifactsScanned`, `ArtifactsTotal`, `CharactersScanned`, `CharactersTotal`, `MaterialsScanned`, `MaterialsTotal`. Plus computed `WeaponProgressText` etc. via `[NotifyPropertyChangedFor]`.
- **No global hotkey in 2.0.** `NHotkey.WindowsForms` is WinForms-bound. Document a follow-up to add a Windows-only global hotkey via P/Invoke; for now, the in-app Stop button (and `Esc` keybinding) is sufficient.

## Files

**Modify:**
- `InventoryKamera.Avalonia/ViewModels/ScanViewModel.cs` — full implementation. Inject `ISettingsService`, `IScreenCapture`, `IInputSimulator`, `IGameDataService`, `IOcrEngine`, `IImageProcessor`, `IUserInterface`, `ILogger<ScanViewModel>`. Construct an `InventoryKamera.InventoryKamera` instance per-scan (existing class); pass it the injected services rather than the WinForms-coupled overload.
- `InventoryKamera/data/InventoryKamera.cs` — add a constructor that takes injected services + a `CancellationToken`. Keep the existing `MainForm`-coupled constructor for the WinForms app. Add `GatherData(CancellationToken token)` overload. Wire the token through to scraper loops and worker threads (the existing `b_threadCancel` becomes `token.IsCancellationRequested`).

**Create:**
- `InventoryKamera.Avalonia/Services/ConsoleEntry.cs` — record with timestamp, level, message.
- `InventoryKamera.Avalonia/Services/ObservableCollectionLogTarget.cs` — NLog `Target` subclass that posts to an `ObservableCollection<ConsoleEntry>` via Avalonia `Dispatcher.UIThread.Post`.
- `InventoryKamera.Tests/ScanViewModelTests.cs`:
  - StartScan command sets `IsScanning = true` and disables itself
  - Cancel triggers token cancellation and `IsScanning = false` resolves
  - Counter increments observably
  - Console grows on log events
  - OCR preview properties update via `IUserInterface` mock

## Verification

1. `dotnet test` — ScanViewModel tests green.
2. Build: 0 errors. The Avalonia app still boots (the view doesn't show progress yet — chunk 5).

## Notes

- Use `IAsyncRelayCommand` for `StartScanCommand` so WPF/Avalonia disable-while-running works automatically.
- `_inventoryKamera.HasData` already exists — bind the manual export command's `CanExecute` to it.

---

# CHUNK 5 — ScanView.axaml layout, bindings, OCR preview

**Recommended model:** Sonnet
**Depends on:** chunks 4, 6
**Blocks:** chunks 7, 8 (only by sequencing convention; technically independent)

## Goal

Deliver the visual scan tab that matches the milestone doc's UI mockup:

- Left panel: scan-category checkboxes, min-rarity dropdown, scan-speed selector, Start/Stop buttons.
- Right panel top: per-category progress bars + counts (e.g. `237 / 895`).
- Right panel middle: **OCR preview** — current item name, parsed stats, screenshot thumbnail, validation status (✓/✗).
- Bottom: scrollable timestamped console output, collapsible.

## Files

**Modify:**
- `InventoryKamera.Avalonia/Views/ScanView.axaml` — replace placeholder with full grid layout. Use `Grid` with `RowDefinitions="Auto,*,200"` and `ColumnDefinitions="280,*"` (or DockPanel + StackPanel). Bind every control to `ScanViewModel`.
- `InventoryKamera.Avalonia/Views/ScanView.axaml.cs` — empty code-behind, no logic.
- `InventoryKamera.Avalonia/Assets/Styles/GenshinTheme.axaml` — add `ProgressBar`, `Expander`, `CheckBox`, `ComboBox`, `Slider` styles to match the dark theme.

**Create:**
- `InventoryKamera.Avalonia/Converters/PercentConverter.cs` — `(scanned, total) → double 0..1` for ProgressBar Value.
- `InventoryKamera.Avalonia/Converters/ValidationIconConverter.cs` — `bool? → "✓" | "✗" | ""`.
- `InventoryKamera.Avalonia/Converters/LogLevelToBrushConverter.cs` — Info/Warn/Error → palette colors.

## Verification

1. Avalonia app launches; Scan tab shows all controls correctly themed and arranged.
2. With the WinForms app closed and Genshin Impact running: click Start. The scan executes. Counters tick up. The OCR preview shows the latest item and its parsed values. The console scrolls. Stop cancels the scan. GOOD JSON output is produced and identical (by counts and key fields) to a WinForms baseline.
3. Take a screenshot of the running app and visually compare to the mockup in `docs/archive/AVALONIA_UI_MOCKUP.md`.

## Notes

- For the screenshot thumbnail: Avalonia `Image` source must be `Avalonia.Media.Imaging.Bitmap`. The conversion happens in `AvaloniaUserInterface` (chunk 6) — `ScanView.axaml` just binds `Source="{Binding CurrentScreenshot}"`.
- The console `ItemsControl` should virtualize (`<ScrollViewer>` with `VirtualizingStackPanel`) — log volume can hit thousands of entries.

---

# CHUNK 6 — AvaloniaUserInterface (IUserInterface adapter)

**Recommended model:** Sonnet
**Depends on:** chunks 1, 2, 4
**Blocks:** chunk 5 (effectively, since the view is empty without UI updates)

## Goal

Implement the `IUserInterface` adapter that the scrapers call into. It marshals every call onto the Avalonia UI thread via `Dispatcher.UIThread.Post` and updates `ScanViewModel` properties. It also converts `Image<Rgba32>` (from chunk 1) to `Avalonia.Media.Imaging.Bitmap` for display.

## Files

**Create:**
- `InventoryKamera.Avalonia/Services/AvaloniaUserInterface.cs` — implements `IUserInterface`. Constructor takes `ScanViewModel`. Each method is a `Dispatcher.UIThread.Post(...)`. Implement bitmap conversion via `image.SaveAsPng(stream); stream.Position = 0; new Bitmap(stream)`.
- `InventoryKamera.Avalonia/Services/ImageSharpToAvaloniaBitmap.cs` — extension method `ToAvaloniaBitmap(this Image<Rgba32> img)` for conversion. (If chunk 1's `ImageSharpExtensions.cs` already has helpers, add this there instead.)
- `InventoryKamera.Tests/AvaloniaUserInterfaceTests.cs` — verify property fan-out using a stub `Dispatcher` (or run on the test thread synchronously with `Dispatcher.UIThread.RunJobs()`).

**Modify:**
- `InventoryKamera.Avalonia/App.axaml.cs` — DI registration: `services.AddSingleton<IUserInterface, AvaloniaUserInterface>();` (note: `AvaloniaUserInterface` resolves `ScanViewModel` from DI).

## Verification

1. `dotnet test` — adapter tests pass.
2. Combined with chunk 5: the Scan tab updates live during a scan.

## Notes

- `Dispatcher.UIThread.Post` is the safe equivalent of WinForms `BeginInvoke` for cross-thread UI updates.
- Avalonia bitmap construction from a stream is **synchronous** — fine on the UI thread but allocate-heavy at high frame rates. Throttle the OCR preview update to one frame every ~100 ms inside the adapter (use a `Stopwatch` field to gate).
- Disposal: `Image<Rgba32>` is `IDisposable`. The adapter takes ownership of incoming images for display and disposes them after rendering. Avalonia `Bitmap` is also IDisposable — keep the previous one until the next replaces it, then dispose.

---

# CHUNK 7 — SettingsView + SettingsViewModel + key capture

**Recommended model:** Sonnet
**Depends on:** chunks 3, 5
**Blocks:** none

## Goal

Build the Settings tab: two-way bound to `ISettingsService.Current`, with key-rebinding controls for the three Genshin keys (inventory / character / slot1) and an output-path picker.

## Files

**Modify:**
- `InventoryKamera.Avalonia/ViewModels/SettingsViewModel.cs` — full impl. Properties mirror `ScanSettings`, mutated through `ISettingsService.Current.X = ...; settingsService.Save();`.
- `InventoryKamera.Avalonia/Views/SettingsView.axaml` — full layout with `CheckBox`, `ComboBox`, `Slider`, three `KeyBindingCapture` controls, `TextBox` + `Button` for output path.

**Create:**
- `InventoryKamera.Avalonia/Controls/KeyBindingCapture.axaml` + `.axaml.cs` — focusable `Border` that captures next `KeyDown`, displays the key glyph via `Key.ToString()`. Cross-platform (no P/Invoke). Output: a `Key` enum value bound via a `KeyboardKey` property.
- `InventoryKamera.Avalonia/Services/FolderPickerService.cs` — wraps `TopLevel.GetTopLevel(...).StorageProvider.OpenFolderPickerAsync(...)`. Avoids tying the view model directly to `TopLevel`.
- `InventoryKamera.Tests/SettingsViewModelTests.cs` — toggling a property persists via `ISettingsService`; key capture stores the right `Key`.

## Verification

1. Settings tab renders. Toggling a checkbox writes to `settings.json` (verify by reading the file). Restart preserves the value.
2. Capture a key in the `Inventory` field — the displayed glyph matches the pressed key.
3. Folder picker opens and returns a path that lands in the `OutputPath` text box.

## Notes

- `WindowsInput.Native.VirtualKeyCode` is what Genshin scraping uses today, but it's a Windows-specific enum. Store the cross-platform Avalonia `Key` in settings; convert to `VirtualKeyCode` inside the Windows `IInputSimulator` adapter at use time. Add a small `KeyToVirtualKeyCode` map there.

---

# CHUNK 8 — ExportView + ExportViewModel + AboutView + AboutViewModel + DB update integration

**Recommended model:** Sonnet (could be Haiku per-half but cohesive enough to keep together)
**Depends on:** chunks 3, 5
**Blocks:** none

## Goal

Two simple tabs:

- **Export:** Manual export to GOOD JSON; "Open output folder" button; "Upload to Genshin Optimizer" (clipboard + open URL).
- **About:** Version, GitHub/releases/issues links, "Update game data" button (wraps `DatabaseManager.UpdateGameData()`), "Check for app updates" (Octokit, copied from `CheckForKameraUpdates` in MainForm).

## Files

**Modify:**
- `InventoryKamera.Avalonia/ViewModels/ExportViewModel.cs` + `Views/ExportView.axaml` — full impl.
- `InventoryKamera.Avalonia/ViewModels/AboutViewModel.cs` + `Views/AboutView.axaml` — full impl.

**Create:**
- `InventoryKamera.Avalonia/Services/ClipboardService.cs` — `ClipboardService.SetTextAsync(string)` wrapper around `TopLevel.Clipboard.SetTextAsync` so view models stay testable.
- `InventoryKamera.Avalonia/Services/UrlLauncher.cs` — `Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = url })` cross-platform.
- `InventoryKamera.Tests/ExportViewModelTests.cs` and `AboutViewModelTests.cs` — light coverage.

## Verification

1. Both tabs render. Each command does the right thing.
2. Clicking "Update game data" with a stale version actually fetches and updates `inventorylists/*.json` — verify the dialog flow and the on-disk file change.

## Notes

- ExportViewModel needs the latest scan result. ScanViewModel exposes a `LastScan` property (an `InventoryKamera.InventoryKamera` instance or `GOOD` payload); ExportViewModel observes it.
- The `OpenOptimizerDialog` in WinForms uses a synchronous `MessageBox.Show`. In Avalonia, use `MessageBoxManager`-style flow — either the `MsBox.Avalonia` package or a simple custom `ContentDialog`. `MsBox.Avalonia` is fine for v1.

---

# CHUNK 9 — Cross-platform build + platform warning

**Recommended model:** Sonnet
**Depends on:** chunks 2, 4, 6 (everything that touches platform-specific code must already exist)
**Blocks:** none

## Goal

Make the Avalonia project compile on macOS and Linux. Scanning is unavailable on those platforms; the Start button is enabled but shows a `MessageBox.Show("Scanning requires Windows. ...")` when clicked. The other tabs are fully functional (export of an existing GOOD file, about, settings).

## Architectural decisions

- The `ProjectReference` to `InventoryKamera.Infrastructure` is already conditioned on Windows (chunk 2). On non-Windows, DI registers null/stub implementations of the platform interfaces.
- `ScanViewModel.StartScanCommand` checks `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` first and short-circuits with the warning dialog.

## Files

**Create:**
- `InventoryKamera.Avalonia/Services/PlatformStubs/StubScreenCapture.cs`, `StubInputSimulator.cs`, `StubOcrEngine.cs`, `StubImageProcessor.cs` — minimal `IXxx` implementations that throw `PlatformNotSupportedException` on use. Used on non-Windows.

**Modify:**
- `InventoryKamera.Avalonia/App.axaml.cs` — DI registration uses platform stubs when not on Windows. Pseudocode:
  ```csharp
  if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
  {
      services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
      // ... etc
  }
  else
  {
      services.AddSingleton<IScreenCapture, StubScreenCapture>();
      // ... etc
  }
  ```
- `InventoryKamera.Avalonia/ViewModels/ScanViewModel.cs` — platform check at the top of `StartScanAsync`.

**CI / build:**
- Add a smoke build to the existing build commands list for Linux/macOS:
  ```
  dotnet build InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj -r linux-x64
  dotnet build InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj -r osx-x64
  ```
  These should succeed (they will produce a binary that refuses to scan but renders the UI).

## Verification

1. The two `dotnet build` commands above succeed on Windows (cross-compile target).
2. Optional: SSH into / use a VM for Linux, run the produced binary, verify the window opens and Start shows the platform warning.

## Notes

- `System.Drawing.Common` runtime support on Linux requires `libgdiplus`. We removed it from Core in chunk 1 — verify it didn't sneak back in via a transitive dep on Core.
- The Infrastructure project (`net8.0-windows`) won't even build on Linux. That's why it's referenced conditionally.

---

# CHUNK 10 — ViewModel unit tests (>80% coverage)

**Recommended model:** Sonnet
**Depends on:** chunks 4, 6, 7, 8
**Blocks:** none

## Goal

Bring view-model coverage above 80% as required by the milestone exit criteria. Each prior chunk seeded the `*Tests.cs` file with the most important assertions; this chunk closes gaps systematically.

## Files

**Modify (expand):**
- `InventoryKamera.Tests/ScanViewModelTests.cs`
- `InventoryKamera.Tests/SettingsViewModelTests.cs`
- `InventoryKamera.Tests/AvaloniaUserInterfaceTests.cs`
- `InventoryKamera.Tests/ExportViewModelTests.cs`
- `InventoryKamera.Tests/AboutViewModelTests.cs`

**Create:**
- `InventoryKamera.Tests/coverage.runsettings` — coverlet collector config to exclude generated MVVM source-generator code from coverage denominator.

## Verification

```
dotnet test --collect:"XPlat Code Coverage" --settings InventoryKamera.Tests/coverage.runsettings
```

Look at the produced `coverage.cobertura.xml`. View-model assemblies must report > 80% line coverage.

## Notes

- Use `Moq` (already in the dependency tree if we add it; if not, add `Moq 4.20.x`).
- Avoid testing CommunityToolkit-generated property setters; focus on behavior: command CanExecute transitions, computed properties, error paths, persistence side-effects.

---

# CHUNK 11 — WinForms deprecation marking

**Recommended model:** Haiku
**Depends on:** chunks 1–10 (Avalonia must be fully functional)
**Blocks:** chunk 12 (by sequencing — gives a beta window)

## Goal

Mark the WinForms project as deprecated and document the migration. No code deletion yet; the WinForms app remains buildable and runnable for the beta window.

## Files

**Create:**
- `InventoryKamera/DEPRECATED.md` — short note: "The WinForms UI is deprecated as of M2. Use the Avalonia build at `InventoryKamera.Avalonia/`. This folder will be removed in M2.2."

**Modify:**
- `README.md` — update the build/run sections to point to Avalonia first; note the WinForms version is deprecated.
- `CLAUDE.md` — add a note in the Build Commands section about the new Avalonia project.
- `InventoryKamera/InventoryKamera.csproj` — `<PropertyGroup><AssemblyTitle>InventoryKamera (DEPRECATED)</AssemblyTitle></PropertyGroup>` so the binary metadata reflects status.

## Verification

Both builds still produce binaries. The README guides a new user to the Avalonia version first.

---

# CHUNK 12 — Remove WinForms project

**Recommended model:** Sonnet
**Depends on:** chunk 11 (and a successful beta window — gating decision is the user's, not the agent's)
**Blocks:** none

## Goal

Delete the WinForms project and clean the solution. This is destructive — the agent must confirm with the user before running `git rm -r InventoryKamera/`.

## Files

**Delete:**
- `InventoryKamera/` (entire WinForms project — `MainForm.*`, `MainUI.*`, `Program.cs`, `Settings.cs`, `JsonUserSettingsProvider.cs`, `App.config`, `app.manifest`, `Properties/`, `data/InventoryKamera.cs` *(IF the data orchestration class has been moved to Core or shared by chunk 4 — verify before deleting)*, `game/Navigation.cs`, `scraping/*`, `ui/*`, `tessdata/*`, `inventorylists/*`).

**Critical:** the following files **must be moved into Core or Infrastructure before deletion**, not lost:
- `InventoryKamera/data/InventoryKamera.cs` → `InventoryKamera.Core/Orchestration/InventoryScanner.cs` (rename to avoid the project-name collision while in-flight).
- `InventoryKamera/game/Navigation.cs` → `InventoryKamera.Infrastructure/Windows/Navigation.cs`.
- `InventoryKamera/scraping/*` → `InventoryKamera.Core/Scraping/*` (or split: per-game-area scrapers in Core, OCR-region static helpers in Core's ImageProcessing). Delete `GenshinProcesor.cs` outright if its remaining methods have all been replaced by `IImageProcessor` calls.
- `InventoryKamera/tessdata/`, `InventoryKamera/inventorylists/` → already conditionally referenced by Avalonia in chunk 2. Move to a shared `assets/` folder at the solution root and update every csproj's `<None>` includes.
- `Item_Special_Kamera.ico` → solution root.

**Modify:**
- `InventoryKamera.sln` — remove the WinForms project entry.
- `CLAUDE.md` — drop the Rider-MSBuild instructions (no longer needed for net8.0-only projects), update build commands to `dotnet build`.

## Verification

1. `dotnet build InventoryKamera.sln` — 0 errors. The MSB3202 warning about `InventoryKameraWPF.csproj` should also disappear.
2. The Avalonia app still runs and scans correctly.
3. `git status` shows the WinForms folder removed; commit with a clear "feat: retire WinForms UI" message.

## Notes

- This is the most error-prone chunk because it relocates code that was previously inside the WinForms project. Make a tag (`pre-winforms-removal`) before starting so it can be rolled back.
- The build script should now be a single `dotnet build` — no Rider MSBuild requirement, since the C# 7.3 compatibility issue from the .NET Framework era is gone.

---

## Milestone 2 Exit Checklist

Maps directly to the milestone doc:

- [ ] Avalonia app compiles and runs on Windows 10/11 — chunks 2 + 5
- [ ] All scanning features work identically to WinForms — chunks 4 + 5 + 6
- [ ] OCR preview panel functional during scanning — chunks 5 + 6
- [ ] Settings persist across restarts — chunks 3 + 7
- [ ] GOOD JSON identical to WinForms output — verified after chunk 5
- [ ] ViewModel unit test coverage > 80% — chunk 10
- [ ] macOS and Linux builds compile (scanning shows platform warning) — chunk 9
- [ ] WinForms project removed from the solution — chunk 12

## Out of Scope for This Plan

- **Visual region editor** for `ScanProfile.json` — the milestone doc lists this as "Phase 2+" / future work. Defer until M2 ships and feedback is in.
- **Avalonia global hotkey** — `NHotkey.WindowsForms` is gone; if a global Stop hotkey is desired, it goes in a follow-up plan with platform-specific implementations.
- **Localization / i18n** — not in scope.
- **Auto-update for the Avalonia binary** — covered for app version checks (chunk 8) but not auto-installing updates.

---

*Plan written 2026-04-29 on branch `feat/avalonia-implementation`. Source spec: `docs/MILESTONE_2_AVALONIA.md`.*
