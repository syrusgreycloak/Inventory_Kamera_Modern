# Inventory Kamera Documentation

This directory contains planning documents and architecture diagrams for Inventory Kamera modernization.

## Overview

Inventory Kamera is evolving from a Windows-only WinForms application to a cross-platform tool with modern architecture. This documentation captures the current state, planned improvements, and migration strategy.

## Documents

### Planning Documents

- **[PHASE_1.5_PLAN.md](PHASE_1.5_PLAN.md)** - Detailed plan for extracting Core library and preparing for cross-platform support
- **[MODERNIZATION_PLAN.md](../MODERNIZATION_PLAN.md)** - High-level roadmap for Phase 1 (configurable data sources) and Phase 2 (Avalonia UI)

### Architecture Diagrams (PlantUML)

All diagrams are in the `plantuml/` directory. To view them:

1. **Online:** Use [PlantUML Web Server](http://www.plantuml.com/plantuml/uml/)
2. **VSCode:** Install "PlantUML" extension
3. **Rider:** PlantUML support built-in
4. **Command line:** Install PlantUML and run `plantuml *.puml`

#### Diagram Index

| Diagram | File | Description |
|---------|------|-------------|
| **Current Class Diagram** | [01-current-class-diagram.puml](plantuml/01-current-class-diagram.puml) | Shows existing class relationships in v1.3.17 with identified problems |
| **Proposed Class Diagram** | [02-proposed-class-diagram.puml](plantuml/02-proposed-class-diagram.puml) | Target Phase 1.5 architecture with abstractions and DI |
| **Current Sequence Diagram** | [03-current-sequence-diagram.puml](plantuml/03-current-sequence-diagram.puml) | Artifact scanning workflow in current implementation |
| **Proposed Sequence Diagram** | [04-proposed-sequence-diagram.puml](plantuml/04-proposed-sequence-diagram.puml) | Improved async workflow with abstractions |
| **Component Diagram** | [05-component-diagram.puml](plantuml/05-component-diagram.puml) | Layered architecture showing Core, Infrastructure, and UI layers |
| **Activity Diagram** | [06-activity-diagram.puml](plantuml/06-activity-diagram.puml) | Complete scanning workflow from start to GOOD export |
| **Deployment Diagram** | [07-deployment-diagram.puml](plantuml/07-deployment-diagram.puml) | Cross-platform deployment on Windows, macOS, Linux |
| **State Diagram** | [08-state-diagram.puml](plantuml/08-state-diagram.puml) | Scanning process state machine |
| **OCR Engine Pool Pattern** | [09-ocr-engine-pool-pattern.puml](plantuml/09-ocr-engine-pool-pattern.puml) | Thread-safe engine pool implementation |
| **Data Flow Diagram** | [10-data-flow-diagram.puml](plantuml/10-data-flow-diagram.puml) | Data flow from game to GOOD JSON export |
| **Before/After Comparison** | [11-before-after-comparison.puml](plantuml/11-before-after-comparison.puml) | Side-by-side comparison of current vs Phase 1.5 |
| **Error Handling Flow** | [12-error-handling-flow.puml](plantuml/12-error-handling-flow.puml) | Error handling and cancellation strategies |

## Quick Start Guide to Diagrams

### For Understanding Current Architecture

Start with these diagrams to understand how the current codebase works:

1. **01-current-class-diagram.puml** - See all classes and their relationships
2. **03-current-sequence-diagram.puml** - Follow the artifact scanning workflow
3. **09-ocr-engine-pool-pattern.puml** - Understand multi-threading architecture

### For Planning Phase 1.5

These diagrams show the target architecture:

1. **11-before-after-comparison.puml** - High-level comparison of current vs proposed
2. **02-proposed-class-diagram.puml** - See how abstractions simplify the design
3. **05-component-diagram.puml** - Understand layer separation

### For Implementation

Reference these during coding:

1. **04-proposed-sequence-diagram.puml** - Async workflow patterns
2. **12-error-handling-flow.puml** - Error handling and cancellation
3. **06-activity-diagram.puml** - Complete scanning logic
4. **08-state-diagram.puml** - State management

### For Cross-Platform Planning (Phase 2)

1. **07-deployment-diagram.puml** - See how platform-specific implementations work
2. **05-component-diagram.puml** - Note the abstraction layer that enables cross-platform

## Key Architectural Concepts

### Current Architecture (v1.3.17)

- **Monolithic:** Business logic tightly coupled to WinForms UI
- **Platform-specific:** System.Drawing, Graphics.CopyFromScreen (Windows-only)
- **Hard-coded:** Scan regions defined in code (magic numbers like 0.4216)
- **Static dependencies:** Navigation, Scraper, GenshinProcessor (untestable)
- **Threading issues:** Tesseract 5.2.0 deadlocks, 30-second timeout workaround

### Phase 1.5 Target Architecture

- **Layered:** Core (business logic) + Infrastructure (platform-specific) + UI (presentation)
- **Platform-agnostic Core:** No Windows-specific dependencies, uses abstractions
- **External configuration:** ScanProfile.json for region coordinates
- **Dependency Injection:** Constructor injection, testable with mocks
- **Improved threading:** Better OCR engine management, async/await patterns

### Abstractions

Core library defines interfaces that Infrastructure implements:

- **IScreenCapture** - Platform-specific screen capture (Windows: Graphics.CopyFromScreen, macOS: CGWindowListCreateImage, Linux: X11/Wayland)
- **IImageProcessor** - Cross-platform image processing (ImageSharp)
- **IOcrEngine** - OCR abstraction (Tesseract wrapper)
- **IInputSimulator** - Platform-specific input (Windows: WindowsInput, macOS: Core Graphics, Linux: xdotool)

### Configuration

Externalizing hard-coded data:

- **ScanProfile.json** - Scan region coordinates for different resolutions (16:9, 16:10, Steam Deck)
- **datasources.json** - GitHub repository URLs (from Phase 1)
- **inventorylists/*.json** - Game data mappings (existing)

## Migration Strategy

Phase 1.5 is incremental and non-breaking:

1. **Extract Core library** - Move business logic to .NET 8 class library
2. **Define abstractions** - Create interfaces for platform services
3. **Implement Infrastructure** - Windows implementations of abstractions
4. **Adapt WinForms** - Update existing UI to use Core via DI
5. **Test thoroughly** - Ensure identical behavior to v1.3.17
6. **Prepare for Phase 2** - Core library ready for Avalonia UI

## Testing Strategy

### Unit Tests (Core Library)

- Test business logic without UI or game
- Use mock implementations of abstractions
- Fast, isolated, repeatable

Example:
```csharp
var mockCapture = new MockScreenCapture("./testdata/");
var mockOcr = new MockOcrEngine();
var scraper = new ArtifactScraper(mockCapture, mockOcr, ...);

var artifacts = await scraper.ScanArtifactsAsync();

Assert.Equal(5, artifacts.Count);
Assert.True(artifacts[0].IsValid());
```

### Integration Tests

- Use real implementations (WindowsScreenCapture, TesseractOcrEngine)
- Test with pre-captured screenshots
- Verify OCR accuracy, performance

### Manual Testing

- Test with actual Genshin Impact game
- Verify UI responsiveness
- Check GOOD export compatibility with Genshin Optimizer

## Contributing

When making architectural changes:

1. **Update diagrams** - Keep PlantUML diagrams in sync with code
2. **Update PHASE_1.5_PLAN.md** - Document decisions and progress
3. **Add tests** - Unit tests for Core, integration tests for Infrastructure
4. **Document breaking changes** - Update migration notes

## References

- [PlantUML Documentation](https://plantuml.com/)
- [GOOD Format Specification](https://frzyc.github.io/genshin-optimizer/#/doc)
- [ImageSharp Documentation](https://docs.sixlabors.com/articles/imagesharp/)
- [Microsoft DI Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/) (Phase 2)

## Questions?

For questions about the architecture or migration plan:

1. Review the relevant diagram(s) above
2. Read [PHASE_1.5_PLAN.md](PHASE_1.5_PLAN.md) for detailed explanation
3. Check code comments in Core library
4. Open an issue for discussion

---

**Last Updated:** 2026-04-07
**Status:** Phase 1.5 Planning Complete, Implementation Pending
