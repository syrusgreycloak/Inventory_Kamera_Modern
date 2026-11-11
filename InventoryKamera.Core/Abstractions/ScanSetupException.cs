using System;

namespace InventoryKamera
{
    /// <summary>
    /// A scan can't start because of something the user can fix (e.g. missing game data). The
    /// message is shown to the user as is, so it should say what to do.
    /// </summary>
    public class ScanSetupException : Exception
    {
        public ScanSetupException(string message) : base(message) { }
    }
}
