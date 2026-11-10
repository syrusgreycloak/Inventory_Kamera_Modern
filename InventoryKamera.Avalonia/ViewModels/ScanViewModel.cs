// NOTE: Global hotkeys are not supported in InventoryKamera 2.0 (Avalonia).
// NHotkey.WindowsForms is WinForms-bound and has no Avalonia equivalent in M2.
// Follow-up work: add a Windows-only global Stop hotkey via P/Invoke in a later milestone.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Configuration;
using NLog;

namespace InventoryKamera.Avalonia.ViewModels
{
    public partial class ScanViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object _logTargetLock = new object();

        // ----------------------------------------------------------------
        // Injected services
        // ----------------------------------------------------------------
        private readonly ISettingsService  _settingsService;
        private readonly IScanRunner       _scanRunner;
        private readonly IDialogService    _dialog;

        // Active cancellation token source for the running scan.
        private CancellationTokenSource _cts;

        // ----------------------------------------------------------------
        // Observable state — per-category progress counters
        // ----------------------------------------------------------------

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WeaponsProgressText))]
        private int _weaponsScanned;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WeaponsProgressText))]
        private int _weaponsTotal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArtifactsProgressText))]
        private int _artifactsScanned;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ArtifactsProgressText))]
        private int _artifactsTotal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CharactersProgressText))]
        private int _charactersScanned;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CharactersProgressText))]
        private int _charactersTotal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaterialsProgressText))]
        private int _materialsScanned;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaterialsProgressText))]
        private int _materialsTotal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CharDevItemsProgressText))]
        private int _charDevItemsScanned;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CharDevItemsProgressText))]
        private int _charDevItemsTotal;

        // ----------------------------------------------------------------
        // OCR preview state (set by AvaloniaUserInterface in chunk 6)
        // ----------------------------------------------------------------

        [ObservableProperty]
        private string _currentItemName;

        [ObservableProperty]
        private string[] _currentItemStats;

        /// <summary>
        /// PNG-encoded bytes of the current OCR screenshot.
        /// The chunk-5 view uses a converter to render this as an Avalonia Bitmap.
        /// Choosing byte[] keeps ScanViewModel testable without Avalonia.Headless.
        /// </summary>
        [ObservableProperty]
        private byte[] _currentScreenshot;

        [ObservableProperty]
        private bool? _isCurrentItemValid;

        [ObservableProperty]
        private string _validationMessage;

        // ----------------------------------------------------------------
        // Status bar (set by AvaloniaUserInterface)
        // ----------------------------------------------------------------

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private bool _statusIsOk = true;

        // ----------------------------------------------------------------
        // Traveler / main character name (set by AvaloniaUserInterface)
        // ----------------------------------------------------------------

        [ObservableProperty]
        private string _mainCharacterName;

        // ----------------------------------------------------------------
        // Scan state
        // ----------------------------------------------------------------

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartScanCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopScanCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExportLastCommand))]
        private bool _isScanning;

        // ----------------------------------------------------------------
        // Last completed scan — exposed so ExportViewModel can read
        // ----------------------------------------------------------------

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportLastCommand))]
        private IScanRunner _lastScan;

        // ----------------------------------------------------------------
        // Console output
        // ----------------------------------------------------------------

        /// <summary>Scrollable timestamped console entries; populated by ObservableCollectionLogTarget.</summary>
        public ObservableCollection<ConsoleEntry> Console { get; } = new ObservableCollection<ConsoleEntry>();

        // ----------------------------------------------------------------
        // Computed / derived properties
        // ----------------------------------------------------------------

        public string WeaponsProgressText      => $"{WeaponsScanned} / {WeaponsTotal}";
        public string ArtifactsProgressText    => $"{ArtifactsScanned} / {ArtifactsTotal}";
        public string CharactersProgressText   => $"{CharactersScanned} / {CharactersTotal}";
        public string MaterialsProgressText    => $"{MaterialsScanned} / {MaterialsTotal}";
        public string CharDevItemsProgressText => $"{CharDevItemsScanned} / {CharDevItemsTotal}";

        // ----------------------------------------------------------------
        // Settings-bound properties
        // These are hand-rolled (not [ObservableProperty]) because the source of
        // truth is ISettingsService.Current, not a local backing field.
        // ----------------------------------------------------------------

        /// <summary>Whether to include weapons in the next scan.</summary>
        public bool ScanWeapons
        {
            get => _settingsService.Current.ScanWeapons;
            set
            {
                if (_settingsService.Current.ScanWeapons == value) return;
                _settingsService.Current.ScanWeapons = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Whether to include artifacts in the next scan.</summary>
        public bool ScanArtifacts
        {
            get => _settingsService.Current.ScanArtifacts;
            set
            {
                if (_settingsService.Current.ScanArtifacts == value) return;
                _settingsService.Current.ScanArtifacts = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Whether to include characters in the next scan.</summary>
        public bool ScanCharacters
        {
            get => _settingsService.Current.ScanCharacters;
            set
            {
                if (_settingsService.Current.ScanCharacters == value) return;
                _settingsService.Current.ScanCharacters = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Whether to include character development items in the next scan.</summary>
        public bool ScanCharDevItems
        {
            get => _settingsService.Current.ScanCharDevItems;
            set
            {
                if (_settingsService.Current.ScanCharDevItems == value) return;
                _settingsService.Current.ScanCharDevItems = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Whether to include materials in the next scan.</summary>
        public bool ScanMaterials
        {
            get => _settingsService.Current.ScanMaterials;
            set
            {
                if (_settingsService.Current.ScanMaterials == value) return;
                _settingsService.Current.ScanMaterials = value;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Index into the scan-speed selector: 0=Fast (0ms delay), 1=Normal (default), 2=Slow.
        /// Mapped to <see cref="ScanSettings.ScannerDelay"/> as 0/0/500 ms.
        /// </summary>
        public int ScanSpeedIndex
        {
            get
            {
                int delay = _settingsService.Current.ScannerDelay;
                if (delay <= 0)   return 0; // Fast
                if (delay < 500)  return 1; // Normal
                return 2;                   // Slow
            }
            set
            {
                int delay = value == 0 ? 0 : value == 1 ? 0 : 500;
                // Keep Normal (index 1) distinguishable: store 100ms for Normal vs 0ms for Fast.
                if (value == 1) delay = 100;
                if (_settingsService.Current.ScannerDelay == delay) return;
                _settingsService.Current.ScannerDelay = delay;
                _settingsService.Save();
                OnPropertyChanged();
            }
        }

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public ScanViewModel(
            ISettingsService settingsService,
            IScanRunner      scanRunner,
            IDialogService   dialog)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _scanRunner      = scanRunner      ?? throw new ArgumentNullException(nameof(scanRunner));
            _dialog          = dialog          ?? throw new ArgumentNullException(nameof(dialog));

            // Wire the NLog → ObservableCollection bridge
            RegisterLogTarget();
        }

        // ----------------------------------------------------------------
        // Commands
        // ----------------------------------------------------------------

        /// <summary>
        /// Starts a full inventory scan on a background thread.
        /// Automatically disables itself and enables StopScan while running.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartScan))]
        private async Task StartScan()
        {
            // Platform guard: scanning is Windows-only. Show an informational dialog and
            // bail out rather than letting the NullScanRunner throw PlatformNotSupportedException.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await _dialog.ShowInfoAsync(
                    "Scanning Unavailable",
                    "Scanning requires Windows. You can view and export existing scan data on this platform.");
                return;
            }

            IsScanning = true;
            ResetCounters();
            Console.Clear();

            // Snapshot settings at scan start — not subscribed to SettingsChanged.
            var settings = _settingsService.Current;

            _cts = new CancellationTokenSource();

            try
            {
                await _scanRunner.RunAsync(settings, _cts.Token);

                // Write GOOD export if output path is configured
                if (_scanRunner.HasData && !string.IsNullOrWhiteSpace(settings.OutputPath))
                {
                    try
                    {
                        var good = new GOOD(_scanRunner.Characters, _scanRunner.Inventory, settings.EquipWeapons, settings.EquipArtifacts);
                        good.WriteToJSON(settings.OutputPath);
                        Log.Info("GOOD export written to {OutputPath}", settings.OutputPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "GOOD export write failed");
                    }
                }
                else if (_scanRunner.HasData)
                {
                    Log.Warn("Scan finished, but no output folder is set, so nothing was saved. Choose one in Settings, then use Manual Export.");
                }

                LastScan = _scanRunner.HasData ? _scanRunner : null;
            }
            catch (OperationCanceledException)
            {
                Log.Info("Scan stopped by user");
                // Do not write export for partial scans
                LastScan = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Scan failed with unexpected error");
                LastScan = null;
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                IsScanning = false;
            }
        }

        private bool CanStartScan() => !IsScanning;

        /// <summary>Cancels the running scan.</summary>
        [RelayCommand(CanExecute = nameof(CanStopScan))]
        private void StopScan()
        {
            _cts?.Cancel();
        }

        private bool CanStopScan() => IsScanning;

        /// <summary>Re-emits GOOD JSON from the last completed scan.</summary>
        [RelayCommand(CanExecute = nameof(CanExportLast))]
        private void ExportLast()
        {
            if (LastScan == null || !LastScan.HasData) return;

            var settings = _settingsService.Current;
            var outputPath = settings.OutputPath;

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                Log.Warn("No output path configured; cannot export");
                return;
            }

            try
            {
                var good = new GOOD(LastScan.Characters, LastScan.Inventory, settings.EquipWeapons, settings.EquipArtifacts);
                good.WriteToJSON(outputPath);
                Log.Info("GOOD export written to {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export failed");
            }
        }

        private bool CanExportLast() => LastScan != null && LastScan.HasData && !IsScanning;

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private void ResetCounters()
        {
            WeaponsScanned      = WeaponsTotal      = 0;
            ArtifactsScanned    = ArtifactsTotal    = 0;
            CharactersScanned   = CharactersTotal   = 0;
            MaterialsScanned    = MaterialsTotal    = 0;
            CharDevItemsScanned = CharDevItemsTotal = 0;
        }

        private void RegisterLogTarget()
        {
            lock (_logTargetLock)
            {
                // Only register once per process to avoid duplicate entries if
                // multiple ScanViewModel instances are created in tests.
                const string targetName = "ScanViewModelConsole";
                if (LogManager.Configuration == null)
                    return;

                if (LogManager.Configuration.FindTargetByName(targetName) != null)
                    return;

                var target = new ObservableCollectionLogTarget(Console) { Name = targetName };
                LogManager.Configuration.AddTarget(target);
                LogManager.Configuration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, target);
                LogManager.ReconfigExistingLoggers();
            }
        }
    }
}
