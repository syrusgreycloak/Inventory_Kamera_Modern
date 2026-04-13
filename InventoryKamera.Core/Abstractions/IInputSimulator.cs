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

        // New
        void MouseVerticalScroll(int clicks);
        void SystemWait(ScanDelay delay = ScanDelay.Normal);
        void SetDelay(double ms);
        double GetDelay();

        // Game navigation actions
        void SelectNextCharacter();
        void SelectCharacterAttributes();
        void SelectCharacterConstellation();
        void SelectCharacterTalents();
    }
}
