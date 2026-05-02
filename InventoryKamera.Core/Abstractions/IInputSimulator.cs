namespace InventoryKamera
{
    public interface IInputSimulator
    {
        // Existing
        void Click();
        void Click(int x, int y);
        void SetCursor(int x, int y);
        void ScrollDown(int scrolls);
        void ScrollUp(int scrolls);
        void Wait(int ms);
        void SystemWaitMs(int ms);

        // New
        void MouseVerticalScroll(int clicks);
        void SystemWait(ScanDelay delay = ScanDelay.Normal);
        void SetDelay(double ms);
        double GetDelay();

        // Re-asserts the game window as the foreground window. No-op if already foreground.
        // Call before any capture/click cycle that follows arbitrary delays — Windows can
        // surface other apps' windows mid-scan (Rider, notifications, etc.) and once Genshin
        // is occluded, GDI screen capture returns black or wrong content.
        void EnsureForeground();

        // Game navigation actions
        void SelectNextCharacter();
        void SelectCharacterAttributes();
        void SelectCharacterConstellation();
        void SelectCharacterTalents();
        void ClearArtifactFilters();
    }
}
