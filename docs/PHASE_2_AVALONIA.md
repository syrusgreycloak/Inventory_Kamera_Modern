# Phase 2: Avalonia UI Migration

**Status:** Planned (Post-Phase 1.6)
**Dependencies:** Phase 1.6 SQLite migration complete, Phase 1.5 abstractions in place
**Goal:** Migrate from WinForms to Avalonia UI for modern, cross-platform interface

---

## Overview

Phase 2 migrates the application UI from Windows Forms (.NET Framework 4.7.2) to Avalonia UI (.NET 8), enabling:
- Cross-platform support (Windows, macOS, Linux)
- Modern UI patterns (MVVM, data binding, reactive programming)
- Better styling and theming capabilities
- Foundation for Phase 2+ visual region configuration tool
- Improved performance and rendering
- Community contributions from non-Windows developers

---

## Problem Statement

### Current Limitations with WinForms

1. **Windows-only** - Cannot run on macOS or Linux
2. **Outdated UI patterns** - Direct manipulation of controls, event-driven
3. **Poor styling** - Limited theming, inconsistent look across Windows versions
4. **Difficult testing** - UI logic tightly coupled to forms
5. **Limited modern controls** - No built-in support for modern UX patterns
6. **Accessibility issues** - Poor screen reader support, limited keyboard navigation

### Target Experience (Phase 2)

- **Cross-platform binary** - Same app runs on Windows, macOS, Linux
- **Modern MVVM architecture** - Separation of concerns, testable ViewModels
- **Consistent styling** - Custom theme matching Genshin Impact aesthetic
- **Responsive layout** - Adapts to different window sizes
- **Better accessibility** - Keyboard shortcuts, screen reader support
- **Foundation for visual tools** - Region config editor (Phase 2+)

---

## Feature Scope

### In Scope (Phase 2.0)

**Core UI Migration:**
- Main window with tabbed interface
- Scan tab (start/stop scanning, progress indicators)
- Settings tab (resolution, sorting, filters, logging)
- Export tab (GOOD JSON, database export)
- About tab (version info, credits, links)

**Preserve Existing Functionality:**
- All Phase 1.5 scanning features work identically
- No regression in OCR accuracy or reliability
- Same GOOD JSON export format
- Settings persist across restarts

**MVVM Architecture:**
- ViewModels for each view (MainViewModel, ScanViewModel, etc.)
- Data binding for all UI state
- Commands for all user actions
- Dependency injection for services

**Cross-Platform Support:**
- Windows 10/11 (primary platform)
- macOS 12+ (Intel and Apple Silicon)
- Linux (Ubuntu 22.04+, other distros)

### Out of Scope (Phase 2+)

- ❌ Visual region configuration tool (Phase 2+)
- ❌ Database admin UI (Phase 2+)
- ❌ Advanced charting/statistics
- ❌ Mobile support (Android/iOS)

---

## Architecture

### Project Structure

```
InventoryKamera.sln
├── InventoryKamera/                    # Legacy WinForms (Phase 1.5)
├── InventoryKamera.Core/               # Shared business logic (Phase 1.5)
│   ├── Services/
│   ├── Models/
│   └── Abstractions/
├── InventoryKamera.Avalonia/           # NEW: Avalonia UI
│   ├── App.axaml                       # Application entry point
│   ├── Views/
│   │   ├── MainWindow.axaml            # Main window with tabs
│   │   ├── ScanView.axaml              # Scanning tab
│   │   ├── SettingsView.axaml          # Settings tab
│   │   ├── ExportView.axaml            # Export tab
│   │   └── AboutView.axaml             # About tab
│   ├── ViewModels/
│   │   ├── ViewModelBase.cs            # Base class with INotifyPropertyChanged
│   │   ├── MainViewModel.cs            # Main window VM
│   │   ├── ScanViewModel.cs            # Scan tab VM
│   │   ├── SettingsViewModel.cs        # Settings tab VM
│   │   ├── ExportViewModel.cs          # Export tab VM
│   │   └── AboutViewModel.cs           # About tab VM
│   ├── Services/
│   │   └── AvaloniaUserInterface.cs    # IUserInterface impl for Avalonia
│   ├── Converters/
│   │   └── BoolToVisibilityConverter.cs
│   └── Assets/
│       ├── Fonts/
│       └── Images/
├── InventoryKamera.Database/           # Phase 1.6 SQLite layer
└── InventoryKamera.Tests/
```

### Dependency Flow

```
InventoryKamera.Avalonia (UI)
    ↓ depends on
InventoryKamera.Core (Business Logic)
    ↓ depends on
InventoryKamera.Database (Data Access)
```

---

## MVVM Pattern

### ViewModelBase

```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

### ScanViewModel Example

```csharp
public class ScanViewModel : ViewModelBase
{
    private readonly IInventoryScanner _scanner;
    private readonly IUserInterface _ui;

    private bool _isScanning;
    private int _weaponsScanned;
    private int _totalWeapons;
    private string _statusMessage;

    public ScanViewModel(IInventoryScanner scanner, IUserInterface ui)
    {
        _scanner = scanner;
        _ui = ui;

        // Use AsyncRelayCommand for async operations to avoid async void anti-pattern
        StartScanCommand = new AsyncRelayCommand(StartScanAsync, CanStartScan);
        StopScanCommand = new RelayCommand(StopScan, CanStopScan);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                StartScanCommand.RaiseCanExecuteChanged();
                StopScanCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int WeaponsScanned
    {
        get => _weaponsScanned;
        set => SetProperty(ref _weaponsScanned, value);
    }

    public int TotalWeapons
    {
        get => _totalWeapons;
        set => SetProperty(ref _totalWeapons, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public IAsyncRelayCommand StartScanCommand { get; }
    public ICommand StopScanCommand { get; }

    private bool CanStartScan() => !IsScanning;
    private bool CanStopScan() => IsScanning;

    private async Task StartScanAsync()
    {
        IsScanning = true;
        StatusMessage = "Starting scan...";

        try
        {
            await Task.Run(() => _scanner.ScanWeapons());
            StatusMessage = "Scan completed successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void StopScan()
    {
        _scanner.StopScanning();
        StatusMessage = "Scan stopped by user";
        IsScanning = false;
    }
}
```

### View with Data Binding

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:InventoryKamera.Avalonia.ViewModels"
             x:Class="InventoryKamera.Avalonia.Views.ScanView">

  <Design.DataContext>
    <vm:ScanViewModel />
  </Design.DataContext>

  <Grid RowDefinitions="Auto,*,Auto" Margin="16">

    <!-- Header -->
    <StackPanel Grid.Row="0" Spacing="8">
      <TextBlock Text="Scan Inventory" FontSize="24" FontWeight="Bold" />
      <TextBlock Text="Scan your Genshin Impact inventory for weapons, artifacts, and characters"
                 Foreground="{DynamicResource ThemeAccentBrush2}" />
    </StackPanel>

    <!-- Scan Progress -->
    <Border Grid.Row="1"
            Background="{DynamicResource ThemeBackgroundBrush}"
            CornerRadius="8"
            Padding="16"
            Margin="0,16">

      <StackPanel Spacing="16">

        <!-- Status Message -->
        <TextBlock Text="{Binding StatusMessage}"
                   FontSize="16"
                   HorizontalAlignment="Center" />

        <!-- Progress Bar -->
        <ProgressBar Value="{Binding WeaponsScanned}"
                     Maximum="{Binding TotalWeapons}"
                     IsIndeterminate="{Binding IsScanning}"
                     Height="24" />

        <!-- Progress Text -->
        <TextBlock HorizontalAlignment="Center">
          <Run Text="{Binding WeaponsScanned}" />
          <Run Text=" / " />
          <Run Text="{Binding TotalWeapons}" />
          <Run Text=" weapons scanned" />
        </TextBlock>

      </StackPanel>
    </Border>

    <!-- Action Buttons -->
    <StackPanel Grid.Row="2"
                Orientation="Horizontal"
                HorizontalAlignment="Right"
                Spacing="8">

      <Button Content="Start Scan"
              Command="{Binding StartScanCommand}"
              Width="120"
              Height="36" />

      <Button Content="Stop"
              Command="{Binding StopScanCommand}"
              Width="120"
              Height="36" />
    </StackPanel>

  </Grid>
</UserControl>
```

---

## IUserInterface Avalonia Implementation

Replace WinForms `UserInterface.cs` with Avalonia-compatible implementation:

```csharp
public class AvaloniaUserInterface : IUserInterface
{
    private readonly Dispatcher _dispatcher;
    private readonly MainViewModel _mainViewModel;

    public AvaloniaUserInterface(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _dispatcher = Dispatcher.UIThread;
    }

    public void SetWeapon_Max(int count)
    {
        _dispatcher.Post(() =>
        {
            _mainViewModel.ScanViewModel.TotalWeapons = count;
        });
    }

    public void IncrementWeapon(int count)
    {
        _dispatcher.Post(() =>
        {
            _mainViewModel.ScanViewModel.WeaponsScanned += count;
        });
    }

    public void AddError(string message)
    {
        _dispatcher.Post(() =>
        {
            _mainViewModel.ScanViewModel.Errors.Add(new ErrorMessage
            {
                Timestamp = DateTime.Now,
                Message = message
            });
        });
    }

    public void SetStatus(string message)
    {
        _dispatcher.Post(() =>
        {
            _mainViewModel.ScanViewModel.StatusMessage = message;
        });
    }
}
```

---

## Styling and Theming

### Custom Genshin Impact Theme

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Color Palette (inspired by Genshin Impact UI) -->
  <Style.Resources>
    <Color x:Key="PrimaryColor">#1e2328</Color>        <!-- Dark background -->
    <Color x:Key="SecondaryColor">#2d3139</Color>      <!-- Lighter background -->
    <Color x:Key="AccentColor">#4A90E2</Color>         <!-- Blue accent -->
    <Color x:Key="TextPrimaryColor">#ECE5D8</Color>    <!-- Light text -->
    <Color x:Key="TextSecondaryColor">#A69F92</Color>  <!-- Dim text -->
    <Color x:Key="SuccessColor">#5AB55E</Color>        <!-- Green -->
    <Color x:Key="WarningColor">#F9B959</Color>        <!-- Orange -->
    <Color x:Key="ErrorColor">#E74C3C</Color>          <!-- Red -->
  </Style.Resources>

  <!-- Window Style -->
  <Style Selector="Window">
    <Setter Property="Background" Value="{DynamicResource PrimaryColor}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryColor}" />
    <Setter Property="FontFamily" Value="Segoe UI,Arial,sans-serif" />
  </Style>

  <!-- Button Style -->
  <Style Selector="Button">
    <Setter Property="Background" Value="{DynamicResource SecondaryColor}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryColor}" />
    <Setter Property="BorderBrush" Value="{DynamicResource AccentColor}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="4" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="Cursor" Value="Hand" />
  </Style>

  <Style Selector="Button:pointerover">
    <Setter Property="Background" Value="{DynamicResource AccentColor}" />
  </Style>

  <Style Selector="Button:disabled">
    <Setter Property="Opacity" Value="0.5" />
    <Setter Property="Cursor" Value="Arrow" />
  </Style>

  <!-- TabControl Style -->
  <Style Selector="TabControl">
    <Setter Property="Background" Value="{DynamicResource PrimaryColor}" />
  </Style>

  <Style Selector="TabItem">
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Padding" Value="16,8" />
  </Style>

  <Style Selector="TabItem:selected">
    <Setter Property="Background" Value="{DynamicResource AccentColor}" />
  </Style>

</Styles>
```

---

## Cross-Platform Considerations

### Platform-Specific Code

Use Avalonia's platform detection for OS-specific features:

```csharp
public static class PlatformHelper
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static string GetGameProcessName()
    {
        if (IsWindows)
            return "GenshinImpact.exe";
        else if (IsMacOS)
            return "Genshin Impact"; // .app bundle name
        else
            return "GenshinImpact"; // Wine/Proton on Linux
    }

    public static string GetConfigDirectory()
    {
        if (IsWindows)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "InventoryKamera");
        else if (IsMacOS)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/InventoryKamera");
        else // Linux
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config/inventorykamera");
    }
}
```

### Input Simulation

Windows-only `InputSimulator` needs alternatives for macOS/Linux:

**Phase 2.0:** Keep Windows-only input simulation, show platform warning
**Phase 2.1+:** Investigate cross-platform input libraries (e.g., virtual input devices)

```csharp
public interface IInputSimulator
{
    void Click();
    void SetCursor(int x, int y);
    void KeyPress(VirtualKeyCode key);
}

public class WindowsInputSimulator : IInputSimulator
{
    private readonly InputSimulator _sim = new InputSimulator();
    // Existing implementation
}

public class MacOSInputSimulator : IInputSimulator
{
    // TODO: Phase 2.1 - Use CGEventPost or similar
    public void Click() => throw new PlatformNotSupportedException(
        "Input simulation not yet supported on macOS. Scanning requires Windows.");
}
```

### Screenshot Capture

Windows uses `Graphics.CopyFromScreen()`, need alternatives:

```csharp
public interface IScreenCapture
{
    Bitmap CaptureRegion(Rectangle region);
    Bitmap CaptureWindow(IntPtr handle);
}

public class WindowsScreenCapture : IScreenCapture
{
    // Existing Graphics.CopyFromScreen implementation
}

public class X11ScreenCapture : IScreenCapture
{
    // Linux X11 screenshot implementation
}

public class MacOSScreenCapture : IScreenCapture
{
    // macOS CoreGraphics implementation
}
```

---

## Migration Strategy

### Phase 2.0: Side-by-Side

1. **Keep WinForms project** - Don't delete, mark as legacy
2. **Create Avalonia project** - New project, shares Core library
3. **Dual builds** - Both EXEs ship in release
4. **User choice** - README explains which to use
5. **Beta testing** - Avalonia marked as "beta" for Phase 2.0

### Phase 2.1: Avalonia Primary

1. **Avalonia is default** - Prominently featured in releases
2. **WinForms deprecated** - Marked as "legacy, will be removed"
3. **Bug fixes in both** - Critical fixes backported to WinForms

### Phase 2.2: WinForms Removal

1. **Delete WinForms project** - No more dual maintenance
2. **Avalonia only** - Single executable per platform
3. **Full feature parity** - All WinForms features migrated

**WinForms Project Retirement Timeline:**
- **Phase 2.0 completion**: WinForms project marked as `[DEPRECATED]` in README
- **Phase 2.1 completion**: Final WinForms release with deprecation notice
- **Phase 2.2 completion**: WinForms project removed from repository entirely
- **Goal**: "Build it right and release it" - Once Avalonia is validated, WinForms is retired completely

After Phase 2.2, the repository will contain only:
- `InventoryKamera.Core` (shared business logic)
- `InventoryKamera.Database` (data access layer)
- `InventoryKamera.Avalonia` (modern UI)
- `InventoryKamera.Tests` (unit and integration tests)

---

## Testing Strategy

### Unit Tests for ViewModels

```csharp
public class ScanViewModelTests
{
    [Fact]
    public void StartScan_WhenNotScanning_ShouldEnableScanning()
    {
        // Arrange
        var mockScanner = new Mock<IInventoryScanner>();
        var mockUI = new Mock<IUserInterface>();
        var vm = new ScanViewModel(mockScanner.Object, mockUI.Object);

        // Act
        vm.StartScanCommand.Execute(null);

        // Assert
        Assert.True(vm.IsScanning);
        mockScanner.Verify(s => s.ScanWeapons(), Times.Once);
    }

    [Fact]
    public void StartScan_WhenAlreadyScanning_ShouldNotStartAgain()
    {
        // Arrange
        var vm = new ScanViewModel(Mock.Of<IInventoryScanner>(), Mock.Of<IUserInterface>());
        vm.IsScanning = true;

        // Act
        bool canExecute = vm.StartScanCommand.CanExecute(null);

        // Assert
        Assert.False(canExecute);
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task MainWindow_LoadsWithoutCrashing()
{
    // Use Avalonia's headless test environment
    using var app = AvaloniaApp.BuildAvaloniaApp()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .SetupWithoutStarting();

    var window = new MainWindow();
    window.Show();

    Assert.NotNull(window.DataContext);
}
```

---

## Implementation Timeline

### Phase 2.0: Core Migration (4-5 weeks)

**Week 1-2: Project Setup**
- [ ] Create InventoryKamera.Avalonia project
- [ ] Set up MVVM infrastructure (ViewModelBase, RelayCommand)
- [ ] Configure dependency injection
- [ ] Create MainWindow with tab structure
- [ ] Implement basic styling/theming

**Week 3: Scan View**
- [ ] Migrate scan UI to Avalonia
- [ ] Implement ScanViewModel with data binding
- [ ] Wire up commands (Start, Stop)
- [ ] Test progress updates from scanner

**Week 4: Settings & Export Views**
- [ ] Migrate settings UI
- [ ] Implement two-way binding for settings
- [ ] Migrate export UI
- [ ] Test GOOD JSON export from Avalonia

**Week 5: Testing & Polish**
- [ ] Write ViewModel unit tests
- [ ] Cross-platform testing (Windows, macOS, Linux)
- [ ] Fix platform-specific bugs
- [ ] Update documentation

### Phase 2.1: Feature Parity (2-3 weeks)

- [ ] All WinForms features work in Avalonia
- [ ] Cross-platform input simulation (macOS, Linux)
- [ ] Cross-platform screenshot capture
- [ ] Beta testing with community

### Phase 2.2: WinForms Removal (1 week)

- [ ] Delete WinForms project
- [ ] Update build scripts
- [ ] Update documentation
- [ ] Release Avalonia-only version

---

## Success Criteria

### Phase 2.0 Complete When:

- [ ] Avalonia app compiles and runs on Windows 10/11
- [ ] Avalonia app compiles and runs on macOS (Intel & Apple Silicon)
- [ ] Avalonia app compiles and runs on Linux (Ubuntu 22.04+)
- [ ] All scanning features work identically to WinForms version
- [ ] Settings persist across restarts
- [ ] GOOD JSON export produces identical output
- [ ] ViewModel unit tests have >80% coverage
- [ ] Documentation updated with Avalonia setup instructions
- [ ] At least 3 beta testers successfully use Avalonia version

### Phase 2.1 Complete When:

- [ ] Cross-platform input simulation works (or graceful fallback)
- [ ] Cross-platform screenshot capture works
- [ ] All WinForms features migrated
- [ ] Avalonia marked as "stable" (not beta)
- [ ] WinForms marked as "deprecated"

### Phase 2.2 Complete When:

- [ ] WinForms project deleted from repository
- [ ] Single Avalonia binary ships per platform
- [ ] All documentation references to WinForms removed
- [ ] Community fully transitioned to Avalonia version

---

## Risks and Mitigation

### Risk 1: Cross-Platform Input Simulation

**Risk:** Input simulation (mouse clicks, keyboard) may not work on macOS/Linux

**Mitigation:**
- Phase 2.0: Ship Windows-only version, document limitation
- Phase 2.1: Research cross-platform input libraries
- Worst case: Require Windows for scanning, other platforms for data viewing only

### Risk 2: Screenshot Capture Performance

**Risk:** Cross-platform screenshot APIs may be slower than Windows-specific approach

**Mitigation:**
- Benchmark each platform's capture method
- Use native APIs where possible (CoreGraphics on macOS, X11 on Linux)
- Maintain Windows optimization path

### Risk 3: Community Resistance

**Risk:** Users may prefer familiar WinForms interface

**Mitigation:**
- Ship both versions during Phase 2.0-2.1
- Collect feedback, address concerns
- Highlight benefits (cross-platform, modern UI, visual tools)

---

## Required NuGet Packages

```xml
<ItemGroup>
  <!-- Avalonia Core -->
  <PackageReference Include="Avalonia" Version="11.0.0" />
  <PackageReference Include="Avalonia.Desktop" Version="11.0.0" />
  <PackageReference Include="Avalonia.Themes.Fluent" Version="11.0.0" />

  <!-- MVVM Framework (choose one) -->
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
  <!-- OR -->
  <PackageReference Include="ReactiveUI.Avalonia" Version="19.5.0" />

  <!-- Rendering (for Phase 2+ region config tool) -->
  <PackageReference Include="Avalonia.Skia" Version="11.0.0" />

  <!-- Dependency Injection -->
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />

  <!-- Shared Core Library -->
  <ProjectReference Include="..\InventoryKamera.Core\InventoryKamera.Core.csproj" />
  <ProjectReference Include="..\InventoryKamera.Database\InventoryKamera.Database.csproj" />
</ItemGroup>
```

---

## Dependency Injection Setup

Avalonia has first-class DI support. Replace WinForms adapter pattern with proper DI container:

```csharp
// App.axaml.cs
public partial class App : Application
{
    public IServiceProvider Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Build service provider
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core services (from Phase 1.5)
        services.AddSingleton<IGameDataRepository, SqliteGameDataRepository>();
        services.AddSingleton<IScreenCapture, WindowsScreenCapture>();
        services.AddSingleton<IInputSimulator, WindowsInputSimulator>();
        services.AddSingleton<IScanOrchestrator, ScanOrchestrator>();

        // Avalonia UI services
        services.AddSingleton<IUserInterface, AvaloniaUserInterface>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<ScanViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ExportViewModel>();
        services.AddTransient<AboutViewModel>();

        // Database context
        services.AddDbContext<GenshinDbContext>(options =>
            options.UseSqlite("Data Source=genshin.db"));
    }
}
```

---

## Progress Reporting with IProgress<T>

The `ScanOrchestrator` (from Phase 1.5) surfaces scan progress via `IProgress<ScanProgress>`. ViewModels subscribe to these updates:

```csharp
// From Phase 1.5 - ScanOrchestrator
public class ScanOrchestrator : IScanOrchestrator
{
    public async Task ScanWeaponsAsync(
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken)
    {
        int total = DetermineWeaponCount();
        int current = 0;

        progress?.Report(new ScanProgress
        {
            Type = ScanType.Weapons,
            Current = current,
            Total = total,
            Status = "Starting weapon scan..."
        });

        // ... scan logic ...

        foreach (var weapon in weapons)
        {
            current++;
            progress?.Report(new ScanProgress
            {
                Type = ScanType.Weapons,
                Current = current,
                Total = total,
                Status = $"Scanned {weapon.Name}"
            });

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}

// Avalonia ViewModel subscribes
public class ScanViewModel : ViewModelBase
{
    private readonly IScanOrchestrator _orchestrator;

    private int _currentItem;
    private int _totalItems;
    private string _statusMessage;

    public async Task StartScanAsync()
    {
        var progress = new Progress<ScanProgress>(p =>
        {
            // Update UI on UI thread automatically
            CurrentItem = p.Current;
            TotalItems = p.Total;
            StatusMessage = p.Status;
        });

        var cts = new CancellationTokenSource();
        await _orchestrator.ScanWeaponsAsync(progress, cts.Token);
    }
}
```

---

## OCR Preview and Screenshot Panel

The Avalonia UI includes an OCR preview panel for debugging scan regions (referenced in `AVALONIA_UI_MOCKUP.md`):

```xml
<!-- ScanView.axaml - Screenshot Preview Panel -->
<Border Grid.Column="1"
        Background="{DynamicResource ThemeBackgroundBrush}"
        CornerRadius="8"
        Padding="16">

  <StackPanel Spacing="8">
    <TextBlock Text="OCR Preview"
               FontSize="18"
               FontWeight="Bold" />

    <!-- Live screenshot display -->
    <Image Source="{Binding CurrentScreenshot}"
           Width="400"
           Height="300"
           Stretch="Uniform" />

    <!-- Current region being processed -->
    <TextBlock Text="{Binding CurrentRegionName}"
               HorizontalAlignment="Center" />

    <!-- OCR result text -->
    <Border BorderBrush="{DynamicResource AccentColor}"
            BorderThickness="1"
            CornerRadius="4"
            Padding="8"
            Background="{DynamicResource SecondaryColor}">
      <TextBlock Text="{Binding OcrResultText}"
                 FontFamily="Consolas"
                 TextWrapping="Wrap" />
    </Border>

    <!-- Toggle live preview -->
    <CheckBox Content="Enable live preview (slower scanning)"
              IsChecked="{Binding EnableOcrPreview}" />
  </StackPanel>
</Border>
```

```csharp
// ScanViewModel - OCR preview properties
public class ScanViewModel : ViewModelBase
{
    private Avalonia.Media.Imaging.Bitmap _currentScreenshot;
    private string _currentRegionName;
    private string _ocrResultText;
    private bool _enableOcrPreview;

    public Avalonia.Media.Imaging.Bitmap CurrentScreenshot
    {
        get => _currentScreenshot;
        set => SetProperty(ref _currentScreenshot, value);
    }

    public string CurrentRegionName
    {
        get => _currentRegionName;
        set => SetProperty(ref _currentRegionName, value);
    }

    public string OcrResultText
    {
        get => _ocrResultText;
        set => SetProperty(ref _ocrResultText, value);
    }

    public bool EnableOcrPreview
    {
        get => _enableOcrPreview;
        set => SetProperty(ref _enableOcrPreview, value);
    }
}

// AvaloniaUserInterface - Update preview
public class AvaloniaUserInterface : IUserInterface
{
    public void UpdateOcrPreview(System.Drawing.Bitmap screenshot, string regionName, string ocrText)
    {
        if (!_viewModel.ScanViewModel.EnableOcrPreview)
            return;

        // Convert System.Drawing.Bitmap to Avalonia.Media.Imaging.Bitmap
        var avaloniaBitmap = ConvertToAvaloniaBitmap(screenshot);

        _dispatcher.Post(() =>
        {
            _viewModel.ScanViewModel.CurrentScreenshot = avaloniaBitmap;
            _viewModel.ScanViewModel.CurrentRegionName = regionName;
            _viewModel.ScanViewModel.OcrResultText = ocrText;
        });
    }

    private Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(System.Drawing.Bitmap gdiBitmap)
    {
        using var memoryStream = new MemoryStream();
        gdiBitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
        memoryStream.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(memoryStream);
    }
}
```

---

## UI Layout Reference

See `AVALONIA_UI_MOCKUP.md` for detailed UI mockups and layout specifications. The mockups define:
- Main window tab structure
- Scan view with progress indicators
- Settings panel organization
- Export options layout
- OCR preview panel (described above)

---

## Cross-Platform Deployment Targets

**Phase 2.0 Priority:**
- **Windows 10/11** - Primary platform, full feature support
- **macOS** - Experimental, input simulation not yet implemented (Windows-only stubs)
- **Linux** - Experimental, input simulation not yet implemented (Windows-only stubs)

**Future Phases:**
- Phase 2.1: Cross-platform `IScreenCapture` implementations
- Phase 2.2: Cross-platform `IInputSimulator` implementations (or graceful degradation)
- Phase 2.3: Full feature parity on macOS/Linux

For now, macOS/Linux builds will show a warning:
```csharp
if (!PlatformHelper.IsWindows)
{
    await ShowDialog(
        "Platform Limitation",
        "Input simulation is currently Windows-only. You can view/export data but cannot scan on this platform.",
        "OK");
}
```

---

## Related Documents

- `AVALONIA_UI_MOCKUP.md` - Detailed UI layouts and mockups
- `PHASE_1.5_PLAN.md` - Core library extraction, abstractions
- `PHASE_1.6_DATA_STORAGE.md` - SQLite migration (data layer for Avalonia)
- `PHASE_2_VISUAL_REGION_CONFIG.md` - Visual region tool (depends on this Avalonia migration)
- `PHASE_3_AI_ASSISTANT.md` - AI chat interface (depends on this Avalonia migration)

---

**Last Updated:** 2026-04-10
**Status:** Draft - Planned for Phase 2 (Post-Phase 1.6)
**Note:** WinForms project will be retired after Phase 2.2 validation
