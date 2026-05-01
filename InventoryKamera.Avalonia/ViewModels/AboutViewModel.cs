using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryKamera.Avalonia.Services;
using InventoryKamera.Configuration;
using NLog;
using Octokit;

namespace InventoryKamera.Avalonia.ViewModels
{
    /// <summary>
    /// ViewModel for the About tab.
    /// Shows version, game-data status, and GitHub links.
    /// Provides commands to update game data and check for app updates.
    /// </summary>
    public partial class AboutViewModel : ViewModelBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string GitHubOwner = "syrusgreycloak";
        private const string GitHubRepo  = "Inventory_Kamera_Modern";

        private readonly ISettingsService _settings;
        private readonly IUrlLauncher     _url;
        private readonly IDialogService   _dialog;
        private readonly DatabaseManager  _databaseManager;

        // ─────────────────────────────────────────────────────────────────────
        // Observable state
        // ─────────────────────────────────────────────────────────────────────

        [ObservableProperty]
        private bool _isCheckingUpdates;

        [ObservableProperty]
        private bool _isUpdatingGameData;

        // ─────────────────────────────────────────────────────────────────────
        // Static / computed properties
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Assembly version string in Major.Minor.Build form.</summary>
        public string Version
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v != null ? v.ToString(3) : "0.0.0";
            }
        }

        /// <summary>Local game-data version (Major.Minor).</summary>
        public string LocalGameVersion =>
            _databaseManager.LocalVersion.ToString(2);

        /// <summary>Date/time of the most recent update check (from settings).</summary>
        public DateTime LastUpdateCheck => _settings.Current.LastUpdateCheck;

        /// <summary>URLs exposed for binding so the View doesn't hardcode link targets.</summary>
        public string RepoUrl     => $"https://github.com/{GitHubOwner}/{GitHubRepo}/";
        public string ReleasesUrl => $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";
        public string IssuesUrl   => $"https://github.com/{GitHubOwner}/{GitHubRepo}/issues";

        // ─────────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────────

        public AboutViewModel(
            ISettingsService settings,
            IUrlLauncher     url,
            IDialogService   dialog)
        {
            _settings        = settings ?? throw new ArgumentNullException(nameof(settings));
            _url             = url      ?? throw new ArgumentNullException(nameof(url));
            _dialog          = dialog   ?? throw new ArgumentNullException(nameof(dialog));
            _databaseManager = new DatabaseManager();
        }

        // Test constructor — allows injection of a DatabaseManager substitute.
        public AboutViewModel(
            ISettingsService settings,
            IUrlLauncher     url,
            IDialogService   dialog,
            DatabaseManager  databaseManager)
        {
            _settings        = settings        ?? throw new ArgumentNullException(nameof(settings));
            _url             = url             ?? throw new ArgumentNullException(nameof(url));
            _dialog          = dialog          ?? throw new ArgumentNullException(nameof(dialog));
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        }

        // ─────────────────────────────────────────────────────────────────────
        // Link commands
        // ─────────────────────────────────────────────────────────────────────

        [RelayCommand]
        private void OpenGitHub() =>
            _url.Open($"https://github.com/{GitHubOwner}/{GitHubRepo}/");

        [RelayCommand]
        private void OpenReleases() =>
            _url.Open($"https://github.com/{GitHubOwner}/{GitHubRepo}/releases");

        [RelayCommand]
        private void OpenIssues() =>
            _url.Open($"https://github.com/{GitHubOwner}/{GitHubRepo}/issues");

        // ─────────────────────────────────────────────────────────────────────
        // Check for app updates
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks GitHub releases for a newer version and prompts the user to download it.
        /// Mirrors the logic in WinForms <c>MainForm.CheckForKameraUpdates</c>.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCheckAppUpdates))]
        private async Task CheckAppUpdates()
        {
            IsCheckingUpdates = true;

            // Record check time
            _settings.Current.LastUpdateCheck = DateTime.Now;
            _settings.Save();
            OnPropertyChanged(nameof(LastUpdateCheck));

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("Inventory_Kamera"));
                var releases = await client.Repository.Release.GetAll(GitHubOwner, GitHubRepo);
                if (releases == null || releases.Count == 0)
                {
                    await _dialog.ShowInfoAsync("Up to date", "No releases found on GitHub.");
                    return;
                }

                var latest = releases[0];
                var tagName = latest.TagName ?? string.Empty;
                // Strip any alpha prefix such as 'v', 'V', letters only
                var cleaned = Regex.Replace(tagName, "[a-zA-Z]", string.Empty);
                if (!System.Version.TryParse(cleaned, out var latestVersion))
                {
                    Log.Warn("Could not parse release tag: {Tag}", tagName);
                    await _dialog.ShowInfoAsync("Check complete", "Could not determine the latest version.");
                    return;
                }

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new System.Version();

                if (currentVersion.CompareTo(latestVersion) < 0)
                {
                    var message = $"A new version of Inventory Kamera is available.\n\n" +
                                  $"Current version: {currentVersion}\n" +
                                  $"Latest version:  {latestVersion}\n\n" +
                                  $"Would you like to download the update?";

                    if (await _dialog.ConfirmAsync("Update available", message))
                        _url.Open(latest.HtmlUrl);
                }
                else
                {
                    await _dialog.ShowInfoAsync("Up to date",
                        $"You are running the latest version ({currentVersion}).");
                }
            }
            catch (RateLimitExceededException)
            {
                Log.Warn("GitHub rate limit exceeded while checking for Kamera updates. Try again in an hour.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "App update check failed");
            }
            finally
            {
                IsCheckingUpdates = false;
            }
        }

        private bool CanCheckAppUpdates() => !IsCheckingUpdates && !IsUpdatingGameData;

        // ─────────────────────────────────────────────────────────────────────
        // Update game data
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs <see cref="DatabaseManager.UpdateGameData"/> and shows the result in a dialog.
        /// Mirrors the logic in WinForms <c>MainForm.DatabaseUpdateMenuItem_Click</c>.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanUpdateGameData))]
        private async Task UpdateGameData()
        {
            IsUpdatingGameData = true;

            try
            {
                var status = await Task.Run(() => _databaseManager.UpdateGameData());

                // Stamp the last-check timestamp for any non-failed outcome — a "skipped"
                // result still represents a successful round-trip to the remote source,
                // so the user has confirmed currency at this moment.
                if (status != UpdateStatus.Fail)
                {
                    _settings.Current.LastUpdateCheck = DateTime.Now;
                    _settings.Save();
                    OnPropertyChanged(nameof(LastUpdateCheck));
                }

                await HandleUpdateStatus(status, forced: false);

                // Refresh displayed version regardless of status (force-update path may have
                // bumped the version even when initial status was Skipped).
                OnPropertyChanged(nameof(LocalGameVersion));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Game data update threw an exception");
                await _dialog.ShowErrorAsync("Update failed", ex.Message);
            }
            finally
            {
                IsUpdatingGameData = false;
            }
        }

        private bool CanUpdateGameData() => !IsUpdatingGameData && !IsCheckingUpdates;

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private async Task HandleUpdateStatus(UpdateStatus status, bool forced)
        {
            switch (status)
            {
                case UpdateStatus.Success:
                    var ver = _databaseManager.LocalVersion.ToString(2);
                    await _dialog.ShowInfoAsync(
                        "Update successful",
                        $"Game data updated to version {ver}.");
                    Log.Info("Updated game data to {Version}", ver);
                    break;

                case UpdateStatus.Skipped:
                    if (!forced)
                    {
                        var alreadyLatest = _databaseManager.LocalVersion.ToString(2);
                        var doForce = await _dialog.ConfirmAsync(
                            "Already up to date",
                            $"No update necessary — you are already using the latest game data ({alreadyLatest}).\n\n" +
                            "Would you like to force an update anyway?");

                        if (doForce)
                        {
                            var forcedStatus = await Task.Run(() => _databaseManager.UpdateGameData(force: true));
                            await HandleUpdateStatus(forcedStatus, forced: true);
                        }
                    }
                    else
                    {
                        await _dialog.ShowInfoAsync(
                            "Update successful",
                            $"Game data updated to version {_databaseManager.LocalVersion.ToString(2)}.");
                    }
                    break;

                case UpdateStatus.Fail:
                default:
                    await _dialog.ShowErrorAsync(
                        "Update failed",
                        "Unable to update game data. Please check the log for more details.");
                    break;
            }
        }
    }
}
