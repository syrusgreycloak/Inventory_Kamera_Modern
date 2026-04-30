using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Configuration;

namespace InventoryKamera.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings tab.
    /// All properties write through to <see cref="ISettingsService.Current"/>
    /// and call <see cref="ISettingsService.Save"/> on each change (auto-save pattern).
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settings;
        private readonly IFolderPickerService _folderPicker;

        // ─────────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────────

        public SettingsViewModel(
            ISettingsService settings,
            IFolderPickerService folderPicker)
        {
            _settings      = settings      ?? throw new ArgumentNullException(nameof(settings));
            _folderPicker  = folderPicker  ?? throw new ArgumentNullException(nameof(folderPicker));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Output
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Directory path where exported GOOD JSON is saved.</summary>
        public string OutputPath
        {
            get => _settings.Current.OutputPath;
            set
            {
                if (_settings.Current.OutputPath == value) return;
                _settings.Current.OutputPath = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Item filters
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Minimum weapon star rarity to include (3–5).</summary>
        public int MinimumWeaponRarity
        {
            get => _settings.Current.MinimumWeaponRarity;
            set
            {
                if (_settings.Current.MinimumWeaponRarity == value) return;
                _settings.Current.MinimumWeaponRarity = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Minimum weapon level to include (1–90).</summary>
        public int MinimumWeaponLevel
        {
            get => _settings.Current.MinimumWeaponLevel;
            set
            {
                if (_settings.Current.MinimumWeaponLevel == value) return;
                _settings.Current.MinimumWeaponLevel = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Minimum artifact star rarity to include (1–5).</summary>
        public int MinimumArtifactRarity
        {
            get => _settings.Current.MinimumArtifactRarity;
            set
            {
                if (_settings.Current.MinimumArtifactRarity == value) return;
                _settings.Current.MinimumArtifactRarity = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Minimum artifact level to include (0–20).</summary>
        public int MinimumArtifactLevel
        {
            get => _settings.Current.MinimumArtifactLevel;
            set
            {
                if (_settings.Current.MinimumArtifactLevel == value) return;
                _settings.Current.MinimumArtifactLevel = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Include equipped weapons in scan output.</summary>
        public bool EquipWeapons
        {
            get => _settings.Current.EquipWeapons;
            set
            {
                if (_settings.Current.EquipWeapons == value) return;
                _settings.Current.EquipWeapons = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Include equipped artifacts in scan output.</summary>
        public bool EquipArtifacts
        {
            get => _settings.Current.EquipArtifacts;
            set
            {
                if (_settings.Current.EquipArtifacts == value) return;
                _settings.Current.EquipArtifacts = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Hotkeys — stored as Avalonia Key enum values cast to int
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Avalonia Key int for the inventory menu shortcut.</summary>
        public int InventoryKey
        {
            get => _settings.Current.InventoryKey;
            set
            {
                if (_settings.Current.InventoryKey == value) return;
                _settings.Current.InventoryKey = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Avalonia Key int for the character screen shortcut.</summary>
        public int CharacterKey
        {
            get => _settings.Current.CharacterKey;
            set
            {
                if (_settings.Current.CharacterKey == value) return;
                _settings.Current.CharacterKey = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Avalonia Key int for the equipment slot-1 shortcut.</summary>
        public int Slot1Key
        {
            get => _settings.Current.Slot1Key;
            set
            {
                if (_settings.Current.Slot1Key == value) return;
                _settings.Current.Slot1Key = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Advanced
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Raw scanner step delay in milliseconds.</summary>
        public int ScannerDelay
        {
            get => _settings.Current.ScannerDelay;
            set
            {
                if (_settings.Current.ScannerDelay == value) return;
                _settings.Current.ScannerDelay = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>In-game name of the Traveler character (user-specific).</summary>
        public string TravelerName
        {
            get => _settings.Current.TravelerName;
            set
            {
                if (_settings.Current.TravelerName == value) return;
                _settings.Current.TravelerName = value;
                _settings.Save();
                OnPropertyChanged();
            }
        }

        /// <summary>Date of the last lookup-table update check (read-only display).</summary>
        public DateTime LastUpdateCheck => _settings.Current.LastUpdateCheck;

        // ─────────────────────────────────────────────────────────────────────
        // Commands
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Opens a folder picker and sets <see cref="OutputPath"/> on success.</summary>
        [RelayCommand]
        private async Task BrowseOutputPath()
        {
            var picked = await _folderPicker.PickFolderAsync(_settings.Current.OutputPath);
            if (!string.IsNullOrEmpty(picked))
                OutputPath = picked;
        }
    }
}
