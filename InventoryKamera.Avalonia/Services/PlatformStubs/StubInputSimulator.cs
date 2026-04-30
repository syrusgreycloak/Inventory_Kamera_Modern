using System;

namespace InventoryKamera.Avalonia.Services.PlatformStubs
{
    /// <summary>
    /// Non-Windows stub for <see cref="IInputSimulator"/>.
    /// Every method throws <see cref="PlatformNotSupportedException"/>.
    /// The ScanViewModel platform guard prevents these from being reached at runtime.
    /// </summary>
    public class StubInputSimulator : IInputSimulator
    {
        private static void Throw() =>
            throw new PlatformNotSupportedException("Scanning requires Windows.");

        public void Click()                                     { Throw(); }
        public void Click(int x, int y)                         { Throw(); }
        public void SetCursor(int x, int y)                     { Throw(); }
        public void ScrollDown(int scrolls)                     { Throw(); }
        public void ScrollUp(int scrolls)                       { Throw(); }
        public void Wait(int ms)                                { Throw(); }
        public void SystemWaitMs(int ms)                        { Throw(); }
        public void MouseVerticalScroll(int clicks)             { Throw(); }
        public void SystemWait(ScanDelay delay = ScanDelay.Normal) { Throw(); }
        public void SetDelay(double ms)                         { Throw(); }
        public double GetDelay()                                { Throw(); return 0; }
        public void SelectNextCharacter()                       { Throw(); }
        public void SelectCharacterAttributes()                 { Throw(); }
        public void SelectCharacterConstellation()              { Throw(); }
        public void SelectCharacterTalents()                    { Throw(); }
        public void ClearArtifactFilters()                      { Throw(); }
    }
}
