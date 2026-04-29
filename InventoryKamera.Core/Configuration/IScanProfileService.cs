namespace InventoryKamera.Configuration
{
    public interface IScanProfileService
    {
        AspectRatioProfile ActiveProfile { get; }
        bool IsLoaded { get; }
        void Load(string profilePath, double windowAspectRatio);
    }
}
