using InventoryKamera.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InventoryKamera
{
    internal class WeaponScraper : InventoryScraper
    {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public WeaponScraper(IScreenCapture screenCapture, IOcrEngine ocrEngine, IImageProcessor imageProcessor, IUserInterface userInterface, IGameDataService gameDataService, IInputSimulator inputSimulator, IScanProfileService scanProfile)
            : base(screenCapture, ocrEngine, imageProcessor, userInterface, gameDataService, inputSimulator, scanProfile)
        {
            inventoryPage = InventoryPage.Weapons;
        }

        public WeaponScraper()
        {
            inventoryPage = InventoryPage.Weapons;
            SortByLevel = Properties.Settings.Default.MinimumWeaponLevel > 1;
        }

        public void ScanWeapons(int count = 0)
        {
            // Determine maximum number of weapons to scan
            int weaponCount = count == 0 ? ScanItemCount() : count;
            int page = 0;
            var (rectangles, cols, rows) = GetPageOfItems(page);
            int fullPage = cols * rows;
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
                    Rectangle item = rectangles[i];
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
                    if (_screenCapture.GetAspectRatio() == new Size(8, 5))
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
                (rectangles, cols, rows) = GetPageOfItems(page, acceptLess: totalRows - rowsQueued <= fullPage);
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

        private void QueueScan(int id)
        {
			var card = GetItemCard();

            Bitmap name, level, refinement, equipped, locked;
            name = GetItemNameBitmap(card);
            locked = GetLockedBitmap(card);
            equipped = GetEquippedBitmap(card);
            level = GetLevelBitmap(card);
            refinement = GetRefinementBitmap(card);

            //Navigation.DisplayBitmap(name);
            //Navigation.DisplayBitmap(locked);
            //Navigation.DisplayBitmap(equipped);
            //Navigation.DisplayBitmap(level);
            //Navigation.DisplayBitmap(refinement);

            // Separate to all pieces of card
            List<Bitmap> weaponImages = new List<Bitmap>
            {
                name, //0
                level,
                refinement,
                locked,
                equipped,
                card //5
            };

            bool a = false;

            bool belowRarity = GetQuality(name) < Properties.Settings.Default.MinimumWeaponRarity;
            bool belowLevel = ScanLevel(level, ref a) < Properties.Settings.Default.MinimumWeaponLevel;
            StopScanning = (SortByLevel && belowLevel) || (!SortByLevel && belowRarity);

            if (StopScanning || belowRarity || belowLevel)
            {
                weaponImages.ForEach(i => i.Dispose());
                return;
            }

            // Send images to worker queue
            InventoryKamera.workerQueue.Enqueue(new OCRImageCollection(weaponImages, "weapon", id));
        }

        Bitmap GetLevelBitmap(Bitmap card)
        {
            var r = _scanProfile.ActiveProfile.Weapons.Level;
            return _imageProcessor.Crop(card,
                new Rectangle(
                    x: (int)(card.Width * r.X),
                    y: (int)(card.Height * r.Y),
                    width: (int)(card.Width * r.W),
                    height: (int)(card.Height * r.H)));
        }

        Bitmap GetRefinementBitmap(Bitmap card)
        {
            var r = _scanProfile.ActiveProfile.Weapons.Refinement;
            return _imageProcessor.Crop(card,
                new Rectangle(
                    x: (int)(card.Width * r.X),
                    y: (int)(card.Height * r.Y),
                    width: (int)(card.Width * r.W),
                    height: (int)(card.Height * r.H)));
        }

        public async Task<Weapon> CatalogueFromBitmapsAsync(List<Bitmap> bm, int id)
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

				// Check for equipped color
				Color equippedColor = Color.FromArgb(255, 255, 231, 187);
				Color equippedStatus = bm[w_equippedCharacter].GetPixel(5, 5);
				bool b_equipped = _imageProcessor.CompareColors(equippedColor, equippedStatus);

				// Check for lock color
				Color lockedColor = Color.FromArgb(255, 70, 80, 100); // Dark area around red lock
				Color lockStatus = bm[w_lock].GetPixel(5, 5);
				locked = _imageProcessor.CompareColors(lockedColor, lockStatus);

				List<Task> tasks = new List<Task>();

				var taskName = Task.Run(() =>
				{
					name = ScanWeaponName(ScanItemName(bm[w_name]));
				});
				var taskLevel = Task.Run(() => level = ScanLevel(bm[w_level], ref ascended));
				var taskRefinement = Task.Run(() => refinementLevel = ScanRefinement(bm[w_refinement]));
				var taskEquipped = Task.Run(() => equippedCharacter = ScanEquippedCharacter(bm[w_equippedCharacter]));

				tasks.Add(taskName);
				tasks.Add(taskLevel);
				tasks.Add(taskRefinement);

				if (b_equipped)
				{
					tasks.Add(taskEquipped);
				}

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

        public bool IsEnhancementMaterial(Bitmap nameBitmap)
		{
			string material = ScanEnchancementOreName(nameBitmap);
			return !string.IsNullOrWhiteSpace(material) && _gameDataService.EnhancementMaterials.Contains(material.ToLower());
		}

		public string ScanEnchancementOreName(Bitmap bm)
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

        public int ScanLevel(Bitmap bm, ref bool ascended)
		{
			Bitmap n = _imageProcessor.SetGrayscale(bm);
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

		public int ScanRefinement(Bitmap image)
		{
			// Removed scaling loop - process image once at original size
			// Scaling was distorting digit "2" specifically, causing 100% OCR failure on R2 weapons
			Bitmap n = _imageProcessor.SetGrayscale(image);
			n = _imageProcessor.SetInvert(n);

			string text = _ocrEngine.AnalyzeText(n).Trim();
			n.Dispose();

			// Debug logging to see raw OCR output
			Logger.Debug("Refinement OCR raw output: '{0}'", text);

			text = Regex.Replace(text, @"[^\d]", string.Empty);
			Logger.Debug("Refinement OCR after regex: '{0}'", text);

			// Handle multi-digit reads - Tesseract often reads R2 as "12" and R3 as "13"
			// Extract the last digit when we get invalid multi-digit results
			if (text.Length > 1 && int.TryParse(text, out int multiDigit) && multiDigit > 5)
			{
				text = text.Substring(text.Length - 1);
				Logger.Debug("Refinement OCR extracted last digit: '{0}'", text);
			}

			// Parse Int
			if (int.TryParse(text, out int refinementLevel) && 1 <= refinementLevel && refinementLevel <= 5)
			{
				Logger.Debug("Refinement OCR parsed successfully: {0}", refinementLevel);
				return refinementLevel;
			}

			Logger.Debug("Refinement OCR failed to parse valid level (1-5)");
			return -1;
		}

		public string ScanEquippedCharacter(Bitmap bm)
		{
			Bitmap n = _imageProcessor.SetGrayscale(bm);
			n = _imageProcessor.SetContrast(n, 60.0);

			string extractedString = _ocrEngine.AnalyzeText(n);
			n.Dispose();

			if (extractedString != "")
			{
				var regexItem = new Regex("Equipped:");
				if (regexItem.IsMatch(extractedString))
				{
					var name = extractedString.Split(':')[1];

					name = Regex.Replace(name, @"[\W]", string.Empty).ToLower();
					name = _gameDataService.FindClosestCharacterName(name);

					return name;
				}
			}
			// artifact has no equipped character
			return null;
		}

		#endregion Task Methods
	}
}
