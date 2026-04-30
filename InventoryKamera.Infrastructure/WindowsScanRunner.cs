using InventoryKamera.Configuration;
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
        private readonly IUserInterface      _userInterface;
        private readonly IGameDataService    _gameDataService;
        private readonly IScanProfileService _scanProfile;

        // Results from the last completed scan — null until a scan finishes.
        private InventoryScanner _lastRun;

        public bool HasData       => _lastRun != null && _lastRun.HasData;
        public List<Character> Characters => _lastRun?.Characters ?? new List<Character>();
        public Inventory Inventory        => _lastRun?.Inventory  ?? new Inventory();

        public WindowsScanRunner(
            IScreenCapture      screenCapture,
            IInputSimulator     inputSimulator,
            IOcrEngine          ocrEngine,
            IImageProcessor     imageProcessor,
            IUserInterface      userInterface,
            IGameDataService    gameDataService,
            IScanProfileService scanProfile)
        {
            _screenCapture   = screenCapture   ?? throw new ArgumentNullException(nameof(screenCapture));
            _inputSimulator  = inputSimulator  ?? throw new ArgumentNullException(nameof(inputSimulator));
            _ocrEngine       = ocrEngine       ?? throw new ArgumentNullException(nameof(ocrEngine));
            _imageProcessor  = imageProcessor  ?? throw new ArgumentNullException(nameof(imageProcessor));
            _userInterface   = userInterface   ?? throw new ArgumentNullException(nameof(userInterface));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _scanProfile     = scanProfile     ?? throw new ArgumentNullException(nameof(scanProfile));
        }

        public Task RunAsync(ScanSettings settings, CancellationToken token = default)
        {
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
                _userInterface,
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
