using System;

namespace InventoryKamera.Configuration
{
    /// <summary>
    /// Cross-platform settings service.  Exposes strongly-typed <see cref="ScanSettings"/>
    /// and persists them to a JSON file under the user's AppData folder.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// The live settings object.  Callers mutate properties on this instance
        /// directly then call <see cref="Save"/> to persist.
        /// </summary>
        ScanSettings Current { get; }

        /// <summary>
        /// Schedules a write.  Back-to-back calls within 500 ms are coalesced
        /// (debounced) into a single file write.  Use
        /// <see cref="JsonSettingsService.FlushPendingSave"/> (internal) or
        /// <see cref="IDisposable.Dispose"/> to force an immediate write.
        /// </summary>
        void Save();

        /// <summary>
        /// Re-reads the settings file from disk, replacing <see cref="Current"/>
        /// with freshly deserialized values.  Useful after an external edit.
        /// </summary>
        void Reload();

        /// <summary>
        /// Raised on the timer thread after a successful write completes.
        /// </summary>
        event EventHandler SettingsChanged;
    }
}
