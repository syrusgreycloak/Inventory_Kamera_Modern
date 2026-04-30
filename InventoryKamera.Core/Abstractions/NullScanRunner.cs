using InventoryKamera.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryKamera
{
    /// <summary>
    /// No-op implementation of <see cref="IScanRunner"/> for non-Windows platforms
    /// and for unit-test stubs that do not need real scan behaviour.
    /// </summary>
    public class NullScanRunner : IScanRunner
    {
        public bool HasData => false;
        public List<Character> Characters => new List<Character>();
        public Inventory Inventory => new Inventory();

        public Task RunAsync(ScanSettings settings, CancellationToken token = default)
        {
            // On non-Windows this throws; callers (ScanViewModel) should check
            // RuntimeInformation.IsOSPlatform(Windows) before invoking.
            throw new PlatformNotSupportedException("Scanning is only supported on Windows.");
        }
    }
}
