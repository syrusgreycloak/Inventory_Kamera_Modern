using System;
using NLog;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Represents a single timestamped log entry displayed in the scan console.
    /// </summary>
    public class ConsoleEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public LogLevel Level { get; set; }
    }
}
