namespace InventoryKamera
{
    public interface IInputSimulator
    {
        void Click();
        void Click(int x, int y);
        void SetCursor(int x, int y);
        void ScrollDown(int scrolls);
        void ScrollUp(int scrolls);
        void Wait(int ms);
    }
}
