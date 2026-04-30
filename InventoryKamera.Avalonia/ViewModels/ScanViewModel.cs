// NOTE: Global hotkeys are not supported in InventoryKamera 2.0 (Avalonia).
// NHotkey.WindowsForms is WinForms-bound and has no Avalonia equivalent in M2.
// Follow-up work: add a Windows-only global Stop hotkey via P/Invoke in a later milestone.

using System;
using System.Collections.ObjectModel;
using System.IO;
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

        // ----------------------------------------------------------------
        // Injected services
        // ----------------------------------------------------------------
        private readonly ISettingsService  _settingsService;
        private readonly IScanRunner       _scanRunner;

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

        public string WeaponsProgressText    => $"{WeaponsScanned} / {WeaponsTotal}";
        public string ArtifactsProgressText  => $"{ArtifactsScanned} / {ArtifactsTotal}";
        public string CharactersProgressText => $"{CharactersScanned} / {CharactersTotal}";
        public string MaterialsProgressText  => $"{MaterialsScanned} / {MaterialsTotal}";

        // ----------------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------------

        public ScanViewModel(
            ISettingsService settingsService,
            IScanRunner      scanRunner)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _scanRunner      = scanRunner      ?? throw new ArgumentNullException(nameof(scanRunner));

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
            WeaponsScanned    = WeaponsTotal    = 0;
            ArtifactsScanned  = ArtifactsTotal  = 0;
            CharactersScanned = CharactersTotal  = 0;
            MaterialsScanned  = MaterialsTotal   = 0;
        }

        private void RegisterLogTarget()
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
            LogManager.Configuration = LogManager.Configuration; // triggers reconfiguration of existing loggers
        }
    }
}
