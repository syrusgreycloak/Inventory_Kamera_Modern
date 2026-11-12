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

        /// <summary>
        /// The window title with the app version, e.g. "Inventory Kamera Modern v2.1.0" (the release
        /// build sets the version from the tag).
        /// </summary>
        public string WindowTitle
        {
            get
            {
                var v = typeof(MainViewModel).Assembly.GetName().Version;
                return "Inventory Kamera Modern v" + (v != null ? v.ToString(3) : "0.0.0");
            }
        }

        /// <summary>The selected tab's view model.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedTabIndex))]
        private object _selectedTab;

        /// <summary>Tab order, matching MainWindow.axaml.</summary>
        private object[] Tabs => new object[] { ScanVm, SettingsVm, ExportVm, AboutVm };

        /// <summary>Index of <see cref="SelectedTab"/>, for binding to the TabControl.</summary>
        public int SelectedTabIndex
        {
            get => System.Array.IndexOf(Tabs, SelectedTab);
            set
            {
                if (value >= 0 && value < Tabs.Length) SelectedTab = Tabs[value];
            }
        }

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
            // Open on Settings until the app is set up (first start, or no output folder)
            _selectedTab = _settingsVm.NeedsSetup ? (object)_settingsVm : _scanVm;
        }
    }
}
