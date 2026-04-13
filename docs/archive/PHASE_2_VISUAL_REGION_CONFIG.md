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

**Note:** Phase 1.5 already externalizes navigation regions as boxes (x, y, width, height) and captures them to `./logging/menus/` for visual feedback. Phase 2 adds a visual editor for these existing regions.

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

### Phase 2.3 (Conditional Region System)
- Conditional region shifting rules
- Detection region configuration
- Visual rule editor
- Rule testing and validation

### Phase 3+ (Future Enhancements)
- Automatic region detection (computer vision to find UI elements)
- Profile sharing marketplace (central repository with ratings/reviews)
- A/B testing (compare two profiles side-by-side)

---

## Conditional Region Shifting System

### Problem Statement

Genshin Impact occasionally adds new UI elements that shift the positions of other elements on specific cards. For example:

**Sanctified Artifacts:**
- Added in version 4.x
- Display a special "sanctified" indicator on the card
- This indicator shifts substats and other regions downward
- Only appears on specific artifact sets

**Current Pain Point:**
Without conditional rules, supporting these variations requires:
1. Duplicate region definitions (normal artifacts vs sanctified artifacts)
2. Manual detection by user (select different profile for sanctified items)
3. Maintenance nightmare (any region adjustment needs updating in multiple places)

**Desired Solution:**
Define regions once, with conditional rules that apply coordinate shifts when specific UI elements are detected.

---

### Feature Scope

**In Scope:**
- Define detection regions (areas to check for UI elements)
- Specify conditions (element exists, color match, image match)
- Apply coordinate shifts to dependent regions
- Visual editor for creating/editing rules
- Test rules against sample screenshots

**Out of Scope (Phase 3+):**
- ❌ Machine learning-based detection
- ❌ Automatic rule generation
- ❌ Complex Boolean logic (AND/OR combinations)

---

### ScanProfile.json Schema Extension

```json
{
  "version": "2.0",
  "profiles": {
    "16-9-1920x1080": {
      "name": "Standard 16:9 (1920x1080)",
      "aspectRatio": 1.7778,
      "artifacts": {
        "card": { "x": 0.7250, "y": 0.1556, "width": 0.2635, "height": 0.7778 },
        "name": { "x": 0.0911, "y": 0.0614, "width": 0.6719, "height": 0.0965 },
        "mainStat": { "x": 0.075, "y": 0.185, "width": 0.250, "height": 0.045 },
        "substats": { "x": 0.075, "y": 0.520, "width": 0.850, "height": 0.180 },

        "detectionRegions": {
          "sanctifyIndicator": { "x": 0.82, "y": 0.15, "width": 0.12, "height": 0.08 }
        },

        "conditionalShifts": [
          {
            "name": "Sanctified Artifact Adjustment",
            "description": "Shifts substats/mainstat down when sanctified indicator is present",
            "detectionRegion": "sanctifyIndicator",
            "condition": {
              "type": "colorMatch",
              "targetColor": "#FFD700",
              "tolerance": 20,
              "minMatchPercent": 15
            },
            "applyToRegions": ["substats", "mainStat"],
            "shift": { "x": 0, "y": 0.052 }
          }
        ]
      },
      "weapons": { /* ... */ }
    }
  }
}
```

---

### Detection Methods

#### 1. Color Match Detection

Check if a specific color appears in the detection region.

**Use Case:** Sanctified indicator has distinctive gold/yellow color

**Parameters:**
- `targetColor`: Hex color code (e.g., "#FFD700")
- `tolerance`: RGB distance tolerance (0-255)
- `minMatchPercent`: Minimum percentage of pixels that must match (0-100)

**Implementation:**
```csharp
public bool DetectColorMatch(Bitmap region, ColorMatchCondition condition)
{
    int matchingPixels = 0;
    int totalPixels = region.Width * region.Height;
    Color target = ColorTranslator.FromHtml(condition.TargetColor);

    for (int y = 0; y < region.Height; y++)
    {
        for (int x = 0; x < region.Width; x++)
        {
            Color pixel = region.GetPixel(x, y);
            int distance = Math.Abs(pixel.R - target.R) +
                          Math.Abs(pixel.G - target.G) +
                          Math.Abs(pixel.B - target.B);

            if (distance <= condition.Tolerance)
            {
                matchingPixels++;
            }
        }
    }

    double matchPercent = (matchingPixels / (double)totalPixels) * 100;
    return matchPercent >= condition.MinMatchPercent;
}
```

#### 2. Image Template Matching

Check if a reference image appears in the detection region.

**Use Case:** Detect specific icon or text pattern

**Parameters:**
- `templatePath`: Path to reference image file
- `minConfidence`: Minimum match confidence (0.0-1.0)

**Implementation:**
```csharp
public bool DetectImageMatch(Bitmap region, ImageMatchCondition condition)
{
    Bitmap template = LoadTemplate(condition.TemplatePath);
    var matcher = new ExhaustiveTemplateMatching(condition.MinConfidence);
    var matches = matcher.ProcessImage(region, template);

    return matches.Length > 0;
}
```

#### 3. Brightness/Contrast Detection

Check if region is significantly brighter/darker than expected.

**Use Case:** Detect presence of glowing effects or overlays

**Parameters:**
- `minBrightness`: Minimum average brightness (0-255)
- `maxBrightness`: Maximum average brightness (0-255)

---

### Coordinate Shift Application

When a condition is met, apply shifts to dependent regions:

```csharp
public Rectangle ApplyConditionalShifts(
    string regionName,
    Rectangle baseRegion,
    Bitmap screenshot,
    ScanProfile profile)
{
    Rectangle adjustedRegion = baseRegion;

    foreach (var rule in profile.ConditionalShifts)
    {
        // Skip if this rule doesn't apply to current region
        if (!rule.ApplyToRegions.Contains(regionName))
            continue;

        // Get detection region
        var detectionRegionDef = profile.DetectionRegions[rule.DetectionRegion];
        Rectangle detectionRect = ConvertToPixels(detectionRegionDef, screenshot.Size);
        Bitmap detectionBitmap = CropBitmap(screenshot, detectionRect);

        // Check condition
        bool conditionMet = false;
        switch (rule.Condition.Type)
        {
            case "colorMatch":
                conditionMet = DetectColorMatch(detectionBitmap, rule.Condition);
                break;
            case "imageMatch":
                conditionMet = DetectImageMatch(detectionBitmap, rule.Condition);
                break;
            case "brightness":
                conditionMet = DetectBrightness(detectionBitmap, rule.Condition);
                break;
        }

        // Apply shift if condition met
        if (conditionMet)
        {
            int shiftX = (int)(rule.Shift.X * screenshot.Width);
            int shiftY = (int)(rule.Shift.Y * screenshot.Height);

            adjustedRegion.X += shiftX;
            adjustedRegion.Y += shiftY;

            Logger.Debug($"Applied conditional shift '{rule.Name}' to region '{regionName}': " +
                        $"x+={shiftX}, y+={shiftY}");
        }

        detectionBitmap.Dispose();
    }

    return adjustedRegion;
}
```

---

### Visual Editor UI

#### Rule Management Panel

```
┌────────────────────────────────────────────────────────────────┐
│ Conditional Region Rules                                       │
├────────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────────┐   │
│ │ ☑ Sanctified Artifact Adjustment                         │   │
│ │   Detection: sanctifyIndicator (color match: #FFD700)    │   │
│ │   Applies to: substats, mainStat                         │   │
│ │   Shift: x=0, y=+0.052                                   │   │
│ │   [Edit] [Test] [Delete]                                 │   │
│ └──────────────────────────────────────────────────────────┘   │
│                                                                 │
│ [+ Add New Rule]                                                │
└────────────────────────────────────────────────────────────────┘
```

#### Rule Editor Dialog

```
┌────────────────────────────────────────────────────────────────┐
│ Edit Conditional Rule                             [─][□][×]   │
├────────────────────────────────────────────────────────────────┤
│ Rule Name: [Sanctified Artifact Adjustment            ]        │
│                                                                 │
│ Description:                                                    │
│ ┌────────────────────────────────────────────────────────────┐ │
│ │Shifts substats/mainstat down when sanctified indicator   │ │
│ │is present on artifact cards.                              │ │
│ └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Detection Region: [sanctifyIndicator ▼]  [Define New...]       │
│                                                                 │
│ Condition Type: [Color Match ▼]                                │
│   Target Color: [#FFD700] [Pick from Screenshot]               │
│   Tolerance: [──●────────] 20                                  │
│   Min Match %: [────────●─] 15%                                │
│                                                                 │
│ Apply Shift To:                                                 │
│   ☑ substats                                                    │
│   ☑ mainStat                                                    │
│   ☐ level                                                       │
│   ☐ rarity                                                      │
│                                                                 │
│ Shift Amount:                                                   │
│   X: [0.000  ] (0 pixels at 1920x1080)                         │
│   Y: [0.052  ] (100 pixels at 1920x1080)                       │
│                                                                 │
│ [Test Rule on Current Screenshot]                              │
│                                                                 │
│               [Cancel]  [Save Rule]                             │
└────────────────────────────────────────────────────────────────┘
```

#### Rule Testing Workflow

1. **Load sample screenshot** (artifact card with sanctified indicator)
2. **Click "Test Rule"** button
3. **Visual feedback:**
   - Detection region highlighted in yellow
   - Overlay shows "CONDITION MET" or "CONDITION NOT MET"
   - Affected regions shown in green (original) and orange (shifted)
   - Side-by-side OCR preview of original vs shifted regions

4. **Adjust parameters** if detection fails:
   - Increase tolerance if too strict
   - Adjust minMatchPercent if false positives/negatives
   - Reposition detection region if wrong area

---

### Integration with Scanner

Modify scraper classes to apply conditional shifts:

```csharp
// In ArtifactScraper.cs

Bitmap GetSubstatsBitmap(Bitmap card)
{
    // Load base region definition from profile
    var baseRegion = _profile.Artifacts.Substats;
    Rectangle rect = ConvertToPixels(baseRegion, card.Size);

    // Apply any conditional shifts
    rect = ApplyConditionalShifts("substats", rect, card, _profile);

    return GenshinProcesor.CopyBitmap(card, rect);
}
```

This ensures all conditional rules are evaluated before cropping regions for OCR.

---

### Debugging and Logging

When conditional rules are applied, log detailed information:

```
[DEBUG] Evaluating conditional rules for region 'substats'
[DEBUG] Rule 'Sanctified Artifact Adjustment': Testing detection region 'sanctifyIndicator'
[DEBUG] Color match detection: Found 18.3% gold pixels (threshold: 15%), MATCH
[DEBUG] Applied shift to 'substats': x+=0, y+=100 pixels
[DEBUG] Final region: (144, 600, 1632, 230) -> (144, 700, 1632, 230)
```

Save detection region screenshots to `./logging/conditions/`:
```
./logging/conditions/
├── artifact_42_sanctifyIndicator_MATCH.png
├── artifact_43_sanctifyIndicator_NO_MATCH.png
└── ...
```

---

### Example Use Cases

#### Use Case 1: Sanctified Artifacts

**Problem:** Sanctified artifacts have gold indicator that shifts substats down

**Solution:**
```json
{
  "name": "Sanctified Indicator Shift",
  "detectionRegion": "sanctifyIndicator",
  "condition": {
    "type": "colorMatch",
    "targetColor": "#FFD700",
    "tolerance": 20,
    "minMatchPercent": 15
  },
  "applyToRegions": ["substats", "mainStat"],
  "shift": { "x": 0, "y": 0.052 }
}
```

#### Use Case 2: Equipped Weapon Character Portrait

**Problem:** Equipped weapons show character portrait that may overlap refinement

**Solution:**
```json
{
  "name": "Equipped Character Portrait Shift",
  "detectionRegion": "characterPortrait",
  "condition": {
    "type": "brightness",
    "minBrightness": 80,
    "maxBrightness": 255
  },
  "applyToRegions": ["refinement"],
  "shift": { "x": 0.08, "y": 0 }
}
```

#### Use Case 3: Future UI Changes

**Problem:** Genshin Impact version 7.x adds new indicator

**Solution:**
User creates custom rule in visual editor:
1. Load screenshot with new indicator
2. Define detection region by dragging box
3. Select detection method (color/image/brightness)
4. Test on multiple samples
5. Save and share profile with community

---

### Benefits

**For Users:**
- ✅ Single profile handles UI variations
- ✅ No manual switching between profiles
- ✅ Self-service when game updates UI

**For Maintainers:**
- ✅ Community can create rules for new UI elements
- ✅ No code changes needed for UI variations
- ✅ Easier debugging (visual feedback on detection)

**For Game Updates:**
- ✅ Adapt to UI changes without waiting for official release
- ✅ Share rules via community profile repository

---

### Implementation Considerations

**Performance:**
- Conditional detection adds overhead to each scan
- Cache detection results within same screenshot
- Skip disabled rules
- Optimize color matching with unsafe code/pointers

**Reliability:**
- Test rules against multiple sample screenshots
- Provide visual feedback on detection success/failure
- Log detection regions to `./logging/conditions/` for debugging

**User Experience:**
- Provide presets for known UI variations (sanctified, etc.)
- Clear error messages when detection fails
- Tutorial/documentation on creating rules

---

### Phase 2 Complete When:
- [ ] Users can load weapon/artifact card screenshot
- [ ] Users can see visual overlays for all OCR regions
- [ ] Users can drag region corners/edges to adjust
- [ ] Users can test OCR on individual regions
- [ ] Users can save custom profile to ScanProfile.json
- [ ] Users can export/import profiles for sharing
- [ ] Documentation/tutorial on creating custom profiles
- [ ] At least 3 community-contributed ultrawide profiles

### Phase 2.3 (Conditional Region System) Complete When:
- [ ] Users can define detection regions in visual editor
- [ ] Users can create conditional shift rules with color/image/brightness detection
- [ ] Users can test rules against sample screenshots with visual feedback
- [ ] Detection regions are logged to ./logging/conditions/ for debugging
- [ ] Scanner applies conditional shifts before OCR
- [ ] At least one preset rule for sanctified artifacts included
- [ ] Documentation on creating and testing conditional rules

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
