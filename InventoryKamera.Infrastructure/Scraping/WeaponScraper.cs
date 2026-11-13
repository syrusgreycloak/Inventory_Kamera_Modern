using InventoryKamera.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ISRect = SixLabors.ImageSharp.Rectangle;
using ISSize = SixLabors.ImageSharp.Size;

namespace InventoryKamera
{
    internal class WeaponScraper : InventoryScraper
    {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WeaponScraper(IScreenCapture screenCapture, IOcrEngine ocrEngine, IImageProcessor imageProcessor, IUserInterface userInterface, IGameDataService gameDataService, IInputSimulator inputSimulator, IScanProfileService scanProfile, ScanSettings settings = null)
            : base(screenCapture, ocrEngine, imageProcessor, userInterface, gameDataService, inputSimulator, scanProfile, settings)
        {
            inventoryPage = InventoryPage.Weapons;
            SortByLevel = settings != null ? settings.MinimumWeaponLevel > 1 : false;
        }

        /// <summary>Enhancement ores shown after the weapons, in the order they appear (highest rarity first).</summary>
        internal static readonly string[] EnhancementOres = { "MysticEnhancementOre", "FineEnhancementOre", "EnhancementOre" };

        // Weapons counted by the last ScanWeapons, for scrolling to the end afterwards
        private int _weaponCount;

        public void ScanWeapons(int count = 0)
        {
            // Determine maximum number of weapons to scan
            int weaponCount = count == 0 ? ScanItemCount() : count;
            _weaponCount = weaponCount;
            int page = 0;
            var (rectangles, cols, rows) = GetPageOfItems(page);
            int fullPage = cols * rows;
            int rowsPerPage = rows; // first page is always a full grid
            int totalRows = (int)Math.Ceiling(weaponCount / (decimal)cols);
            int cardsQueued = 0;
            int rowsQueued = 0;
            int offset = 0;
            _userInterface.SetWeapon_Max(weaponCount);

            // Determine Delay if delay has not been found before
            // Scraper.FindDelay(rectangles);

            StopScanning = false;

            Logger.Info("Found {0} for weapon count.", weaponCount);

            SelectSortingMethod();

            // Go through weapon list
            while (cardsQueued < weaponCount)
            {
                Logger.Debug("Scanning weapon page {0}", page);
                Logger.Debug("Located {0} possible item locations on page.", rectangles.Count);

                int cardsRemaining = weaponCount - cardsQueued;
                // Go through each "page" of items and queue. In the event that not a full page of
                // items are scrolled to, offset the index of rectangle to start clicking from
                for (int i = cardsRemaining < fullPage ? (rows - (totalRows - rowsQueued)) * cols : 0; i < rectangles.Count; i++)
                {
                    ISRect item = rectangles[i];
                    _inputSimulator.SetCursor(item.Center().X, item.Center().Y + offset);
                    _inputSimulator.Click();
                    _inputSimulator.SystemWait(ScanDelay.SelectNextInventoryItem);

                    // Queue card for scanning
                    QueueScan(cardsQueued);
                    cardsQueued++;
                    if (cardsQueued >= weaponCount || this.StopScanning)
                    {
                        if (StopScanning) Logger.Info("Stopping weapon scan based on filtering");
                        else Logger.Info("Stopping weapon scan based on scans queued ({0} of {1})", cardsQueued, weaponCount);
                        return;
                    }
                }
                Logger.Debug("Finished queuing page of weapons. Scrolling...");

                rowsQueued += rows;

                // Page done, now scroll
                // If the number of remaining scans is shorter than a full page then
                // only scroll a few rows
                if (totalRows - rowsQueued <= rows)
                {
                    if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
                    {
                        offset = 35; // Lazy fix
                    }
                    for (int i = 0; i < 10 * (totalRows - rowsQueued) - 1; i++)
                    {
                        _inputSimulator.MouseVerticalScroll(-1);
                        _inputSimulator.Wait(1);
                    }
                    _inputSimulator.SystemWait(ScanDelay.Fast);
                }
                else
                {
                    // Scroll back one to keep it from getting too crazy
                    if (rowsQueued % 15 == 0)
                    {
                        _inputSimulator.MouseVerticalScroll(1);
                    }
                    for (int i = 0; i < 10 * rows - 1; i++)
                    {
                        _inputSimulator.MouseVerticalScroll(-1);
                        _inputSimulator.Wait(1);
                    }
                    _inputSimulator.SystemWait(ScanDelay.Fast);
                }
                ++page;
                (rectangles, cols, rows) = GetPageOfItems(page, acceptLess: InventoryPaging.IsLastPage(totalRows, rowsQueued, rowsPerPage));
            }

            void SelectLevelSorting()
            {
                _inputSimulator.SetCursor(
                    (int)(230 / 1280.0 * _screenCapture.GetWidth()),
                    (int)(680 / 720.0 * _screenCapture.GetHeight()));
                _inputSimulator.Click();
                _inputSimulator.Wait(1000);
                _inputSimulator.SetCursor(
                    (int)(250 / 1280.0 * _screenCapture.GetWidth()),
                    (int)(575 / 720.0 * _screenCapture.GetHeight()));
                _inputSimulator.Click();
                _inputSimulator.Wait(1000);
            }

            void SelectQualitySorting()
            {
                _inputSimulator.SetCursor(
                    (int)(230 / 1280.0 * _screenCapture.GetWidth()),
                    (int)(680 / 720.0 * _screenCapture.GetHeight()));
                _inputSimulator.Click();
                _inputSimulator.Wait(1000);
                _inputSimulator.SetCursor(
                    (int)(250 / 1280.0 * _screenCapture.GetWidth()),
                    (int)(615 / 720.0 * _screenCapture.GetHeight()));
                _inputSimulator.Click();
                _inputSimulator.Wait(1000);
            }

            void SelectSortingMethod()
            {
                if (SortByLevel)
                {
                    Logger.Debug("Sorting by level to optimize scan time.");
                    // Check if sorted by level
                    if (CurrentSortingMethod() != "level")
                    {
                        Logger.Debug("Not already sorting by level...");
                        // If not, sort by level
                        SelectLevelSorting();
                    }
                    Logger.Debug("Inventory is sorted by level.");
                }
                else
                {
                    Logger.Debug("Sorting by quality to optimize scan time.");
                    // Check if sorted by quality
                    if (CurrentSortingMethod() != "quality")
                    {
                        Logger.Debug("Not already sorting by quality...");
                        // If not, sort by quality
                        SelectQualitySorting();
                    }
                    Logger.Debug("Inventory is sorted by quality");
                }
            }
        }

        /// <summary>
        /// Reads the enhancement ores at the end of the weapon inventory. Call on the weapon page
        /// after <see cref="ScanWeapons"/>; see <see cref="InventoryScraper.ScanTrailingMaterials"/>.
        /// </summary>
        public List<Material> ScanEnhancementOres() => ScanTrailingMaterials(EnhancementOres, _weaponCount);

        private void QueueScan(int id)
        {
			var card = GetItemCard();

            Image<Rgba32> name, level, refinement, equipped, locked;
            name = GetItemNameBitmap(card);
            locked = GetLockedBitmap(card);
            equipped = GetEquippedBitmap(card);
            level = GetLevelBitmap(card);
            refinement = GetRefinementBitmap(card);

            // Separate to all pieces of card
            List<Image<Rgba32>> weaponImages = new List<Image<Rgba32>>
            {
                name, //0
                level,
                refinement,
                locked,
                equipped,
                card //5
            };

            int minRarity = _settings != null ? _settings.MinimumWeaponRarity : 3;
            int minLevel  = _settings != null ? _settings.MinimumWeaponLevel  : 1;

            bool belowRarity = GetQuality(name) < minRarity;
            // ScanLevel runs Tesseract OCR (50–300 ms per call) on the scraper thread.
            // Skip when the user hasn't set a level filter (default minimum is 1, which
            // every weapon trivially satisfies). The worker thread does the real level
            // OCR later regardless of this pre-filter.
            bool a = false;
            bool belowLevel = minLevel > 1
                ? ScanLevel(level, ref a) < minLevel
                : false;

            StopScanning = (SortByLevel && belowLevel) || (!SortByLevel && belowRarity);

            if (StopScanning || belowRarity || belowLevel)
            {
                weaponImages.ForEach(i => i.Dispose());
                return;
            }

            // Send images to worker queue
            InventoryScanner.workerQueue.Enqueue(new OCRImageCollection(weaponImages, "weapon", id));
        }

        Image<Rgba32> GetLevelBitmap(Image<Rgba32> card)
        {
            var r = _scanProfile.ActiveProfile.Weapons.Level;
            return _imageProcessor.Crop(card,
                new ISRect(
                    x: (int)(card.Width * r.X),
                    y: (int)(card.Height * r.Y),
                    width: (int)(card.Width * r.W),
                    height: (int)(card.Height * r.H)));
        }

        Image<Rgba32> GetRefinementBitmap(Image<Rgba32> card)
        {
            var r = _scanProfile.ActiveProfile.Weapons.Refinement;
            return _imageProcessor.Crop(card,
                new ISRect(
                    x: (int)(card.Width * r.X),
                    y: (int)(card.Height * r.Y),
                    width: (int)(card.Width * r.W),
                    height: (int)(card.Height * r.H)));
        }

        public async Task<Weapon> CatalogueFromBitmapsAsync(List<Image<Rgba32>> bm, int id)
		{
			// Init Variables
			string name = null;
			int level = -1;
			bool ascended = false;
			int refinementLevel = -1;
			bool locked = false;
			string equippedCharacter = null;
			int rarity = 0;
			bool refinementDefaulted = false;

			if (bm.Count >= 4)
			{
				int w_name = 0; int w_level = 1; int w_refinement = 2; int w_lock = 3; int w_equippedCharacter = 4;

				// Check for Rarity
				rarity = GetQuality(bm[w_name]);


				// Locked if the red padlock shows (see LockReader)
				locked = LockReader.IsLocked(bm[w_lock]);

				List<Task> tasks = new List<Task>();

				var taskName = Task.Run(() =>
				{
					name = ScanWeaponName(ScanItemName(bm[w_name]));
				});
				var taskLevel = Task.Run(() => level = ScanLevel(bm[w_level], ref ascended));
				var taskRefinement = Task.Run(() => refinementLevel = ScanRefinement(bm[w_refinement]));

				tasks.Add(taskName);
				tasks.Add(taskLevel);
				tasks.Add(taskRefinement);

				tasks.Add(Task.Run(() => equippedCharacter = ScanEquippedCharacter(bm[w_equippedCharacter])));

				await Task.WhenAll(tasks.ToArray());

				// Default to refinement 1 if OCR failed to prevent losing high-rarity/equipped weapons
				if (refinementLevel == -1)
				{
					refinementLevel = 1;
					refinementDefaulted = true;
					_userInterface.AddError($"Warning: Could not read refinement level for weapon ID#{id}, defaulting to R1");
				}
			}
			var weapon = new Weapon(name, level, ascended, refinementLevel, locked, equippedCharacter, id, rarity);
			weapon.RefinementDefaulted = refinementDefaulted;
			return weapon;
		}

        public bool IsEnhancementMaterial(Image<Rgba32> nameBitmap)
		{
			string material = ScanEnchancementOreName(nameBitmap);
			return !string.IsNullOrWhiteSpace(material) && _gameDataService.EnhancementMaterials.Contains(material.ToLower());
		}

		public string ScanEnchancementOreName(Image<Rgba32> bm)
		{
			// Analyze
			string name = _gameDataService.FindClosestMaterialName(ScanItemName(bm), minConfidence: 95);

			return name;
		}

        #region Task Methods

		private string ScanWeaponName(string name)
        {
            return _gameDataService.FindClosestWeapon(name);
        }

        public int ScanLevel(Image<Rgba32> bm, ref bool ascended)
		{
			Image<Rgba32> n = _imageProcessor.SetGrayscale(bm);
			n = _imageProcessor.SetInvert(n);

			string text = _ocrEngine.AnalyzeText(n).Trim();
			n.Dispose();
			text = Regex.Replace(text, @"(?![\d/]).", string.Empty);

			if (text.Contains('/'))
			{
				string[] temp = text.Split(new[] { '/' }, 2);

				if (temp.Length == 2)
				{
					if (int.TryParse(temp[0], out int level) && int.TryParse(temp[1], out int maxLevel))
					{
						maxLevel = (int)Math.Round(maxLevel / 10.0, MidpointRounding.AwayFromZero) * 10;
						ascended = 20 <= level && level < maxLevel;
						return level;
					}
				}
			}
			return -1;
		}

		public int ScanRefinement(Image<Rgba32> image) =>
			RefinementReader.Read(image, _imageProcessor, _ocrEngine);

		public string ScanEquippedCharacter(Image<Rgba32> bm) =>
			EquippedReader.Read(bm, _imageProcessor, _ocrEngine, n => _gameDataService.FindClosestCharacterName(n));

		#endregion Task Methods
	}
}
