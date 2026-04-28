# Milestone 2: Avalonia UI + Cross-Platform

**Goal:** New Avalonia UI sharing the Core library. Cross-platform builds for Windows, macOS, and Linux. WinForms retired after feature parity is reached.

**Prerequisite:** Milestone 1 complete — Core library extracted, all interfaces defined.

**Status:** Not started.

## Carry-Overs from Milestone 1

Complete these before starting new M2 work:

- **ImageSharp migration** — Remove `System.Drawing.Common` from `InventoryKamera.Core`; replace `Bitmap`/`Rectangle`/`Color` in all Core interfaces and models with `SixLabors.ImageSharp` types. Remove `Accord.Imaging` at the same time (tightly coupled).
- **ScanProfile scraper wiring** — `ScanProfile.json` and `ScanProfileManager` exist; wire scrapers to read region coordinates from the profile instead of hardcoded ratios.
- **Tesseract 5.5.2 upgrade** — Evaluation confirmed viable; update `PackageReference` from `Tesseract 5.2.0` to `TesseractOCR 5.5.2`, update engine pool initialization, run a full scan to verify.

---

## Project Structure After This Milestone

```
InventoryKamera.sln
├── InventoryKamera.Core/           # .NET 8 — business logic, interfaces
├── InventoryKamera.Infrastructure/ # .NET 8 — Windows implementations
├── InventoryKamera.Avalonia/       # .NET 8 — Avalonia UI (primary)
├── InventoryKamera/                # net8.0-windows — WinForms (deprecated, removed in Phase 2.2)
└── InventoryKamera.Tests/          # .NET 8 — unit tests
```

---

## Technology Choices

| Choice | Rationale |
|--------|-----------|
| Avalonia 11.x | Stable, active development, best cross-platform desktop support |
| CommunityToolkit.Mvvm | Simpler than ReactiveUI, right-sized for this project, source-generator based |
| `net8.0` target (no `-windows`) | Cross-platform from the start |
| Windows-only scanning for Phase 2.0 | macOS/Linux get the UI; platform warning shown for scan features until implementations added |

---

## Phase 2.0: Core Migration (Windows)

### New project: InventoryKamera.Avalonia.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.3" />
    <PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
    <ProjectReference Include="..\InventoryKamera.Core\InventoryKamera.Core.csproj" />
    <ProjectReference Include="..\InventoryKamera.Infrastructure\InventoryKamera.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Project structure

```
InventoryKamera.Avalonia/
├── App.axaml + App.axaml.cs         # DI setup, application entry point
├── Views/
│   ├── MainWindow.axaml             # Tab container
│   ├── ScanView.axaml               # Scan tab (primary workflow)
│   ├── SettingsView.axaml           # Settings tab
│   ├── ExportView.axaml             # Export tab
│   └── AboutView.axaml
├── ViewModels/
│   ├── ViewModelBase.cs             # INotifyPropertyChanged + SetProperty
│   ├── MainViewModel.cs             # Tab navigation
│   ├── ScanViewModel.cs             # Scan start/stop, progress, OCR preview
│   ├── SettingsViewModel.cs         # Settings two-way binding
│   ├── ExportViewModel.cs           # GOOD JSON export
│   └── AboutViewModel.cs
├── Services/
│   └── AvaloniaUserInterface.cs     # IUserInterface using Dispatcher.UIThread
└── Assets/
    └── Styles/
        └── GenshinTheme.axaml       # Dark theme, Genshin-inspired palette
```

### Scan view layout requirements

The scan view must include all of these (from UI mockup):

- **Left panel:** Scan category checkboxes (Characters, Weapons, Artifacts, Materials), min rarity filter, scan speed selector, Start/Stop buttons
- **Right panel (top):** Live scan progress per category — progress bars with item counts (e.g., `237 / 895`)
- **Right panel (middle):** **OCR preview panel** — current item name, parsed stats, screenshot thumbnail, validation status (✓ Valid / ✗ Invalid). This panel is essential for user confidence and debugging.
- **Bottom:** Scrollable timestamped console output, collapsible

### DI setup (App.axaml.cs)

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Platform services — Windows implementations from Infrastructure project
    services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
    services.AddSingleton<IInputSimulator, WindowsInputSimulator>();
    services.AddSingleton<IOcrEngine, OcrEnginePool>();
    services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
    services.AddSingleton<IGameDataService, GameDataService>();

    // UI
    services.AddSingleton<MainViewModel>();
    services.AddTransient<ScanViewModel>();
    services.AddTransient<SettingsViewModel>();
    services.AddTransient<ExportViewModel>();
    services.AddTransient<AboutViewModel>();
    services.AddSingleton<IUserInterface, AvaloniaUserInterface>();
}
```

### AvaloniaUserInterface

Replaces the static `UserInterface.cs` WinForms class:

```csharp
public class AvaloniaUserInterface : IUserInterface
{
    private readonly ScanViewModel _scanVm;

    public AvaloniaUserInterface(ScanViewModel scanVm) => _scanVm = scanVm;

    public void SetWeaponMax(int count) =>
        Dispatcher.UIThread.Post(() => _scanVm.TotalWeapons = count);

    public void IncrementWeapon(int count) =>
        Dispatcher.UIThread.Post(() => _scanVm.WeaponsScanned += count);

    public void UpdatePreview(Bitmap bitmap) =>
        Dispatcher.UIThread.Post(() =>
            _scanVm.CurrentScreenshot = ConvertToAvaloniaBitmap(bitmap));

    private Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(System.Drawing.Bitmap gdi)
    {
        using var ms = new MemoryStream();
        gdi.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(ms);
    }
}
```

### Platform warning for non-Windows

```csharp
// ScanViewModel.StartScanAsync()
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    await MessageBox.Show(
        "Scanning requires Windows. You can view and export existing scan data on this platform.");
    return;
}
```

### Theming

Genshin-inspired dark palette for `GenshinTheme.axaml`:

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

---

## Phase 2.1: WinForms Deprecation

- Mark WinForms project `[DEPRECATED]` in its folder README
- Critical bug fixes backported to both versions during this phase
- Community beta testing of Avalonia version

## Phase 2.2: WinForms Removal

- Delete `InventoryKamera/` WinForms project from solution
- Update solution file and build scripts
- Update CLAUDE.md build commands
- Final state: Core + Infrastructure + Avalonia + Tests only

---

## Visual Region Configuration (Phase 2+)

After Avalonia is stable, add a visual region editor for `ScanProfile.json`. Users load a screenshot and drag region overlays instead of editing JSON numbers.

**In scope:**
- Load a weapon/artifact card screenshot from disk
- Display visual overlays for each OCR region (color-coded, semi-transparent)
- Drag corners/edges to resize; drag center to reposition
- Live OCR test on selected region (shows preprocessed image + Tesseract output)
- Save/export custom profiles

**Out of scope:** Conditional rules engine. Special cases like the sanctified artifact shift are handled in code, not in a config-driven detection framework.

---

## Milestone 2 Complete When

- [ ] Avalonia app compiles and runs on Windows 10/11
- [ ] All scanning features work identically to the WinForms version
- [ ] OCR preview panel is functional during scanning
- [ ] Settings persist across restarts
- [ ] GOOD JSON export produces identical output to WinForms version
- [ ] ViewModel unit tests >80% coverage
- [ ] macOS and Linux builds compile (scanning shows platform warning)
- [ ] WinForms project removed from the solution

---

*See `docs/archive/PHASE_2_AVALONIA.md` and `docs/archive/AVALONIA_UI_MOCKUP.md` for the previous detailed plans (superseded by this document).*
*See `docs/archive/PHASE_2_VISUAL_REGION_CONFIG.md` for the visual region config research.*
*Last updated: 2026-04-12*
