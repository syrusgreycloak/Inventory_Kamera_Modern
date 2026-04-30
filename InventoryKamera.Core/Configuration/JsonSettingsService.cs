using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace InventoryKamera.Configuration
{
    /// <summary>
    /// JSON-backed implementation of <see cref="ISettingsService"/>.
    /// <para>
    /// Storage path is determined by the <c>Func&lt;string&gt;</c> supplied at construction,
    /// allowing tests to inject a temp path.  Production code passes
    /// <see cref="DefaultPath"/> as the factory.
    /// </para>
    /// <para>
    /// Writes are debounced: back-to-back <see cref="Save"/> calls within 500 ms are
    /// coalesced into a single disk write.  The write is atomic — the JSON is first
    /// written to a <c>.tmp</c> sibling file and then renamed over the target.
    /// </para>
    /// </summary>
    public sealed class JsonSettingsService : ISettingsService, IDisposable
    {
        // ------------------------------------------------------------------ //
        //  Constants                                                           //
        // ------------------------------------------------------------------ //

        private const int DebounceMs = 500;

        // ------------------------------------------------------------------ //
        //  Fields                                                              //
        // ------------------------------------------------------------------ //

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Func<string> _pathFactory;

        private ScanSettings _current;
        private Timer _debounceTimer;
        private bool _hasPendingSave;
        private readonly object _saveLock = new object();
        private bool _disposed;

        // ------------------------------------------------------------------ //
        //  Construction / initialisation                                       //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a settings service that stores data at the path returned by
        /// <paramref name="pathFactory"/>.
        /// </summary>
        /// <param name="pathFactory">
        /// Called each time the path is needed.  Pass a lambda that returns a
        /// constant string, or <see cref="DefaultPath"/> for the production path.
        /// </param>
        public JsonSettingsService(Func<string> pathFactory)
        {
            _pathFactory = pathFactory ?? throw new ArgumentNullException(nameof(pathFactory));
            _debounceTimer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            LoadFromDisk();
        }

        // ------------------------------------------------------------------ //
        //  ISettingsService                                                    //
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public ScanSettings Current => _current;

        /// <inheritdoc />
        public event EventHandler SettingsChanged;

        /// <inheritdoc />
        public void Save()
        {
            lock (_saveLock)
            {
                if (_disposed) return;
                _hasPendingSave = true;
                // Reset the debounce timer: fire once, 500 ms after the LAST call.
                _debounceTimer.Change(DebounceMs, Timeout.Infinite);
            }
        }

        /// <inheritdoc />
        public void Reload()
        {
            LoadFromDisk();
        }

        // ------------------------------------------------------------------ //
        //  Public helpers                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the default settings file path:
        /// <c>%AppData%\InventoryKamera\settings.json</c>.
        /// </summary>
        public static string DefaultPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "InventoryKamera",
                "settings.json");

        /// <summary>
        /// Forces any pending debounced write to disk immediately and synchronously.
        /// Internal so test code (via <c>InternalsVisibleTo</c>) can deterministically
        /// flush without waiting for the timer.
        /// </summary>
        internal void FlushPendingSave()
        {
            lock (_saveLock)
            {
                if (!_hasPendingSave) return;
                // Cancel the timer so it doesn't fire again after us.
                _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            WriteToDisk();
        }

        // ------------------------------------------------------------------ //
        //  IDisposable                                                         //
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public void Dispose()
        {
            bool hadPending;
            lock (_saveLock)
            {
                if (_disposed) return;
                _disposed = true;
                // Cancel the timer and atomically claim any pending write so the timer
                // callback cannot also trigger a write (eliminates double-write race).
                _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                hadPending = _hasPendingSave;
                _hasPendingSave = false;
            }

            // Flush outside the lock to avoid deadlock if WriteToDisk raises SettingsChanged.
            if (hadPending) WriteToDisk();

            _debounceTimer.Dispose();
        }

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private void TimerCallback(object state)
        {
            lock (_saveLock)
            {
                if (_disposed || !_hasPendingSave) return;
            }
            WriteToDisk();
        }

        private void WriteToDisk()
        {
            string filePath = _pathFactory();
            string dir = Path.GetDirectoryName(filePath);
            if (dir != null) Directory.CreateDirectory(dir);

            string tmpPath = filePath + ".tmp";

            try
            {
                ScanSettings snapshot;
                lock (_saveLock)
                {
                    snapshot = _current;
                    _hasPendingSave = false;
                }

                string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                File.WriteAllText(tmpPath, json);

                // Atomic rename — overwrites destination if it exists.
                // File.Move with overwrite parameter requires .NET 3.0+; available on net8.0.
                File.Move(tmpPath, filePath, overwrite: true);

                Logger.Debug("Settings saved to {0}", filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to write settings to {0}", filePath);
                // Clean up orphaned .tmp file
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { /* ignore */ }
                return;
            }

            // Raise the event outside any lock.
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void LoadFromDisk()
        {
            string filePath = _pathFactory();

            if (!File.Exists(filePath))
            {
                lock (_saveLock) { _current = new ScanSettings(); }
                Logger.Debug("Settings file not found at {0}; using defaults.", filePath);
                return;
            }

            string json = null;
            try
            {
                json = File.ReadAllText(filePath);
                var loaded = JsonConvert.DeserializeObject<ScanSettings>(json);
                if (loaded == null)
                {
                    Logger.Warn("Settings file at {0} deserialized to null; using defaults.", filePath);
                    lock (_saveLock) { _current = new ScanSettings(); }
                }
                else
                {
                    lock (_saveLock) { _current = loaded; }
                    Logger.Debug("Settings loaded from {0}", filePath);
                }
            }
            catch (JsonException ex)
            {
                Logger.Warn(ex, "Corrupt settings file at {0}; falling back to defaults.", filePath);
                RenameCorruptFile(filePath);
                lock (_saveLock) { _current = new ScanSettings(); }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error reading settings from {0}; using defaults.", filePath);
                lock (_saveLock) { _current = new ScanSettings(); }
            }
        }

        private static void RenameCorruptFile(string filePath)
        {
            try
            {
                string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                string corruptPath = filePath + ".corrupt-" + timestamp;
                File.Move(filePath, corruptPath, overwrite: false);
                Logger.Warn("Renamed corrupt settings file to {0}", corruptPath);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not rename corrupt settings file {0}; leaving in place.", filePath);
            }
        }
    }
}
