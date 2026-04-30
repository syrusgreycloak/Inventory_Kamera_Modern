using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InventoryKamera.Configuration;

namespace InventoryKamera
{
    /// <summary>
    /// Abstraction over the inventory scan orchestration.
    /// Implemented by Windows infrastructure; stubbed on non-Windows.
    /// </summary>
    public interface IScanRunner
    {
        /// <summary>
        /// Runs a full inventory scan with the given settings snapshot.
        /// The token is monitored at each category boundary.
        /// </summary>
        Task RunAsync(ScanSettings settings, CancellationToken token = default);

        /// <summary>Whether the last completed scan produced any data.</summary>
        bool HasData { get; }

        /// <summary>Characters scanned in the last run.</summary>
        List<Character> Characters { get; }

        /// <summary>Inventory (weapons, artifacts, materials) from the last run.</summary>
        Inventory Inventory { get; }
    }
}
