namespace InventoryKamera.Infrastructure
{
    internal class WindowsInputSimulator : IInputSimulator
    {
        public void Click() => Navigation.Click();
        public void Click(int x, int y) => Navigation.Click(x, y);
        public void SetCursor(int x, int y) => Navigation.SetCursor(x, y);
        public void ScrollDown(int scrolls) => Navigation.Scroll(Navigation.Direction.DOWN, scrolls);
        public void ScrollUp(int scrolls) => Navigation.Scroll(Navigation.Direction.UP, scrolls);
        public void Wait(int ms) => Navigation.Wait(ms);
        public void MouseVerticalScroll(int clicks) => Navigation.sim.Mouse.VerticalScroll(clicks);
        public void SystemWait(ScanDelay delay = ScanDelay.Normal) => Navigation.SystemWait((Navigation.Speed)(int)delay);
        public void SetDelay(double ms) => Navigation.SetDelay(ms);
        public double GetDelay() => Navigation.GetDelay();
        public void SelectNextCharacter() => Navigation.SelectNextCharacter();
        public void SelectCharacterAttributes() => Navigation.SelectCharacterAttributes();
        public void SelectCharacterConstellation() => Navigation.SelectCharacterConstellation();
        public void SelectCharacterTalents() => Navigation.SelectCharacterTalents();
        public void ClearArtifactFilters() => Navigation.ClearArtifactFilters();
    }
}
