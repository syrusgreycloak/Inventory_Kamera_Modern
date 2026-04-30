using System;
using NLog;

namespace InventoryKamera.Avalonia.Services
{
    /// <summary>
    /// Represents a single timestamped log entry displayed in the scan console.
    /// </summary>
    public record ConsoleEntry(DateTime Timestamp, LogLevel Level, string Message);
}
