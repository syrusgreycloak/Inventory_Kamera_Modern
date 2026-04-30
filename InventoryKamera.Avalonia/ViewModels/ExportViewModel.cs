using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Configuration;
using NLog;

namespace InventoryKamera.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for the Export tab.
    /// Handles GOOD JSON export, opening the output folder, and uploading to Genshin Optimizer.
    /// </summary>
    public partial class ExportViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ScanViewModel    _scanVm;
        private readonly ISettingsService _settings;
        private readonly IClipboardService _clipboard;
        private readonly IUrlLauncher      _url;
        private readonly IDialogService    _dialog;

        // ─────────────────────────────────────────────────────────────────────
        // Observable state
        // ─────────────────────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ScanStatusText))]
        [NotifyCanExecuteChangedFor(nameof(ExportGoodJsonCommand))]
        [NotifyCanExecuteChangedFor(nameof(UploadToOptimizerCommand))]
        private bool _hasScanData;

        // ─────────────────────────────────────────────────────────────────────
        // Derived / pass-through properties
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Human-readable scan status line shown in the Export panel.</summary>
        public string ScanStatusText => HasScanData ? "Available" : "No data — run a scan first";

        /// <summary>Current output directory path (mirrors settings).</summary>
        public string OutputPath => _settings.Current.OutputPath;

        // ─────────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────────

        public ExportViewModel(
            ScanViewModel     scanVm,
            ISettingsService  settings,
            IClipboardService clipboard,
            IUrlLauncher      url,
            IDialogService    dialog)
        {
            _scanVm    = scanVm    ?? throw new ArgumentNullException(nameof(scanVm));
            _settings  = settings  ?? throw new ArgumentNullException(nameof(settings));
            _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            _url       = url       ?? throw new ArgumentNullException(nameof(url));
            _dialog    = dialog    ?? throw new ArgumentNullException(nameof(dialog));

            // Sync HasScanData from ScanViewModel.LastScan whenever it changes.
            _scanVm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScanViewModel.LastScan))
                {
                    HasScanData = _scanVm.LastScan?.HasData == true;
                    OnPropertyChanged(nameof(OutputPath));
                }
            };

            // Initialise from the current state (in case ExportViewModel is resolved
            // after the scan has already completed).
            HasScanData = _scanVm.LastScan?.HasData == true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes a GOOD JSON file to <see cref="OutputPath"/> from the last completed scan.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private void ExportGoodJson()
        {
            var settings   = _settings.Current;
            var outputPath = settings.OutputPath;

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                Log.Warn("No output path configured; cannot export");
                return;
            }

            try
            {
                var good = new GOOD(
                    _scanVm.LastScan.Characters,
                    _scanVm.LastScan.Inventory,
                    settings.EquipWeapons,
                    settings.EquipArtifacts);

                good.WriteToJSON(outputPath);
                Log.Info("GOOD export written to {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Export failed");
            }
        }

        private bool CanExport() => HasScanData;

        /// <summary>Opens the configured output folder using the OS default handler.</summary>
        [RelayCommand]
        private void OpenOutputFolder()
        {
            var path = _settings.Current.OutputPath;
            if (!string.IsNullOrWhiteSpace(path))
                _url.Open(path);
        }

        /// <summary>
        /// Copies the GOOD JSON to the clipboard and opens the Genshin Optimizer import URL.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private async Task UploadToOptimizer()
        {
            var settings = _settings.Current;

            try
            {
                var good = new GOOD(
                    _scanVm.LastScan.Characters,
                    _scanVm.LastScan.Inventory,
                    settings.EquipWeapons,
                    settings.EquipArtifacts);

                var json = good.ToString();
                await _clipboard.SetTextAsync(json);
                _url.Open("https://frzyc.github.io/genshin-optimizer/#/setting");
                await _dialog.ShowInfoAsync(
                    "Copied",
                    "Content copied to clipboard! Paste the content into the Genshin Optimizer textbox when prompted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Upload to optimizer failed");
                await _dialog.ShowErrorAsync("Export failed", ex.Message);
            }
        }
    }
}
