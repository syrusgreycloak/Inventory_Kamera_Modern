# Avalonia UI Mockup - Phase 2

## Current UI Problems (v1.3.17 WinForms)

You mentioned the current UI is a "mess" - absolutely agreed! The current WinForms UI has:

- **Fixed size, non-resizable** - Cramped on modern high-DPI monitors
- **Poor information density** - Lots of wasted space in some areas, too cramped in others
- **Absolute positioning** - Everything breaks if you try to resize
- **Dated visual design** - Looks like a 2010 Windows app
- **Poor spacing** - Controls jammed together with no breathing room

### What You Do Need

You specifically called out the **lower right section** (screenshots and OCR preview) as essential. This makes sense - seeing what the scanner is processing is critical for debugging and confidence that it's working correctly.

## Proposed Avalonia UI (Phase 2)

Here's a modern, spacious layout that keeps the useful parts and fixes the cramped mess:

```
┌────────────────────────────────────────────────────────────────────┐
│ Inventory Kamera 2.0                                    [─][□][×]   │
├────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────┐  ┌────────────────────────────┐  │
│  │ Scan Categories             │  │ Scan Progress              │  │
│  │                             │  │                            │  │
│  │ ☑ Characters                │  │ ┌────────────────────────┐ │  │
│  │ ☑ Weapons                   │  │ │ Cinder City Sands     │ │  │
│  │ ☑ Artifacts                 │  │ │ Artifact #237 / 895   │ │  │
│  │ ☑ Materials                 │  │ │                        │ │  │
│  │                             │  │ │  [Preview thumbnail]   │ │  │
│  │ Filters:                    │  │ │                        │ │  │
│  │ Min Rarity:  [3★ ▼]         │  │ │                        │ │  │
│  │ Min Level:   [0 ▼]          │  │ └────────────────────────┘ │  │
│  │                             │  │                            │  │
│  │ ☑ Include equipped items    │  │ Artifacts: 237 / 895      │  │
│  │                             │  │ ████████░░░░░░░░  26%      │  │
│  │                             │  │                            │  │
│  │ Scan Speed:                 │  │ Weapons:   145 / 312      │  │
│  │ ◉ Fast  ○ Medium  ○ Slow    │  │ ███████████░░░░░  46%      │  │
│  │                             │  │                            │  │
│  │         [Scan Inventory]    │  │ Status: Scanning...       │  │
│  │         [Stop Scan]         │  │ ETA: ~5 minutes           │  │
│  └─────────────────────────────┘  └────────────────────────────┘  │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ OCR Processing Details                                       │  │
│  │                                                              │  │
│  │  Current Item: Cinder City Sands of Eon (5★)                │  │
│  │  Main Stat: DEF% 46.6%                                       │  │
│  │  Sub Stats: Flat ATK +19, Flat DEF +21, CRIT Rate +3.5%,    │  │
│  │             Flat HP +299                                     │  │
│  │  Equipped: HuTao                                             │  │
│  │  Status: ✓ Valid                                             │  │
│  │                                                              │  │
│  │  [Screenshot Preview - Resizable]                            │  │
│  │  ┌──────────────────────────────────────┐                   │  │
│  │  │                                      │                   │  │
│  │  │    [Artifact card screenshot]        │                   │  │
│  │  │                                      │                   │  │
│  │  │    (This area is what you said       │                   │  │
│  │  │     is needed - the lower right      │                   │  │
│  │  │     OCR preview section)             │                   │  │
│  │  │                                      │                   │  │
│  │  └──────────────────────────────────────┘                   │  │
│  │                                                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Console Output                                               │  │
│  │                                                              │  │
│  │ [2026-04-07 15:30:12] Starting scan...                       │  │
│  │ [2026-04-07 15:30:15] Scanning artifacts...                  │  │
│  │ [2026-04-07 15:30:16] Found 895 artifacts                    │  │
│  │ [2026-04-07 15:30:18] Scanning artifact #1...                │  │
│  │ [2026-04-07 15:30:19] ✓ Gladiator's Finale Flower (5★)      │  │
│  │ [2026-04-07 15:30:20] Scanning artifact #2...                │  │
│  │ [2026-04-07 15:30:21] ✓ Crimson Witch of Flames Sands (5★)  │  │
│  │ [2026-04-07 15:30:22] Scanning artifact #3...                │  │
│  │ [2026-04-07 15:30:23] ✓ Noblesse Oblige Plume (5★)          │  │
│  │ ⋮                                                            │  │
│  │                                                              │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                      │
│ Output: C:\Users\karlp\Downloads\inventorykamera\GenshinData\       │
│ Last scan: 2026-04-07 15:05 (895 artifacts, 312 weapons, 45 chars) │
└────────────────────────────────────────────────────────────────────┘
```

## Key Improvements Over Current UI

### 1. Resizable & Responsive
- **Fully resizable window** - No more cramped fixed size
- **Responsive layouts** - Controls resize properly with window
- **High-DPI support** - Crisp on your 1440p ultrawide monitors
- **Min width:** 1024px to prevent too-cramped state
- **Expands naturally** on your 34" curved displays

### 2. Better Information Hierarchy

**Top Section (Controls):**
- Left column: Scan settings (categories, filters, speed)
- Right column: Real-time progress with preview thumbnail
- Clear visual separation
- Breathing room between sections

**Middle Section (OCR Details):**
- **THIS IS YOUR "NEEDED" SECTION** - The lower right OCR preview you mentioned
- Shows current item being scanned
- Screenshot preview (resizable!)
- Parsed OCR results in readable format
- Validation status

**Bottom Section (Console):**
- Scrollable log output
- Timestamped entries
- Color-coded status (✓ success, ✗ error, ⚠ warning)
- Can be collapsed if you don't need it

### 3. Modern Visual Design

- **Proper spacing:** 16px padding, 8px margins
- **Readable fonts:** Default system font, proper sizes
- **Status indicators:** Icons + color for accessibility (see section below)
- **Material Design influence:** Cards, shadows, proper elevation
- **Light/Dark mode support:** Switch based on system theme

### 4. Accessibility Requirements

**Current WinForms Issue:** The lower right feedback section uses straight red/green text for errors and success messages. This is not ADA compliant and creates problems for users with color vision deficiencies (colorblindness affects ~8% of males, ~0.5% of females).

**Accessible Alternatives:**

1. **Color + Icon** (Recommended)
   - ✓ Success (with green color)
   - ⚠ Warning (with yellow/orange color)
   - ✗ Error (with red color)
   - ℹ Info (with blue color)
   - Icons convey meaning even without color

2. **Color + Text Prefix**
   - "SUCCESS: Artifact scanned"
   - "WARNING: Could not read refinement level, defaulting to R1"
   - "ERROR: Invalid weapon name"
   - Text label provides context independent of color

3. **Colorblind-Safe Palettes**
   - Use blue/orange instead of red/green
   - Ensure sufficient brightness contrast (WCAG AA: 4.5:1 ratio)
   - Test with colorblind simulation tools

4. **Alternative Visual Indicators**
   - Different background patterns or textures
   - Border styles (solid for success, dashed for warning, dotted for error)
   - Font weight (bold for errors, regular for success)

**Implementation Strategy:**
- Use icon + color combination for all status messages
- Ensure console output includes text prefixes
- Test with Windows High Contrast mode
- Validate contrast ratios meet WCAG 2.1 Level AA standards
- Consider adding colorblind mode toggle in settings

### 5. Scan Progress Enhancements

Current WinForms UI has basic counters. New UI adds:
- **Per-category progress bars** with percentages
- **Preview thumbnail** of current item (live updates)
- **ETA calculation** based on current speed
- **Current item details** (name, rarity, stats)
- **Validation status** (✓ Valid, ✗ Invalid, ⚠ Warning)

### 5. The OCR Preview Section (What You Need)

You specifically called out the **lower right section** with screenshots and OCR as needed. In the new UI:

**Current (cramped):**
- Small fixed-size PictureBox
- Can't see details clearly
- No context about what's being scanned

**New (spacious):**
- **Resizable preview area** - Drag to make bigger/smaller
- **Larger default size** - Actually see the details
- **Parsed results shown alongside** - Name, stats, validation status
- **Can zoom in** on screenshot if needed
- **Color-coded validation** - Green border if valid, red if invalid

### 6. Settings Moved to Separate Window/Tab

All the cramped settings stuff (traveler name, wanderer name, output path, database updates) moves to:
- **Settings button** → Opens separate window
- **Tabs in settings:** General, Data Sources, Advanced, About
- **Keeps main window clean** and focused on scanning

## Technical Benefits

### For You (User Experience)

- **No more squinting** at tiny controls
- **Works great on 34" ultrawide** - actually uses the space
- **Resizable everything** - Adjust to your workflow
- **Clear status** - Always know what's happening
- **Better debugging** - See exactly what OCR is reading

### For Development

- **XAML layouts** - Declarative, easy to adjust
- **Data binding** - UI updates automatically from ViewModel
- **Vector graphics** - Scales perfectly to any DPI
- **Easy theming** - Light/dark mode with minimal code
- **Cross-platform ready** - Works on Windows, macOS, Linux with same code

## Layout Alternatives

### Option 1: Horizontal Split (Above)
- Settings on left, progress on right (top)
- OCR preview and console (bottom)
- **Best for:** Ultrawide monitors

### Option 2: Vertical Split
```
┌─────────────┬──────────────────────────────┐
│ Settings    │ OCR Preview (Large)          │
│             │                              │
│ ☑ Chars     │ [Big screenshot]             │
│ ☑ Weapons   │                              │
│ ☑ Artifacts │ Current: Cinder City Sands   │
│             │ Status: ✓ Valid              │
│             │                              │
│ [Scan]      ├──────────────────────────────┤
│             │ Progress Bars                │
│             │ Artifacts: 237/895  ████░░   │
│             │ Weapons:   145/312  ███░░░   │
│             │                              │
│             ├──────────────────────────────┤
│             │ Console Output               │
│             │ [2026-04-07] Scanning...     │
└─────────────┴──────────────────────────────┘
```
**Best for:** Standard 16:9 or 16:10 displays

### Option 3: Collapsible Panels
- Settings collapse to just buttons after scan starts
- More room for OCR preview during scan
- Expand settings when you need to change something

## Mockup Tools

To prototype before coding, we could use:
- **Figma** - Web-based, collaborative
- **Avalonia Designer** - Live XAML preview in Rider/VS
- **Pencil Project** - Open source mockup tool
- **Just sketch it** - Paper/whiteboard, photo, iterate

## Your Feedback?

What layout works best for your workflow?
- Horizontal split (Option 1)?
- Vertical split (Option 2)?
- Something else?

Key questions:
1. How big do you want the screenshot preview? (I assumed resizable is fine)
2. Do you need console output always visible, or can it be collapsible?
3. Any other specific pain points with current UI to fix?

---

**Note:** This is Phase 2 work. Phase 1.5 keeps the WinForms UI but makes the backend ready for this new UI. Once Core library is extracted, building this Avalonia UI will be straightforward since all the business logic is already abstracted.
