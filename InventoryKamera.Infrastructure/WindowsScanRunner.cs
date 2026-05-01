using InventoryKamera.Configuration;
using InventoryKamera.Infrastructure.Windows;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryKamera.Infrastructure
{
    /// <summary>
    /// Windows-only implementation of <see cref="IScanRunner"/>.
    /// Constructs and drives <see cref="InventoryKamera.InventoryScanner"/> using
    /// injected services so the Avalonia DI container can wire everything up.
    /// </summary>
    public class WindowsScanRunner : IScanRunner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IScreenCapture      _screenCapture;
        private readonly IInputSimulator     _inputSimulator;
        private readonly IOcrEngine          _ocrEngine;
        private readonly IImageProcessor     _imageProcessor;
        private readonly Func<IUserInterface> _userInterfaceFactory;
        private readonly IGameDataService    _gameDataService;
        private readonly IScanProfileService _scanProfile;

        // Results from the last completed scan — null until a scan finishes.
        private InventoryScanner _lastRun;

        public bool HasData       => _lastRun != null && _lastRun.HasData;
        public List<Character> Characters => _lastRun?.Characters ?? new List<Character>();
        public Inventory Inventory        => _lastRun?.Inventory  ?? new Inventory();

        // IUserInterface is resolved via factory (not constructor-injected) to break a DI cycle:
        // ScanViewModel → IScanRunner → IUserInterface (AvaloniaUserInterface) → ScanViewModel.
        // The runner only needs IUserInterface at scan-start time, after the graph is built.
        public WindowsScanRunner(
            IScreenCapture      screenCapture,
            IInputSimulator     inputSimulator,
            IOcrEngine          ocrEngine,
            IImageProcessor     imageProcessor,
            Func<IUserInterface> userInterfaceFactory,
            IGameDataService    gameDataService,
            IScanProfileService scanProfile)
        {
            _screenCapture        = screenCapture   ?? throw new ArgumentNullException(nameof(screenCapture));
            _inputSimulator       = inputSimulator  ?? throw new ArgumentNullException(nameof(inputSimulator));
            _ocrEngine            = ocrEngine       ?? throw new ArgumentNullException(nameof(ocrEngine));
            _imageProcessor       = imageProcessor  ?? throw new ArgumentNullException(nameof(imageProcessor));
            _userInterfaceFactory = userInterfaceFactory ?? throw new ArgumentNullException(nameof(userInterfaceFactory));
            _gameDataService      = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _scanProfile          = scanProfile     ?? throw new ArgumentNullException(nameof(scanProfile));
        }

        public Task RunAsync(ScanSettings settings, CancellationToken token = default)
        {
            // Locate the Genshin process and capture its window dimensions BEFORE reading
            // aspect ratio or capturing screenshots. Without this, Navigation's static
            // width/height are 0 and GetAspectRatio() throws DivideByZeroException.
            // Use the configured executable list so future renames (e.g. CN client) are
            // honored from settings rather than the hardcoded fallback.
            if (settings?.Executables != null && settings.Executables.Count > 0)
                Navigation.Initialize(settings.Executables);
            else
                Navigation.Initialize();

            // Apply the user-configured per-step delay (Fast/Normal/Slow → ms).
            if (settings != null) _inputSimulator.SetDelay(settings.ScannerDelay);

            // Detect aspect ratio and load the scan profile for this run.
            var aspect = _screenCapture.GetAspectRatio();
            double aspectRatio = aspect.Height > 0 ? (double)aspect.Width / aspect.Height : 16.0 / 9.0;
            string profilePath = Path.Combine(AppContext.BaseDirectory, "inventorylists", "ScanProfile.json");
            _scanProfile.Load(profilePath, aspectRatio);

            var ik = new InventoryScanner(
                _screenCapture,
                _inputSimulator,
                _ocrEngine,
                _imageProcessor,
                _userInterfaceFactory(),
                _gameDataService,
                _scanProfile,
                settings);

            return Task.Run(() =>
            {
                ik.GatherData(token);
                // Store results regardless of cancellation so partial data is accessible.
                _lastRun = ik;
            }, token);
        }
    }
}
