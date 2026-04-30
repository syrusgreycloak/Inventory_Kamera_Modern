using System;
using System.Threading;
using System.Threading.Tasks;
using InventoryKamera;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Avalonia.ViewModels;
using InventoryKamera.Configuration;
using Moq;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for <see cref="MainViewModel"/>.
    /// Verifies constructor DI wiring and initial tab state.
    /// </summary>
    public class MainViewModelTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static (
            MainViewModel main,
            ScanViewModel scan,
            SettingsViewModel settings,
            ExportViewModel export,
            AboutViewModel about)
            MakeMainVm()
        {
            // Build the full VM tree the same way the DI container would.
            var settingsMock = new Mock<ISettingsService>(MockBehavior.Loose);
            settingsMock.Setup(s => s.Current).Returns(new ScanSettings
            {
                OutputPath     = "",
                EquipWeapons   = true,
                EquipArtifacts = true
            });

            var runner = new Mock<IScanRunner>(MockBehavior.Loose);
            runner.Setup(r => r.RunAsync(It.IsAny<ScanSettings>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

            var dialog = new Mock<IDialogService>(MockBehavior.Loose);
            dialog.Setup(d => d.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Task.CompletedTask);

            var picker     = new Mock<IFolderPickerService>(MockBehavior.Loose);
            var clipboard  = new Mock<IClipboardService>(MockBehavior.Loose);
            var url        = new Mock<IUrlLauncher>(MockBehavior.Loose);

            var scan = new ScanViewModel(settingsMock.Object, runner.Object, dialog.Object);
            var settingsVm = new SettingsViewModel(settingsMock.Object, picker.Object);
            var export = new ExportViewModel(scan, settingsMock.Object, clipboard.Object, url.Object, dialog.Object);
            var about  = new AboutViewModel(settingsMock.Object, url.Object, dialog.Object, new DatabaseManager());

            var main = new MainViewModel(scan, settingsVm, export, about);

            return (main, scan, settingsVm, export, about);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 1: Constructor wires child view models from DI
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Constructor_Wires_ScanVm()
        {
            var (main, scan, _, _, _) = MakeMainVm();
            Assert.Same(scan, main.ScanVm);
        }

        [Fact]
        public void Constructor_Wires_SettingsVm()
        {
            var (main, _, settings, _, _) = MakeMainVm();
            Assert.Same(settings, main.SettingsVm);
        }

        [Fact]
        public void Constructor_Wires_ExportVm()
        {
            var (main, _, _, export, _) = MakeMainVm();
            Assert.Same(export, main.ExportVm);
        }

        [Fact]
        public void Constructor_Wires_AboutVm()
        {
            var (main, _, _, _, about) = MakeMainVm();
            Assert.Same(about, main.AboutVm);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 2: Initial selected tab is ScanVm
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void SelectedTab_Defaults_To_ScanVm()
        {
            var (main, scan, _, _, _) = MakeMainVm();
            Assert.Same(scan, main.SelectedTab);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 3: SelectedTab is observable
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void SelectedTab_Fires_PropertyChanged()
        {
            var (main, _, settings, _, _) = MakeMainVm();

            var changes = new System.Collections.Generic.List<string>();
            main.PropertyChanged += (_, e) => changes.Add(e.PropertyName);

            main.SelectedTab = settings;

            Assert.Contains("SelectedTab", changes);
            Assert.Same(settings, main.SelectedTab);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Test 4: None of the child VMs are null
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void All_ChildViewModels_Are_Not_Null()
        {
            var (main, _, _, _, _) = MakeMainVm();
            Assert.NotNull(main.ScanVm);
            Assert.NotNull(main.SettingsVm);
            Assert.NotNull(main.ExportVm);
            Assert.NotNull(main.AboutVm);
        }
    }
}
