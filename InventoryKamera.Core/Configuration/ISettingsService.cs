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
        /// Triggers a debounced save.  Multiple calls within the debounce window
        /// coalesce into a single file write.
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
