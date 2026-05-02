using System;
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
    /// the method returns).
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
        // Per-weapon updates from the worker pool fire at most a few per second
        // (full OCR per weapon is slow), so a throttle gate dropped legitimate
        // SetGear calls under concurrent load and froze the preview after the
        // first weapon. Throttle removed — Dispatcher.Post is cheap enough.
        // ----------------------------------------------------------------

        public AvaloniaUserInterface(ScanViewModel scanVm)
        {
            _scanVm = scanVm ?? throw new ArgumentNullException(nameof(scanVm));
        }

        // ----------------------------------------------------------------
        // Image helper — must NOT dispose image; caller owns it.
        // ----------------------------------------------------------------

        // Returns null when the caller passed null. MaterialScraper, for example, calls
        // SetMora(null, count) — IUserInterface implementations must tolerate that.
        private static byte[] ToPngBytes(Image<Rgba32> image)
        {
            if (image == null) return null;
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        // ----------------------------------------------------------------
        // Gear (weapons / artifacts)
        // ----------------------------------------------------------------

        public void SetGear(Image<Rgba32> bm, Weapon weapon)
        {
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
        //
        // Character info arrives in pieces: name+element, then level, then constellation,
        // then three talents. Each comes through a different IUserInterface call. To show
        // the running combined info in the OCR preview pane (mirroring the WinForms
        // behaviour), keep a per-character buffer keyed by line type. SetCharacter_NameAndElement
        // resets the buffer (it's the first call for each new character). All other Set*
        // methods overwrite the slot they own and re-publish the full list.

        private readonly object _charLock = new object();
        private string _charLevelLine;
        private string _charConstellationLine;
        private readonly string[] _charTalentLines = new string[3];

        private string[] BuildCharacterStats()
        {
            var lines = new System.Collections.Generic.List<string>(5);
            if (!string.IsNullOrEmpty(_charLevelLine))         lines.Add(_charLevelLine);
            if (!string.IsNullOrEmpty(_charConstellationLine)) lines.Add(_charConstellationLine);
            for (int i = 0; i < 3; i++)
                if (!string.IsNullOrEmpty(_charTalentLines[i])) lines.Add(_charTalentLines[i]);
            return lines.ToArray();
        }

        public void SetCharacter_NameAndElement(Image<Rgba32> bm, string name, string element)
        {
            var png = ToPngBytes(bm);
            var label = $"{name} ({element})";
            string[] stats;
            lock (_charLock)
            {
                _charLevelLine = null;
                _charConstellationLine = null;
                _charTalentLines[0] = _charTalentLines[1] = _charTalentLines[2] = null;
                stats = BuildCharacterStats();
            }
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemName = label;
                _scanVm.CurrentItemStats = stats;
            });
        }

        public void SetCharacter_Level(Image<Rgba32> bm, int level, int maxLevel)
        {
            var png = ToPngBytes(bm);
            string[] stats;
            lock (_charLock)
            {
                _charLevelLine = $"Level {level} / {maxLevel}";
                stats = BuildCharacterStats();
            }
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemStats = stats;
            });
        }

        public void SetCharacter_Constellation(int level)
        {
            string[] stats;
            lock (_charLock)
            {
                _charConstellationLine = $"Constellation: C{level}";
                stats = BuildCharacterStats();
            }
            _log.Info("Constellation: C{0}", level);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentItemStats = stats);
        }

        public void SetCharacter_Talent(Image<Rgba32> bm, string text, int i)
        {
            var png = ToPngBytes(bm);
            string[] stats;
            // Talent index from CharacterScraper is 0-based (0=Auto, 1=Skill, 2=Burst).
            int slot = (i >= 0 && i <= 2) ? i : 0;
            string label = i == 0 ? "Auto" : i == 1 ? "Skill" : i == 2 ? "Burst" : $"Talent {i}";
            lock (_charLock)
            {
                _charTalentLines[slot] = $"{label}: {text}";
                stats = BuildCharacterStats();
            }
            _log.Info("Talent {0} ({1}): {2}", i, label, text);
            Dispatcher.UIThread.Post(() =>
            {
                _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemStats = stats;
            });
        }

        // ----------------------------------------------------------------
        // Materials / Mora
        // ----------------------------------------------------------------

        public void SetMora(Image<Rgba32> mora, int count)
        {
            _log.Info("Mora: {0}", count);
            // Some scrapers (MaterialScraper) call SetMora with a null bitmap when they
            // only have a count to surface. Skip the screenshot update in that case so
            // we don't clear the existing preview.
            if (mora == null) return;
            var png = ToPngBytes(mora);
            Dispatcher.UIThread.Post(() => _scanVm.CurrentScreenshot = png);
        }

        public void SetMaterial(Image<Rgba32> bitmap, string name, int count)
        {
            // Surface per-material progress: nameplate as the screenshot, name + count
            // as the OCR-preview header / stats. Mirrors the SetGear pattern.
            var png = ToPngBytes(bitmap);
            var stats = new[] { $"Count: {count}" };
            Dispatcher.UIThread.Post(() =>
            {
                if (png != null) _scanVm.CurrentScreenshot = png;
                _scanVm.CurrentItemName = name ?? string.Empty;
                _scanVm.CurrentItemStats = stats;
                _scanVm.IsCurrentItemValid = !string.IsNullOrEmpty(name);
            });
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
