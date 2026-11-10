# Milestone 2.5: Linux Scanning

**Goal:** Scan from a native Linux build while the game runs under Wine/Proton. Along the way, make the scanner itself platform-independent, so only screen capture, input and window lookup are platform-specific.

**Prerequisite:** Milestone 2 complete. ✅

**Status:** Planned (2026-11-10). Not started.

**Relation to Milestone 3:** independent; neither needs the other. Step 1 is worth doing first anyway (see below).

---

## Why

Genshin Impact has no native Linux (or macOS) client, but it runs on Linux under Wine/Proton. Today the scanner only runs on Windows: on Linux the `net8.0` build shows the UI with stub services and a "Windows only" warning. Running the Windows build of the scanner inside the game's Wine prefix might work, but it's fragile and untested. A native Linux scanner that captures and controls the game's window is the cleaner route.

macOS would follow the same pattern, but with no Mac client for the game there is nothing to scan; it's out of scope.

---

## Where We Are

The design already aims at this:

- `InventoryKamera.Core` (`net8.0`) defines the platform interfaces in `Abstractions/`. `IScreenCapture` and `IInputSimulator` use ImageSharp types, not Windows types.
- The Avalonia app picks implementations by OS through DI (`App.axaml.cs`): Windows services on Windows, `Services/PlatformStubs/` elsewhere.
- Scrapers and the newer readers (`*Reader.cs`) mostly go through those interfaces and plain ImageSharp code.

What still ties scanning to Windows (as of 2026-11-10):

1. **Project target.** Scrapers, readers, `InventoryScanner` and `GenshinProcesor` live in `InventoryKamera.Infrastructure`, which targets `net8.0-windows` with `UseWindowsForms`. They can't load on Linux even with Linux implementations plugged in.
2. **System.Drawing in OCR preprocessing.** `GenshinProcesor` filters and resizes with `System.Drawing.Bitmap` (`AnalyzeText(Bitmap)`, `ResizeImage`, `ScaleImage`, `ConvertToGrayscale`, `SetContrast`, `SetGamma`, `SetInvert`, `SetColor`, `SetBrightness`, `SetThreshold`, `FilterColors`, `PreProcessImage`, `KirschEdgeDetect`, `GetAverageColor`, `CompareBitmapsFast(Bitmap)`), and `MaterialCountReader.ReadCount` takes a `Bitmap`. `WinFormsImageProcessor` implements `IImageProcessor` the same way.
3. **Direct Windows calls that bypass the interfaces.**
   - `CharacterScraper` presses ESC twice through `Navigation.sim` (the Windows input library) instead of `IInputSimulator`.
   - `GenshinProcesor` (card comparison helper) calls the static `Navigation` for size, aspect ratio, capture and cursor.
   - `ArtifactScraper` asks `Navigation.GetAspectRatio()` instead of `IScreenCapture.IsNormal`.
4. **`Windows/Navigation.cs`** (static, ~660 lines) holds window lookup, capture, input and aspect ratio handling. `WindowsScreenCapture` and `WindowsInputSimulator` are thin wrappers around it.
5. **Tesseract.** The `TesseractOCR` NuGet package probably ships only Windows native binaries (to check). Linux would use the system `tesseract`/`leptonica` libraries, possibly through a different wrapper.
6. **Tests.** `InventoryKamera.Infrastructure.Tests` targets `net8.0-windows`.

---

## Step 1: Platform-independent scanner

Worth doing even without Linux: cleaner, more testable code, and widescreen (21:9) support touches the same code.

- **New project** `InventoryKamera.Scanning` (`net8.0`, name to decide): scrapers, readers, `InventoryScanner`, `InventoryPaging`, `GenshinProcesor`, `OcrEnginePool` (or an OCR abstraction). Depends on Core and ImageSharp only.
- **Infrastructure** shrinks to the Windows implementations: `WindowsScreenCapture`, `WindowsInputSimulator`, `WindowsScanRunner` wiring, `Navigation`. Rename to `InventoryKamera.Windows` at the same time (optional).
- **OCR preprocessing to ImageSharp:** port the `Bitmap` filters in `GenshinProcesor` and `MaterialCountReader.ReadCount` to `Image<Rgba32>`; replace `WinFormsImageProcessor` with an ImageSharp implementation.
- **Route the bypasses through the interfaces:** add e.g. `PressEscape()` to `IInputSimulator`; use `IScreenCapture` for size, aspect ratio and capture everywhere; remove the remaining static `Navigation` calls from scanning code.
- **Tests:** move reader tests to a `net8.0` test project so they run on Linux too.

**Risk:** OCR accuracy. Porting the filters can shift pixel values slightly (resampling, grayscale weights, rounding), and Tesseract is sensitive to that. Guard it:

1. Before porting, record the current results on saved crops (the `TestData/` sets plus a full scan's `logging/` folder) as a baseline.
2. Port one filter chain at a time and compare against the baseline; the reader tests must stay green.
3. Finish with a full scan at Fast speed and compare the export with the last verified one (characters, weapons, 684 artifacts with locks, materials).

**Done when:** the scanning project targets `net8.0` with no System.Drawing or WinForms references; Windows scans produce the same export as before; all tests pass.

---

## Step 2: Linux implementations

The game runs under Wine/Proton, normally as an X11 window (under XWayland on a Wayland desktop).

| Piece | Approach | Notes |
|-------|----------|-------|
| Window lookup | X11: find the window by title ("Genshin Impact"), like `xdotool search --name` | Through Xlib/XCB via P/Invoke or a small binding library |
| Screen capture | X11: `XGetImage`/XShm on the game window | Works for XWayland windows. Native Wayland capture would need the ScreenCast portal (permission prompt, PipeWire); avoid unless needed |
| Input | XTest (X11) for clicks, moves, scroll and keys; fall back to a virtual device through `/dev/uinput` (what `ydotool` uses) | uinput works on X11 and Wayland but needs a one-time permission (udev rule or `input` group). Check whether the game under Wine accepts XTest input |
| OCR | System Tesseract (`pacman -S tesseract`) with our `tessdata/` trained data | Check the wrapper loads the system library |
| Elevation | Not needed on Linux | The Windows manifest's admin request doesn't apply |

Implement as `LinuxScreenCapture`, `LinuxInputSimulator` (and a window finder) in a `net8.0` project, e.g. `InventoryKamera.Linux`.

**Test machine:** the maintainer's CachyOS laptop (Arch-based) with the game under Wine/Proton.

**Done when:** a full scan on Linux produces the same export as on Windows for the same account (allowing for in-game changes between scans).

---

## Step 3: Wire up

- In `App.axaml.cs`, register the Linux services instead of the stubs when `OperatingSystem.IsLinux()`.
- Show the "Windows only" warning only where scanning is still unsupported (macOS).
- README: Linux setup (Wine/Proton, Tesseract package, uinput permission if used).

---

## Milestone 2.5 Complete When

- [ ] Scanning code builds for `net8.0` with no System.Drawing or WinForms references
- [ ] Windows scan export unchanged after the port (full Fast scan compared with the last verified export)
- [ ] Reader tests run on Linux
- [ ] Linux capture, input and window lookup implemented
- [ ] Full scan on Linux matches a Windows scan of the same account
- [ ] README documents Linux setup

---

*Last updated: 2026-11-10*
