using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using NLog;
using NLog.Targets;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// NLog target that appends log events to an <see cref="ObservableCollection{ConsoleEntry}"/>
    /// on the Avalonia UI thread.  Register one rule in NLog config pointing to this target
    /// to populate the scan console.
    /// </summary>
    [Target("ObservableCollection")]
    public sealed class ObservableCollectionLogTarget : TargetWithLayout
    {
        private readonly ObservableCollection<ConsoleEntry> _collection;

        /// <summary>Maximum number of entries to keep in the collection (oldest removed first).</summary>
        public int MaxEntries { get; set; } = 5000;

        public ObservableCollectionLogTarget(ObservableCollection<ConsoleEntry> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var entry = new ConsoleEntry(logEvent.TimeStamp, logEvent.Level, RenderLogEvent(Layout, logEvent));

            if (Dispatcher.UIThread.CheckAccess())
            {
                AppendEntry(entry);
            }
            else
            {
                Dispatcher.UIThread.Post(() => AppendEntry(entry));
            }
        }

        private void AppendEntry(ConsoleEntry entry)
        {
            if (_collection.Count >= MaxEntries)
                _collection.RemoveAt(0);
            _collection.Add(entry);
        }
    }
}
