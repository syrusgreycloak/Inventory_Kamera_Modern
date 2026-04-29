using CommunityToolkit.Mvvm.ComponentModel;

namespace InventoryKamera.Avalonia.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ScanViewModel _scanVm;

        [ObservableProperty]
        private SettingsViewModel _settingsVm;

        [ObservableProperty]
        private ExportViewModel _exportVm;

        [ObservableProperty]
        private AboutViewModel _aboutVm;

        [ObservableProperty]
        private object _selectedTab;

        public MainViewModel(
            ScanViewModel scanVm,
            SettingsViewModel settingsVm,
            ExportViewModel exportVm,
            AboutViewModel aboutVm)
        {
            _scanVm = scanVm;
            _settingsVm = settingsVm;
            _exportVm = exportVm;
            _aboutVm = aboutVm;
            _selectedTab = _scanVm;
        }
    }
}
