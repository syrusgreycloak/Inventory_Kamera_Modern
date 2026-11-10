using InventoryKamera.Configuration;
using InventoryKamera.Infrastructure.Windows; // Navigation
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace InventoryKamera
{
    public class InventoryScanner
	{

		private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		[JsonProperty]
		public List<Character> Characters;

		[JsonProperty]
		public Inventory Inventory;

		private List<Artifact> equippedArtifacts;
		private List<Weapon> equippedWeapons;
		public static ConcurrentQueue<OCRImageCollection> workerQueue;
		private List<Thread> ImageProcessors;

		private WeaponScraper weaponScraper;
		private ArtifactScraper artifactScraper;
		private MaterialScraper materialScraper;
		private CharacterScraper _characterScraper;

		private readonly IScreenCapture _screenCapture;
		private readonly IOcrEngine _ocrEngine;
		private readonly IImageProcessor _imageProcessor;
		private readonly IUserInterface _userInterface;
		private readonly IGameDataService _gameDataService;
		private readonly IInputSimulator _inputSimulator;
		private readonly IScanProfileService _scanProfile;

		// Settings snapshot used by the injected-services constructor; null when using WinForms path.
		private readonly ScanSettings _settings;

		private volatile bool b_threadCancel;
		// CancellationToken support — set by the GatherData(CancellationToken) overload.
		private CancellationToken _cancellationToken;
		public bool WasCancelled => b_threadCancel;
		private readonly int NumWorkers;

		public bool HasData
        {
			get { return Characters.Count > 0 || Inventory.Size > 0; }
        }

		/// <summary>
		/// Injected-services constructor for the Avalonia app.
		/// All services are provided by the DI container; no static service construction here.
		/// Settings are snapshotted from <paramref name="settings"/> at construction time.
		/// </summary>
		public InventoryScanner(
			IScreenCapture screenCapture,
			IInputSimulator inputSimulator,
			IOcrEngine ocrEngine,
			IImageProcessor imageProcessor,
			IUserInterface userInterface,
			IGameDataService gameDataService,
			IScanProfileService scanProfile,
			ScanSettings settings)
		{
			_screenCapture   = screenCapture   ?? throw new ArgumentNullException(nameof(screenCapture));
			_inputSimulator  = inputSimulator  ?? throw new ArgumentNullException(nameof(inputSimulator));
			_ocrEngine       = ocrEngine       ?? throw new ArgumentNullException(nameof(ocrEngine));
			_imageProcessor  = imageProcessor  ?? throw new ArgumentNullException(nameof(imageProcessor));
			_userInterface   = userInterface   ?? throw new ArgumentNullException(nameof(userInterface));
			_gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
			_scanProfile     = scanProfile     ?? throw new ArgumentNullException(nameof(scanProfile));
			_settings        = settings        ?? throw new ArgumentNullException(nameof(settings));

			Characters        = new List<Character>();
			Inventory         = new Inventory();
			equippedArtifacts = new List<Artifact>();
			equippedWeapons   = new List<Weapon>();
			ImageProcessors   = new List<Thread>();
			workerQueue       = new ConcurrentQueue<OCRImageCollection>();

			weaponScraper      = new WeaponScraper(_screenCapture, _ocrEngine, _imageProcessor, _userInterface, _gameDataService, _inputSimulator, _scanProfile, _settings);
			artifactScraper    = new ArtifactScraper(_screenCapture, _ocrEngine, _imageProcessor, _userInterface, _gameDataService, _inputSimulator, _scanProfile, _settings);
			materialScraper    = new MaterialScraper(_screenCapture, _ocrEngine, _imageProcessor, _userInterface, _gameDataService, _inputSimulator, _scanProfile, _settings);
			_characterScraper  = new CharacterScraper(_screenCapture, _ocrEngine, _imageProcessor, _userInterface, _gameDataService, _inputSimulator, _scanProfile, _settings);

			b_threadCancel     = false;
			_cancellationToken = CancellationToken.None;

			NumWorkers = settings.ScannerDelay == 0 ? 3 : 2;

			Logger.Info("Kamera initialized (injected)");
		}

		public void ResetLogging()
		{
			try
			{
				Directory.Delete("./logging/weapons", true);
				Directory.Delete("./logging/artifacts", true);
				Directory.Delete("./logging/characters", true);
				Directory.Delete("./logging/materials", true);
			}
			catch { }

			Directory.CreateDirectory("./logging/weapons");
			Directory.CreateDirectory("./logging/artifacts");
			Directory.CreateDirectory("./logging/characters");
			Directory.CreateDirectory("./logging/materials");

			Logger.Info("Logging directory reset");
		}

		public void StopImageProcessorWorkers()
		{
			b_threadCancel = true;
			AwaitProcessors();
			workerQueue = new ConcurrentQueue<OCRImageCollection>();
		}

		// ------------------------------------------------------------------
		// Settings helpers — resolved from injected snapshot (_settings always non-null here)
		// ------------------------------------------------------------------

		private bool S_ScanWeapons      => _settings.ScanWeapons;
		private bool S_ScanArtifacts    => _settings.ScanArtifacts;
		private bool S_ScanCharacters   => _settings.ScanCharacters;
		private bool S_ScanMaterials    => _settings.ScanMaterials;
		private bool S_ScanCharDevItems => _settings.ScanCharDevItems;
		private bool S_LogScreenshots   => _settings.LogScreenshots;
		private string S_TravelerName   => _settings.TravelerName;
		private string S_WandererName   => _settings.WandererName;
		private string S_Manequin1Name  => _settings.Manequin1Name;
		private string S_Manequin2Name  => _settings.Manequin2Name;

		/// <summary>
		/// Starts the full inventory scan using CancellationToken.None.
		/// Delegates to <see cref="GatherData(CancellationToken)"/>.
		/// </summary>
		public void GatherData()
		{
			GatherData(CancellationToken.None);
		}

		/// <summary>
		/// Starts the full inventory scan.  The <paramref name="token"/> is checked at each
		/// category boundary and inside the image-processor worker loop alongside the legacy
		/// <c>b_threadCancel</c> volatile flag (which remains as a safety net).
		/// </summary>
		public void GatherData(CancellationToken token)
		{
			_cancellationToken = token;

			ResetLogging();

			_gameDataService.ReloadData();

			// Initialize Image Processors
			for (int i = 0; i < NumWorkers; i++)
			{
				Thread processor = new Thread(ImageProcessorWorker){ IsBackground = true };
				processor.Start();
				ImageProcessors.Add(processor);
			}
			Logger.Debug("Added {ImageProcessors.Count} workers", ImageProcessors.Count);

			_ocrEngine.RestartEngines();


			// Assign Traveler's name via instance CharacterScraper
			string travelerName = string.IsNullOrWhiteSpace(S_TravelerName)
				? _characterScraper.ScanMainCharacterName()
				: S_TravelerName.ToLower();
			if (!string.IsNullOrWhiteSpace(travelerName))
			{
				_gameDataService.UpdateCharacterName("traveler", travelerName);
				_userInterface.SetMainCharacterName(travelerName);
			}
			else
			{
				_userInterface.AddError("Could not parse Traveler's username");
			}

			// Assign Wanderer's custom name
			_gameDataService.UpdateCharacterName("wanderer", S_WandererName);

			// Assign Mannequin names
			_gameDataService.UpdateCharacterName("manequin1", S_Manequin1Name);
			_gameDataService.UpdateCharacterName("manequin2", S_Manequin2Name);

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			if (S_ScanWeapons)
			{
				Logger.Info("Scanning weapons...");
				// Get Weapons
				Navigation.InventoryScreen();
				Navigation.SelectWeaponInventory();
				try
				{
                    weaponScraper.ScanWeapons();
				}
				catch (FormatException ex) { _userInterface.AddError(ex.Message); }
				catch (Exception ex)
				{
					_userInterface.AddError(ex.Message + "\n" + ex.StackTrace);
				}
				Navigation.MainMenuScreen();
				Logger.Info("Done scanning weapons");
			}

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			if (S_ScanArtifacts)
			{
				Logger.Info("Scanning artifacts...");

				// Get Artifacts
				Navigation.InventoryScreen();
				Navigation.SelectArtifactInventory();
				try
				{
					artifactScraper.ScanArtifacts();
				}
				catch (FormatException ex) { _userInterface.AddError(ex.Message); }
				catch (Exception ex)
				{
					_userInterface.AddError(ex.Message + "\n" + ex.StackTrace);
				}
				Navigation.MainMenuScreen();
				Logger.Info("Done scanning artifacts");
			}

			workerQueue.Enqueue(new OCRImageCollection(null, "END", 0));

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			if (S_ScanCharacters)
			{
				Logger.Info("Scanning characters...");
				// Get characters
				Navigation.CharacterScreen();
				try
				{
					_characterScraper.ScanCharacters(ref Characters);
				}
				catch (Exception ex)
				{
					_userInterface.AddError(ex.Message + "\n" + ex.StackTrace);
				}
				Navigation.MainMenuScreen();
				Logger.Info("Done scanning characters");
			}

			// Wait for Image Processors to finish
			AwaitProcessors();

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			if (S_ScanCharacters)
			{
				// Assign Artifacts to Characters
				if (S_ScanArtifacts)
					AssignArtifacts();
				if (S_ScanWeapons)
					AssignWeapons();
			}

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			// Scan Character Development Items
			if (S_ScanCharDevItems)
			{
				Logger.Info("Scanning character development materials...");
				// Get Materials
				Navigation.InventoryScreen();
				Navigation.SelectCharacterDevelopmentInventory();
				try
				{
					materialScraper.SetInventoryPage(InventoryPage.CharacterDevelopmentItems);
					materialScraper.Scan_Materials(ref Inventory);
				}
				catch (FormatException ex) { _userInterface.AddError(ex.Message); }
				catch (Exception ex)
				{
					_userInterface.AddError(ex.Message + "\n" + ex.StackTrace);
				}
				Navigation.MainMenuScreen();
				Logger.Info("Done scanning character development materials");
			}

			if (token.IsCancellationRequested || b_threadCancel) goto Cleanup;

			// Scan Materials
			if (S_ScanMaterials)
			{
				Logger.Info("Scanning materials...");
				// Get Materials
				Navigation.InventoryScreen();
				Navigation.SelectMaterialInventory();
				try
				{
					materialScraper.SetInventoryPage(InventoryPage.Materials);
					materialScraper.Scan_Materials(ref Inventory);
				}
				catch (FormatException ex) { _userInterface.AddError(ex.Message); }
				catch (Exception ex)
				{
					_userInterface.AddError(ex.Message + "\n" + ex.StackTrace);
				}
				Navigation.MainMenuScreen();
				Logger.Info("Done scanning materials");
			}

			Cleanup:
			if (token.IsCancellationRequested)
			{
				Logger.Info("Scan cancelled via CancellationToken");
				// Signal workers to stop if they haven't already
				b_threadCancel = true;
			}
		}

		private void AwaitProcessors()
		{
			while (ImageProcessors.Count > 0)
			{
				ImageProcessors.RemoveAll(process => !process.IsAlive);
			}
			b_threadCancel = false;
		}

		public void ImageProcessorWorker()
		{
			Logger.Debug("Thread #{0} priority: {1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.Priority);
			while (true)
			{
				if (_cancellationToken.IsCancellationRequested || b_threadCancel)
				{
					while (workerQueue.TryDequeue(out _)) { /* drain */ }
					break;
				}

				if (workerQueue.TryDequeue(out OCRImageCollection imageCollection))
				{
					try
					{
					switch (imageCollection.Type)
					{
						case "weapon":
							if (weaponScraper.IsEnhancementMaterial(imageCollection.Bitmaps.First()))
							{
								Logger.Debug("Enhancement Material found for weapon #{weaponID}", imageCollection.Id);
								weaponScraper.StopScanning = true;
								break;
							}

							_userInterface.SetGearPictureBox(imageCollection.Bitmaps.Last());

							// Scan as weapon with 30 second timeout
							var weaponTask = weaponScraper.CatalogueFromBitmapsAsync(imageCollection.Bitmaps, imageCollection.Id);
							if (!weaponTask.Wait(TimeSpan.FromSeconds(30)))
							{
								Logger.Error("Weapon #{0} OCR timed out after 30 seconds - skipping", imageCollection.Id);
								_userInterface.AddError($"Weapon #{imageCollection.Id} scan timed out - possibly problematic weapon data");
								imageCollection.Bitmaps.ForEach(b => b.Dispose());
								break;
							}
							Weapon weapon = weaponTask.Result;
							_userInterface.SetGear(imageCollection.Bitmaps.Last(), weapon);

							string weaponPath = $"./logging/weapons/weapon{weapon.Id}/";

							if (S_LogScreenshots) Directory.CreateDirectory(weaponPath);

							if (weapon.IsValid())
							{
								_userInterface.IncrementWeaponCount();
								Inventory.Add(weapon);
								if (!string.IsNullOrWhiteSpace(weapon.EquippedCharacter))
									equippedWeapons.Add(weapon);
							}
							else
							{
								_userInterface.AddError($"Unable to validate information for weapon ID#{weapon.Id}");
								string error = "";
								if (!weapon.HasValidWeaponName()) error += "Invalid weapon name\n";
								if (!weapon.HasValidRarity()) error += "Invalid weapon rarity\n";
								if (!weapon.HasValidLevel()) error += "Invalid weapon level\n";
								if (!weapon.HasValidRefinementLevel()) error += "Invalid refinement level\n";
								if (!weapon.HasValidEquippedCharacter()) error += "Inavlid equipped character\n";
								_userInterface.AddError(error + weapon.ToString());
								Directory.CreateDirectory(weaponPath);
								using (var writer = File.CreateText(weaponPath + "log.txt"))
								{
									writer.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
									writer.WriteLine($"Resolution: {Navigation.GetWidth()}x{Navigation.GetHeight()}");
									writer.WriteLine($"Error log:\n\t{error.Replace("\n", "\n\t")}");
								}
							}

                            if (!weapon.IsValid() || weapon.RefinementDefaulted || S_LogScreenshots)
                            {
                                Directory.CreateDirectory(weaponPath + "name");
                                imageCollection.Bitmaps[0].SaveAsPng(weaponPath + "name/name.png");
                                Directory.CreateDirectory(weaponPath + "rarity");
                                imageCollection.Bitmaps[0].SaveAsPng(weaponPath + "rarity/rarity.png");
                                Directory.CreateDirectory(weaponPath + "level");
                                imageCollection.Bitmaps[1].SaveAsPng(weaponPath + "level/level.png");
                                Directory.CreateDirectory(weaponPath + "refinement");
                                imageCollection.Bitmaps[2].SaveAsPng(weaponPath + "refinement/refinement.png");
                                Directory.CreateDirectory(weaponPath + "equipped");
                                imageCollection.Bitmaps[4].SaveAsPng(weaponPath + "equipped/equipped.png");

                                imageCollection.Bitmaps.Last().SaveAsPng(weaponPath + "card.png");
								Task.Run(() => LogObject(weapon, weaponPath + "weapon.json"));
                            }

                            // Dispose of everything
                            imageCollection.Bitmaps.ForEach(b => b.Dispose());
							break;

						case "artifact":
							if (artifactScraper.IsEnhancementMaterial(imageCollection.Bitmaps.Last()))
							{
								Logger.Debug("Enhancement Material found for artifact #{artifactID}", imageCollection.Id);
								artifactScraper.StopScanning = true;
								break;
							}

							_userInterface.SetGearPictureBox(imageCollection.Bitmaps.Last());

							// Scan as artifact with 30 second timeout
							Logger.Debug("Starting OCR for artifact #{0}", imageCollection.Id);
							var artifactTask = artifactScraper.CatalogueFromBitmapsAsync(imageCollection.Bitmaps, imageCollection.Id);
							if (!artifactTask.Wait(TimeSpan.FromSeconds(30)))
							{
								Logger.Error("Artifact #{0} OCR timed out after 30 seconds - skipping", imageCollection.Id);
								_userInterface.AddError($"Artifact #{imageCollection.Id} scan timed out - possibly problematic artifact data");
								imageCollection.Bitmaps.ForEach(b => b.Dispose());
								break;
							}
							Logger.Debug("OCR completed for artifact #{0}", imageCollection.Id);
							Artifact artifact = artifactTask.Result;
							_userInterface.SetGear(imageCollection.Bitmaps.Last(), artifact);

							string artifactPath = $"./logging/artifacts/artifact{artifact.Id}/";

                            if (S_LogScreenshots) Directory.CreateDirectory(artifactPath);

							if (artifact.IsValid())
							{
								_userInterface.IncrementArtifactCount();
								Inventory.Add(artifact);
								if (!string.IsNullOrWhiteSpace(artifact.EquippedCharacter))
									equippedArtifacts.Add(artifact);
							}
							else
							{
								_userInterface.AddError($"Unable to validate information for artifact ID#{artifact.Id}");
								string error = "";
								if (!artifact.HasValidSlot()) error += "Invalid artifact gear slot\n";
								if (!artifact.HasValidSetName()) error += "Invalid artifact set name\n";
								if (!artifact.HasValidRarity()) error += "Invalid artifact rarity\n";
								if (!artifact.HasValidLevel()) error += "Invalid artifact level\n";
								if (!artifact.HasValidMainStat()) error += "Invalid artifact main stat\n";
								if (!artifact.HasValidSubStats()) error += "Invalid artifact sub stats\n";
								if (!artifact.HasValidEquippedCharacter()) error += "Invalid equipped character\n";
								_userInterface.AddError(error + artifact.ToString());
								Directory.CreateDirectory(artifactPath);
								using (var writer = File.CreateText(artifactPath + "log.txt"))
								{
									writer.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
									writer.WriteLine($"Resolution: {Navigation.GetWidth()}x{Navigation.GetHeight()}");
									writer.WriteLine($"Error Log:\n\t{error.Replace("\n", "\n\t")}");
								}
							}

                            if (!artifact.IsValid() || S_LogScreenshots)
                            {
                                Directory.CreateDirectory(artifactPath + "name");
                                imageCollection.Bitmaps[0].SaveAsPng(artifactPath + "name/name.png");
                                Directory.CreateDirectory(artifactPath + "slot");
								imageCollection.Bitmaps[1].SaveAsPng(artifactPath + "slot/slot.png");
                                Directory.CreateDirectory(artifactPath + "mainstat");
                                imageCollection.Bitmaps[2].SaveAsPng(artifactPath + "mainstat/mainstat.png");
								Directory.CreateDirectory(artifactPath + "level");
								imageCollection.Bitmaps[3].SaveAsPng(artifactPath + "level/level.png");
								Directory.CreateDirectory(artifactPath + "substats");
								imageCollection.Bitmaps[4].SaveAsPng(artifactPath + "substats/substats.png");
								Directory.CreateDirectory(artifactPath + "equipped");
								imageCollection.Bitmaps[5].SaveAsPng(artifactPath + "equipped/equipped.png");
								Directory.CreateDirectory(artifactPath + "locked");
								imageCollection.Bitmaps[6].SaveAsPng(artifactPath + "locked/locked.png");

								imageCollection.Bitmaps.Last().SaveAsPng(artifactPath + "card.png");

								Task.Run(()=>LogObject(artifact, artifactPath + "artifact.json"));
							}

							// Dispose of everything
							imageCollection.Bitmaps.ForEach(b => b.Dispose());
							break;

						case "END":
							b_threadCancel = true;
							break;

						default:
							Logger.Error("Unknown image type in ImageProcessorWorker: {type}", imageCollection.Type);
							break;
					}
					}
					catch (Exception ex)
					{
						// An exception inside the per-item switch must NOT escape the worker
						// thread — an uncaught background-thread exception terminates the entire
						// process by runtime policy. Log, dispose any held bitmaps, and keep
						// the worker alive for the next item.
						Logger.Error(ex, "ImageProcessorWorker failed processing {Type} #{Id}",
							imageCollection?.Type ?? "<null>", imageCollection?.Id ?? -1);
						try
						{
							_userInterface?.AddError(
								$"Worker error on {imageCollection?.Type} #{imageCollection?.Id}: {ex.Message}");
						}
						catch { /* never let the error reporter throw */ }
						try
						{
							imageCollection?.Bitmaps?.ForEach(b => b?.Dispose());
						}
						catch { /* swallow secondary disposal errors */ }
					}
				}
				else
				{
					// Wait for more images to process
					Thread.Sleep(250);
				}
			}
			Logger.Debug("Thread {threadId} exit", Thread.CurrentThread.ManagedThreadId);
		}

        private static void LogObject(object obj, string path)
        {
            using (var file = new StreamWriter(path))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(file, obj);
            }
        }

        // Equipped items record the Traveler as "Traveler" (the GOOD location key), while the scanned
        // character's NameGOOD includes the element (e.g. "TravelerCryo").
        private static bool IsEquippedBy(string location, string nameGOOD) =>
            location == nameGOOD ||
            (location == "Traveler" && nameGOOD != null && nameGOOD.StartsWith("Traveler"));

        private static string LocationFor(string nameGOOD) =>
            nameGOOD != null && nameGOOD.StartsWith("Traveler") ? "Traveler" : nameGOOD;

        public void AssignArtifacts()
		{
			foreach (Artifact artifact in equippedArtifacts)
			{
				foreach (Character character in Characters)
				{
					if (IsEquippedBy(artifact.EquippedCharacter, character.NameGOOD))
					{
						character.AssignArtifact(artifact); // Do we even need to do this?
						Logger.Debug("Assigned {fearSlot} to {character}", artifact.GearSlot, character.NameGOOD);
						break;
					}
				}
			}
		}

		public void AssignWeapons()
		{
			foreach (Character character in Characters)
			{
				foreach (Weapon weapon in equippedWeapons)
				{
					if (IsEquippedBy(weapon.EquippedCharacter, character.NameGOOD))
					{
						character.AssignWeapon(weapon);
						Logger.Debug("Assigned {weapon} to {character}", weapon.Name, character.NameGOOD);
						break;
					}
				}
				if (character.Weapon is null)
				{
					Inventory.Add(new Weapon(character.WeaponType, LocationFor(character.NameGOOD)));
					Logger.Info("Default weapon assigned to {character}", character.NameGOOD);
				}
			}
		}
	}
}