namespace InventoryKamera.Infrastructure
{
    internal class WindowsInputSimulator : IInputSimulator
    {
        public void Click()
        {
            Navigation.Click();
        }

        public void Click(int x, int y)
        {
            Navigation.Click(x, y);
        }

        public void SetCursor(int x, int y)
        {
            Navigation.SetCursor(x, y);
        }

        public void ScrollDown(int scrolls)
        {
            Navigation.Scroll(Navigation.Direction.DOWN, scrolls);
        }

        public void ScrollUp(int scrolls)
        {
            Navigation.Scroll(Navigation.Direction.UP, scrolls);
        }

        public void Wait(int ms)
        {
            Navigation.Wait(ms);
        }
    }
}
