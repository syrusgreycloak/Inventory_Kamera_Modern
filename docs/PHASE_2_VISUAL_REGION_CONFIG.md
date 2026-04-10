# Phase 2+: Visual Region Configuration Tool

**Status:** Planned (Post-Avalonia Migration)
**Dependencies:** Phase 2 Avalonia UI complete, Phase 1.5 ScanProfile.json system
**Goal:** Enable users to visually configure scan regions without editing JSON

---

## Overview

The Visual Region Configuration Tool allows users to adjust OCR region coordinates by viewing screenshots and dragging region boundaries. This eliminates trial-and-error JSON editing and enables support for non-standard resolutions (ultrawide, custom windowed sizes, etc.).

---

## Problem Statement

### Current Pain Points (Phase 1.5)

While Phase 1.5's `ScanProfile.json` unblocks users with custom resolutions, the workflow is tedious:

1. **Manual JSON editing** - Error-prone, requires understanding coordinate system
2. **No visual feedback** - Can't see what region coordinates represent
3. **Trial-and-error required** - Edit → Scan → Check screenshots → Repeat
4. **Steep learning curve** - Users must understand relative coordinates (0.0-1.0)

### Target Experience (Phase 2+)

Users should be able to:

1. **Load a reference screenshot** from their game
2. **See visual overlays** showing each OCR region (name, level, refinement, etc.)
3. **Drag region corners/edges** to adjust positioning
4. **See live OCR preview** of what Tesseract would read
5. **Save custom profile** for their resolution
6. **Share profiles** with community (export/import)

---

## Feature Scope

### In Scope

**Core Functionality:**
- Load weapon/artifact card screenshot from disk
- Display image with adjustable region overlays
- Drag corners/edges to resize regions
- Drag region center to reposition
- Live coordinate display (both absolute pixels and relative 0.0-1.0)
- Save/load ScanProfile.json
- Test OCR on current region (show what Tesseract sees)

**Profile Management:**
- Create new profile (enter name, aspect ratio)
- Edit existing profile
- Duplicate profile (create variant)
- Export profile to file
- Import profile from file
- Delete custom profile (keep built-in profiles read-only)

**Navigation Regions:**
- Define clickable UI positions (inventory button, tabs, etc.)
- Test clicks by simulating on screenshot overlay
- Validate navigation flow (can reach all menu locations)

### Out of Scope (Future Enhancements)

- ❌ Automatic region detection (ML/CV to find regions)
- ❌ Multi-screenshot calibration (averaging regions across multiple samples)
- ❌ OCR engine fine-tuning (adjusting Tesseract parameters)
- ❌ Built-in screenshot capture (user provides their own screenshots)

---

## User Interface Design

### Main Window Layout

```
┌────────────────────────────────────────────────────────────────┐
│ Region Configuration Tool                          [─][□][×]   │
├────────────────────────────────────────────────────────────────┤
│ ┌──────────────┬───────────────────────────────────────────┐   │
│ │ Profiles     │ Screenshot Preview                        │   │
│ │              │ ┌───────────────────────────────────────┐ │   │
│ │ [16:9 ▼]     │ │                                       │ │   │
│ │              │ │   [Weapon Card Screenshot]            │ │   │
│ │ Regions:     │ │                                       │ │   │
│ │ ☑ Name       │ │   ┌────────────────┐                │ │   │
│ │ ☑ Level      │ │   │ Name Region    │ ◄── Draggable  │ │   │
│ │ ☑ Refinement │ │   └────────────────┘                │ │   │
│ │ ☐ Rarity     │ │   ┌──┐                              │ │   │
│ │ ☐ Locked     │ │   │LV│ ◄── Level region             │ │   │
│ │ ☐ Equipped   │ │   └──┘                              │ │   │
│ │              │ │   ┌─┐                               │ │   │
│ │ [Load Image] │ │   │2│ ◄── Refinement region         │ │   │
│ │ [Test OCR]   │ │   └─┘                               │ │   │
│ │ [Save]       │ │                                       │ │   │
│ │ [Export]     │ │   [Zoom: 100% ─●─ 200%]              │ │   │
│ └──────────────│ └───────────────────────────────────────┘ │   │
│                │                                           │   │
│                │ Selected: Refinement Region               │   │
│                │ ┌───────────────────────────────────────┐ │   │
│                │ │ X: 0.061  Y: 0.421                    │ │   │
│                │ │ Width: 0.065  Height: 0.033           │ │   │
│                │ │ Pixels: (98, 338) - (202, 396)        │ │   │
│                │ └───────────────────────────────────────┘ │   │
│                │                                           │   │
│                │ OCR Test Results:                         │   │
│                │ ┌───────────────────────────────────────┐ │   │
│                │ │ Raw output: "2°"                      │ │   │
│                │ │ Parsed: 2                             │ │   │
│                │ │ Valid: ✓                              │ │   │
│                │ └───────────────────────────────────────┘ │   │
└────────────────┴───────────────────────────────────────────────┘
```

### Interaction Design

**Region Selection:**
- Click region overlay to select (highlights border)
- Selected region shows resize handles at corners
- Drag corner handles to resize
- Drag region body to move
- Arrow keys for fine adjustment (1px increments)
- Shift+Arrow for coarse adjustment (10px increments)

**Region Visibility:**
- Toggle individual regions on/off (checkboxes in sidebar)
- Color-coded overlays (green=name, blue=level, red=refinement, etc.)
- Semi-transparent overlays (can see screenshot beneath)
- Selected region overlay brighter/opaque

**OCR Testing:**
- Select region → Click "Test OCR" button
- Shows preprocessed image (grayscale + inverted) in popup
- Displays raw Tesseract output
- Displays parsed/validated result
- Highlights if validation would fail

**Profile Workflow:**
1. Create new profile: "New Profile" button → Enter name, aspect ratio → Blank regions
2. Load reference screenshot: "Load Image" → Select weapon/artifact card from disk
3. Adjust regions: Drag overlays to position correctly
4. Test: Click "Test OCR" on each region to verify
5. Save: "Save" button → Updates ScanProfile.json
6. Export: "Export" button → Save profile to shareable file

---

## Technical Implementation

### Technology Stack

**UI Framework:** Avalonia UI (cross-platform)
**Image Display:** Avalonia.Skia for rendering
**Region Overlays:** Custom canvas with adorner pattern
**OCR Integration:** Use existing `IOcrEngine` abstraction from Phase 1.5

### Architecture

```
InventoryKamera.RegionConfig/      # New Avalonia project
├── ViewModels/
│   ├── RegionConfigViewModel.cs   # Main view model
│   ├── RegionOverlayViewModel.cs  # Individual region overlay
│   └── ProfileManagerViewModel.cs # Profile CRUD operations
├── Views/
│   ├── RegionConfigWindow.axaml   # Main window
│   ├── RegionCanvas.axaml         # Screenshot + overlays
│   └── OcrTestDialog.axaml        # OCR test results popup
├── Models/
│   ├── RegionDefinition.cs        # Represents single region (x, y, w, h)
│   └── ScanProfile.cs             # Full profile (all regions)
└── Services/
    ├── ProfileSerializer.cs       # Load/save ScanProfile.json
    └── RegionValidator.cs         # Validate regions within bounds
```

### ScanProfile.json Schema (Extended)

```json
{
  "version": "2.0",
  "profiles": {
    "custom-ultrawide-3440x1440": {
      "name": "Custom Ultrawide 21:9 (3440x1440)",
      "aspectRatio": 2.3889,
      "author": "CommunityUser123",
      "created": "2026-04-10T12:00:00Z",
      "artifacts": {
        "card": { "x": 0.7250, "y": 0.1556, "width": 0.2635, "height": 0.7778 },
        "name": { "x": 0.0911, "y": 0.0614, "width": 0.6719, "height": 0.0965 },
        "refinement": { "x": 0.061, "y": 0.421, "width": 0.065, "height": 0.033 }
      },
      "weapons": { /* ... */ },
      "navigation": {
        "inventoryButton": { "x": 0.5, "y": 0.8 },
        "weaponTab": { "x": 0.3, "y": 0.15 },
        "artifactTab": { "x": 0.4, "y": 0.15 }
      }
    }
  }
}
```

### Region Overlay Rendering

**Avalonia Canvas approach:**
```csharp
public class RegionOverlay : Control
{
    public override void Render(DrawingContext context)
    {
        var rect = new Rect(X * ImageWidth, Y * ImageHeight,
                            Width * ImageWidth, Height * ImageHeight);

        var brush = IsSelected
            ? new SolidColorBrush(Colors.Yellow, 0.5)
            : new SolidColorBrush(RegionColor, 0.3);

        context.FillRectangle(brush, rect);
        context.DrawRectangle(new Pen(RegionColor, 2), rect);

        // Draw resize handles if selected
        if (IsSelected)
        {
            DrawResizeHandles(context, rect);
        }
    }
}
```

### OCR Testing Integration

Reuse existing infrastructure from Phase 1.5:
```csharp
public async Task<OcrTestResult> TestRegionAsync(RegionDefinition region)
{
    // 1. Crop region from loaded screenshot
    var croppedImage = await _imageProcessor.CropAsync(_screenshot, region.ToRectangle());

    // 2. Apply preprocessing (grayscale, invert)
    var processed = await _imageProcessor.ApplyGrayscaleAsync(croppedImage);
    processed = await _imageProcessor.ApplyInvertAsync(processed);

    // 3. Run OCR
    var ocrResult = await _ocrEngine.RecognizeAsync(processed);

    // 4. Validate parsed result
    var parsed = ParseRefinement(ocrResult.Text); // Example for refinement region
    var isValid = parsed >= 1 && parsed <= 5;

    return new OcrTestResult
    {
        RawText = ocrResult.Text,
        ParsedValue = parsed,
        IsValid = isValid,
        PreprocessedImage = processed
    };
}
```

---

## Navigation Region Configuration

### Extended Scope

In addition to OCR scan regions, the tool should allow configuring navigation coordinates:

**UI Elements to Configure:**
- Main menu button positions (Inventory, Character, Map, etc.)
- Inventory tabs (Weapons, Artifacts, Materials, etc.)
- Sort dropdown location
- Confirmation button positions
- Item grid starting position

**Configuration Workflow:**
1. Load full-screen game screenshot (Paimon menu open)
2. Click "Add Navigation Point" button
3. Click on screenshot where button should be clicked
4. Name the navigation point ("InventoryButton", "WeaponTab", etc.)
5. Repeat for all navigation points
6. Test navigation flow: Simulated clicks overlaid on screenshot

**Use Case:**
- **Ultrawide users:** Menu buttons in different positions due to aspect ratio
- **Windowed mode:** Different absolute positions
- **Future game updates:** UI layout changes

---

## Community Profile Sharing

### Export/Import Workflow

**Export:**
1. User creates custom profile for their resolution
2. Click "Export Profile" button
3. Save as `ultrawide-3440x1440.scanprofile.json`
4. Share on GitHub, Discord, Reddit

**Import:**
1. User downloads `.scanprofile.json` from community
2. Click "Import Profile" button
3. Select file → Profile added to `ScanProfile.json`
4. Available in profile dropdown

**Built-in Profile Protection:**
- Default profiles (16:9, 16:10) are read-only
- Users can duplicate and modify, but not overwrite originals
- Custom profiles can be deleted

### Profile Repository (Future)

Could maintain community-contributed profiles in GitHub repo:
```
InventoryKamera/
└── profiles/
    ├── 16-9/             # Built-in
    ├── 16-10/            # Built-in
    ├── 21-9-ultrawide/   # Community
    ├── 32-9-super-ultrawide/
    └── steam-deck/
```

Users can pull latest community profiles or submit their own via PR.

---

## Benefits

### For Users
- ✅ No JSON editing required
- ✅ Visual feedback on what regions capture
- ✅ Immediate validation via OCR testing
- ✅ Support any resolution/aspect ratio
- ✅ Fix misaligned regions in minutes instead of hours

### For Ultrawide/Custom Resolution Users
- ✅ Can create working profiles themselves
- ✅ Share profiles with others using same resolution
- ✅ No waiting for maintainer to add support

### For Maintainers
- ✅ Community self-service for custom resolutions
- ✅ Less support burden ("why isn't my ultrawide working?")
- ✅ Easier to debug user issues ("export your profile and share it")

### For Game Updates
- ✅ When Genshin Impact updates UI layout, users can fix it themselves
- ✅ Community shares updated profiles faster than waiting for official release

---

## Implementation Priority

### Phase 2 (Critical Path)
- Basic region editor for OCR regions (weapon, artifact)
- Load/save ScanProfile.json
- Visual overlay rendering with drag handles

### Phase 2.1 (High Priority)
- OCR testing integration
- Profile export/import
- Navigation region configuration

### Phase 2.2 (Nice to Have)
- Multi-region batch testing (test all regions at once)
- Profile validation (check for overlapping regions, out of bounds, etc.)
- Preset templates (start from 16:9, adjust for ultrawide)
- Undo/redo for region adjustments

### Phase 3+ (Future Enhancements)
- Automatic region detection (computer vision to find UI elements)
- Profile sharing marketplace (central repository with ratings/reviews)
- A/B testing (compare two profiles side-by-side)

---

## Success Criteria

### Phase 2 Complete When:
- [ ] Users can load weapon/artifact card screenshot
- [ ] Users can see visual overlays for all OCR regions
- [ ] Users can drag region corners/edges to adjust
- [ ] Users can test OCR on individual regions
- [ ] Users can save custom profile to ScanProfile.json
- [ ] Users can export/import profiles for sharing
- [ ] Documentation/tutorial on creating custom profiles
- [ ] At least 3 community-contributed ultrawide profiles

### Non-Goals:
- ❌ Built-in screenshot capture (user provides screenshots)
- ❌ Automatic region detection (manual adjustment only)
- ❌ Real-time scanning (test mode only, not live game)

---

## Related Documents

- `PHASE_1.5_PLAN.md` - ScanProfile.json foundation
- `PHASE_2_AVALONIA.md` - Avalonia UI migration plan
- `MODERNIZATION_PLAN.md` - Overall modernization roadmap

---

**Last Updated:** 2026-04-10
**Status:** Draft - Planned for Phase 2+
