using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Threading;
using InventoryKamera.Avalonia.ViewModels;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IUserInterface"/>.
    /// Marshals every call onto the Avalonia UI thread and updates
    /// <see cref="ScanViewModel"/> observable properties.
    ///
    /// Image-producing calls encode the <see cref="Image{Rgba32}"/> to a PNG
    /// byte array (the caller owns and may dispose the image immediately after
    /// the method returns).  Screenshot updates are throttled to at most one
    /// update per <see cref="_previewThrottleMs"/> milliseconds.
    ///
    /// Text-only state (counters, status, names) is forwarded directly to VM
    /// properties.  Console narrative (constellation/talent/mora progress) is
    /// routed through NLog so the existing ObservableCollectionLogTarget
    /// delivers it to the UI console with proper level/timestamp.
    /// </summary>
    public sealed class AvaloniaUserInterface : IUserInterface
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly ScanViewModel _scanVm;

        // ----------------------------------------------------------------
        // Screenshot throttle — at most one bitmap update per 100 ms.
        // The gate is evaluated before the Dispatcher.Post so we skip
        // useless cross-thread hops entirely.
        // ----------------------------------------------------------------
        private readonly Stopwatch _previewClock = Stopwatch.StartNew();
        private long _lastPreviewMs = -1000;
        private static readonly long _previewThrottleMs = 100;

        public AvaloniaUserInterface(ScanViewModel scanVm)
        {
            _scanVm = scanVm ?? throw new ArgumentNullException(nameof(scanVm));
        }

        // ----------------------------------------------------------------
        // Throttle helper
        // ----------------------------------------------------------------

        private bool ShouldUpdatePreview()
        {
            var now = _previewClock.ElapsedMilliseconds;
            if (now - _lastPreviewMs < _previewThrottleMs) return false;
            _lastPreviewMs = now;
            return true;
        }

        // ----------------------------------------------------------------
        // Image helper — must NOT dispose image; caller owns it.
        // ----------------------------------------------------------------

        private static byte[] ToPngBytes(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        // ----------------------------------------------------------------
        // Gear (weapons / artifacts)
        // ----------------------------------------------------------------

        public void SetGear(Image<Rgba32> bm, Weapon weapon)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            var name = weapon?.Name ?? string.Empty;
            var valid = weapon?.IsValid() ?? false;
            var stats = weapon?.ToString() ?? string.Empty;
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemName = name;
                _scanVm.IsCurrentItemValid = valid;
                _scanVm.CurrentItemStats = stats.Split('\n');
            });
        }

        public void SetGear(Image<Rgba32> bm, Artifact artifact)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            var name = artifact?.SetName ?? string.Empty;
            var valid = artifact?.IsValid() ?? false;
            var stats = artifact?.ToString() ?? string.Empty;
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemName = name;
                _scanVm.IsCurrentItemValid = valid;
                _scanVm.CurrentItemStats = stats.Split('\n');
            });
        }

        public void SetGearPictureBox(Image<Rgba32> bm)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentScreenshot = png);
        }

        public void SetGearTextBox(string text)
        {
            var stats = (text ?? string.Empty).Split('\n');
            Dispatcher.UIThread.Post(() => _scanVm.CurrentItemStats = stats);
        }

        // ----------------------------------------------------------------
        // Character
        // ----------------------------------------------------------------

        public void SetCharacter_NameAndElement(Image<Rgba32> bm, string name, string element)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            var label = $"{name} ({element})";
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemName = label;
            });
        }

        public void SetCharacter_Level(Image<Rgba32> bm, int level, int maxLevel)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            var stats = new[] { $"Level {level} / {maxLevel}" };
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemStats = stats;
            });
        }

        public void SetCharacter_Constellation(int level)
        {
            _log.Info("Constellation: C{0}", level);
        }

        public void SetCharacter_Talent(Image<Rgba32> bm, string text, int i)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            _log.Info("Talent {0}: {1}", i, text);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentScreenshot = png);
        }

        // ----------------------------------------------------------------
        // Materials / Mora
        // ----------------------------------------------------------------

        public void SetMora(Image<Rgba32> mora, int count)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(mora);
            _log.Info("Mora: {0}", count);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentScreenshot = png);
        }

        // ----------------------------------------------------------------
        // Counters / Max
        // ----------------------------------------------------------------

        public void SetWeapon_Max(int value)
        {
            Dispatcher.UIThread.Post(() => _scanVm.WeaponsTotal = value);
        }

        public void SetArtifact_Max(int value)
        {
            Dispatcher.UIThread.Post(() => _scanVm.ArtifactsTotal = value);
        }

        public void IncrementWeaponCount()
        {
            Dispatcher.UIThread.Post(() => _scanVm.WeaponsScanned++);
        }

        public void IncrementArtifactCount()
        {
            Dispatcher.UIThread.Post(() => _scanVm.ArtifactsScanned++);
        }

        public void IncrementCharacterCount()
        {
            Dispatcher.UIThread.Post(() => _scanVm.CharactersScanned++);
        }

        // ----------------------------------------------------------------
        // Status and errors
        // ----------------------------------------------------------------

        public void SetProgramStatus(string status, bool ok = true)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.StatusMessage = status;
                _scanVm.StatusIsOk = ok;
            });
        }

        public void AddError(string error)
        {
            _log.Error(error);
        }

        public void SetMainCharacterName(string name)
        {
            Dispatcher.UIThread.Post(() => _scanVm.MainCharacterName = name);
        }

        // ----------------------------------------------------------------
        // Navigation image
        // ----------------------------------------------------------------

        public void SetNavigation_Image(Image<Rgba32> bm)
        {
            if (!ShouldUpdatePreview()) return;
            var png = ToPngBytes(bm);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentScreenshot = png);
        }

        // ----------------------------------------------------------------
        // Reset methods
        // ----------------------------------------------------------------

        public void ResetGearDisplay()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentItemName = null;
                _scanVm.CurrentItemStats = null;
                _scanVm.CurrentScreenshot = null;
                _scanVm.IsCurrentItemValid = null;
                _scanVm.ValidationMessage = null;
            });
        }

        public void ResetCharacterDisplay()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentItemName = null;
                _scanVm.CurrentItemStats = null;
                _scanVm.CurrentScreenshot = null;
                _scanVm.IsCurrentItemValid = null;
                _scanVm.ValidationMessage = null;
            });
        }

        public void ResetCounters()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.WeaponsScanned    = 0;
                _scanVm.WeaponsTotal      = 0;
                _scanVm.ArtifactsScanned  = 0;
                _scanVm.ArtifactsTotal    = 0;
                _scanVm.CharactersScanned = 0;
                _scanVm.CharactersTotal   = 0;
                _scanVm.MaterialsScanned  = 0;
                _scanVm.MaterialsTotal    = 0;
            });
        }

        public void ResetErrors()
        {
            // The console is managed by ObservableCollectionLogTarget (bounded to 5 000
            // entries); there is no separate error collection to clear.  No-op here.
        }

        public void ResetAll()
        {
            ResetGearDisplay();
            ResetCharacterDisplay();
            ResetCounters();
            ResetErrors();
        }
    }
}
