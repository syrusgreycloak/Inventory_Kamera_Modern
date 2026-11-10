using InventoryKamera.Configuration;
using InventoryKamera.Infrastructure.Windows; // Navigation
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using DrawingSize = System.Drawing.Size;
using ISSize     = SixLabors.ImageSharp.Size;

namespace InventoryKamera
{
    internal class CharacterScraper : InventoryScraper
	{
		private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public CharacterScraper(IScreenCapture screenCapture, IOcrEngine ocrEngine, IImageProcessor imageProcessor, IUserInterface userInterface, IGameDataService gameDataService, IInputSimulator inputSimulator, IScanProfileService scanProfile, ScanSettings settings = null)
			: base(screenCapture, ocrEngine, imageProcessor, userInterface, gameDataService, inputSimulator, scanProfile, settings)
		{
		}

		public void ScanCharacters(ref List<Character> Characters)
		{
			int viewed = 0;
			string first = null;
			HashSet<string> scanned = new HashSet<string>();

			_userInterface.ResetCharacterDisplay();

			while (true)
			{
				var character = ScanCharacter(first);

				// Name couldn't be read after all retries: skip this character rather than crash the
				// whole character scan (crops are in logging/characters/failures)
				if (string.IsNullOrWhiteSpace(character.NameGOOD))
				{
					Logger.Error("Skipping a character whose name could not be read; see logging/characters/failures");
					_userInterface.AddError("Skipped a character whose name could not be read");
					_inputSimulator.SelectNextCharacter();
					_userInterface.ResetCharacterDisplay();
					if (++viewed > 3 && Characters.Count < 1) break;
					continue;
				}

				// Skip mannequins - Genshin Optimizer doesn't support them and they cause crashes
				if (character.NameGOOD.ToLower() == "manequin1" || character.NameGOOD.ToLower() == "manequin2")
				{
					Logger.Info("Skipping mannequin: {0}", character.NameGOOD);
					_inputSimulator.SelectNextCharacter();
					_userInterface.ResetCharacterDisplay();
					continue;
				}

				if (Characters.Count > 0 && character.NameGOOD == Characters.ElementAt(0).NameGOOD) break;
				if (character.IsValid())
				{
                    if (!scanned.Contains(character.NameGOOD))
					{
						Characters.Add(character);
						scanned.Add(character.NameGOOD);
						_userInterface.IncrementCharacterCount();
						Logger.Info("Scanned {0} successfully", character.NameGOOD);
						if (Characters.Count == 1) first = character.NameGOOD;
					}
					else
                    {
						Logger.Info("Prevented {0} duplicate scan", character.NameGOOD);
                    }
				}
                else
				{
					string error = "";
					if (!character.HasValidName()) error += "Invalid character name\n";
					if (!character.HasValidLevel()) error += "Invalid level\n";
					if (!character.HasValidElement()) error += "Invalid element\n";
					if (!character.HasValidConstellation()) error += "Invalid constellation\n";
					if (!character.HasValidTalents()) error += "Invalid talents\n";
					Logger.Error("Failed to scan character\n" + error + character);
				}

				_inputSimulator.SelectNextCharacter();
				_userInterface.ResetCharacterDisplay();

				if (++viewed > 3 && Characters.Count < 1) break;
			}

			// Childe passive buff fix
			for (int i = 0; i < Characters.Count; i++)
			{
				if (Characters[i].NameGOOD.ToLower() == "tartaglia" && Characters[i].Ascension >= 4)
				{
					Logger.Info("Ascension 4+ Tartaglia found at position {0}.", i);
					if (i < 4)
					{
						for (int j = 0; j < 4; j++)
						{
							Characters[j].Talents["auto"] -= 1;
							Logger.Info("Applied Tartaglia auto attack fix to {0} at position {1}.", Characters[j].NameGOOD, j);
						}
						break;
					}
					else
					{
						Characters[i].Talents["auto"] -= 1;
						Logger.Info("Applied Tartaglia auto attack fix to self only.");
						break;
					}
				}
				else if (Characters[i].NameGOOD.ToLower() == "tartaglia") break;
            }
		}

		private Character ScanCharacter(string firstCharacter)
		{
			var character = new Character();
			_inputSimulator.SelectCharacterAttributes();
			string name = null;
			string element = null;

			// Scan the Name and element of Character. Attempt 75 times max.
			ScanNameAndElement(ref name, ref element);

			// Early return for mannequins - just set the name and skip all other scanning
			if (name == "Manequin1" || name == "Manequin2")
			{
				character.NameGOOD = name;
				return character;
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				if (string.IsNullOrWhiteSpace(name)) _userInterface.AddError("Could not determine character's name");
				if (string.IsNullOrWhiteSpace(element)) _userInterface.AddError("Could not determine character's element");
				return character;
			}

			character.NameGOOD = name;
			character.Element = element;

			// Check if character was first scanned
			if (character.NameGOOD != firstCharacter)
			{
				bool ascended = false;
				// Scan Level and ascension
				int level = ScanLevel(ref ascended);
				if (level == -1)
				{
					_userInterface.AddError($"Could not determine {character.NameGOOD}'s level. Setting to 1.");
					level = 1;
					ascended = false;
				}
				character.Level = level;
				character.Ascended = ascended;

				Logger.Info("{0} Level: {1}", character.NameGOOD, character.Level);
				Logger.Info("{0} Ascended: {1}", character.NameGOOD, character.Ascended);

				// Scan Constellation
				_inputSimulator.SelectCharacterConstellation();
				character.Constellation = ScanConstellations(character);
				Logger.Info("{0} Constellation: {1}", character.NameGOOD, character.Constellation);
				_inputSimulator.SystemWait(ScanDelay.Normal);

				// Scan Talents
				_inputSimulator.SelectCharacterTalents();
				character.Talents = ScanTalents(character);
				Logger.Info("{0} Talents: {1}", character.NameGOOD, "{" + string.Join(", ", character.Talents.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}");
				_inputSimulator.SystemWait(ScanDelay.Normal);

				// Scale down talents due to constellations
				if (character.Constellation >= 3)
				{
					if (_gameDataService.Characters.ContainsKey(name.ToLower()))
					{
						var constellationData = _gameDataService.Characters[name.ToLower()]["ConstellationOrder"];
						var elementOrder = character.NameGOOD.Contains("Traveler")
							? constellationData?[character.Element.ToLower()]
							: constellationData;

						if (elementOrder == null)
						{
							Logger.Warn("{0} constellation order not found for element '{1}', skipping talent adjustment", character.NameGOOD, character.Element);
						}
						else
						{
							string talentLeveledAtConst3 = (string)elementOrder[0];

							// Scale down talents (constellations give +3 bonus in-game)
							// Ensure talents don't go below 1 for low-level characters
							if (character.Constellation >= 5)
							{
								string talentLeveledAtConst5 = (string)elementOrder[1];
								Logger.Info("{0} constellation 5+, adjusting scanned {1} and {2} levels", character.NameGOOD, talentLeveledAtConst3, talentLeveledAtConst5);
								character.Talents[talentLeveledAtConst3] = Math.Max(1, character.Talents[talentLeveledAtConst3] - 3);
								character.Talents[talentLeveledAtConst5] = Math.Max(1, character.Talents[talentLeveledAtConst5] - 3);
							}
							else
							{
								Logger.Info("{0} constellation 3+, adjusting scanned {1} level", character.NameGOOD, talentLeveledAtConst3);
								character.Talents[talentLeveledAtConst3] = Math.Max(1, character.Talents[talentLeveledAtConst3] - 3);
							}
						}
					}
					else
						return character;
				}


				return character;
			}
			Logger.Info("Repeat character {0} detected. Finishing character scan...", name);
			return character;
		}

		public string ScanMainCharacterName()
		{
			var xReference = 1280.0;
			var yReference = 720.0;
			if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
			{
				yReference = 800.0;
			}

			RECT region = new RECT(
				Left:   (int)(185 / xReference * _screenCapture.GetWidth()),
				Top:    (int)(26  / yReference * _screenCapture.GetHeight()),
				Right:  (int)(460 / xReference * _screenCapture.GetWidth()),
				Bottom: (int)(60  / yReference * _screenCapture.GetHeight()));

			Image<Rgba32> nameBitmap = _screenCapture.CaptureRegion(region);

			//Image Operations
			var oldNameBitmap = nameBitmap;
			nameBitmap = _imageProcessor.SetGamma(nameBitmap, 0.2, 0.2, 0.2);
			if (!ReferenceEquals(oldNameBitmap, nameBitmap)) oldNameBitmap.Dispose();
			nameBitmap = _imageProcessor.SetInvert(nameBitmap);
			Image<Rgba32> n = _imageProcessor.SetGrayscale(nameBitmap);

			_userInterface.SetNavigation_Image(nameBitmap);

			string text = _ocrEngine.AnalyzeText(n).Trim();
			if (text != "")
			{
				// Only keep a-Z and 0-9
				text = Regex.Replace(text, @"[\W_]", string.Empty).ToLower();

				// Only keep text up until first space
				text = Regex.Replace(text, @"\s+\w*", string.Empty);

			}
			else
			{
				_userInterface.AddError(text);
			}
			n.Dispose();
			nameBitmap.Dispose();
			return text;
		}

		private void ScanNameAndElement(ref string name, ref string element)
		{
			int attempts = 0;
			int maxAttempts = 75;

			var xRef = 1280.0;
			var yRef = 720.0;
			if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
			{
				yRef = 800.0;
			}

			// Capture character name from right stats panel (above "Level" text)
			// Uses same left coordinate as Level region, positioned higher and ~66% wider
			// Name sits just above the star row, which is ~20-30px above Level (Top: 135)
			// Tightened to 85-120 to capture descenders without hitting star tops
			RECT nameRegion = new RECT(
				Left:   (int)( 960  / xRef * _screenCapture.GetWidth() ),
				Top:    (int)( 85   / yRef * _screenCapture.GetHeight() ),
				Right:  (int)( 1234 / xRef * _screenCapture.GetWidth() ),
				Bottom: (int)( 120  / yRef * _screenCapture.GetHeight() ));

			do
			{
				// Re-assert game foreground every retry — Rider/notifications/etc. can
				// surface their own window mid-loop, after which captures grab the wrong
				// content and OCR returns junk like "co" 75 times in a row.
				_inputSimulator.EnsureForeground();
				_inputSimulator.SystemWait(ScanDelay.Fast);
				using (Image<Rgba32> bm = _screenCapture.CaptureRegion(nameRegion))
				{
					// White-text mask first, original grayscale pipeline as fallback (see CharacterNameReader)
					foreach (string reading in CharacterNameReader.ReadCandidates(bm, _imageProcessor, _ocrEngine))
					{
					string text = reading.ToLower().Trim();
					Logger.Debug("Name OCR from right panel: '{0}'", text);

					// Clean up the text - remove non-alphanumeric except spaces
					text = Regex.Replace(text, @"[^a-z0-9\s]", string.Empty).Trim();

					// Remove extra whitespace
					text = Regex.Replace(text, @"\s+", string.Empty);

					Logger.Debug("Cleaned name text: '{0}'", text);

					// Try to match character name
					if (!string.IsNullOrWhiteSpace(text))
					{
						// Check for custom character names first
						string travelerName = (_settings != null ? _settings.TravelerName : null)?.ToLower()?.Trim();
						string wandererName = (_settings != null ? _settings.WandererName : null)?.ToLower()?.Trim();

						Logger.Debug("Checking custom names - Traveler: '{0}', Wanderer: '{1}'",
							travelerName ?? "(not set)", wandererName ?? "(not set)");

						if (!string.IsNullOrWhiteSpace(travelerName) && text.Equals(travelerName, StringComparison.OrdinalIgnoreCase))
						{
							Logger.Debug("Matched custom Traveler name: '{0}'", travelerName);
							name = "Traveler";
						}
						else if (!string.IsNullOrWhiteSpace(wandererName) && text.Equals(wandererName, StringComparison.OrdinalIgnoreCase))
						{
							Logger.Debug("Matched custom Wanderer name: '{0}'", wandererName);
							name = "Wanderer";
						}
						else
						{
							// Use fuzzy matching for regular characters
							name = _gameDataService.FindClosestCharacterName(text);
						}

						if (!string.IsNullOrWhiteSpace(name))
						{
							Logger.Debug("Matched character name: '{0}'", name);

							// Look up element from characters.json
							element = _gameDataService.GetElementForCharacter(name);

							// Special case: Traveler can be any element, need to check left panel for active element
							if (name.Contains("Traveler"))
							{
								Logger.Debug("Traveler detected, scanning left panel for active element");
								element = ScanTravelerElement();
							}

							if (!string.IsNullOrWhiteSpace(element))
							{
								Logger.Debug("Scanned character name as {0} with element {1}", name, element);
								_userInterface.SetCharacter_NameAndElement(bm, name, element);

								// Save screenshot if LogScreenshots is enabled
								if ((_settings != null ? _settings.LogScreenshots : false))
								{
									Directory.CreateDirectory($"./logging/characters/temp");
									bm.Save($"./logging/characters/temp/name_{name}_{DateTime.Now:yyyyMMddHHmmss}.png");
								}

								return;
							}
							else
							{
								Logger.Debug("Could not determine element for character '{0}'", name);
								name = "";
							}
						}
						else
						{
							Logger.Debug("Could not match OCR text '{0}' to any character", text);
						}
					}

					}

					Logger.Debug("Could not parse character name (Attempt {0}/{1}). Retrying...", attempts+1, maxAttempts);

					// Always save screenshot on failure for debugging
					Directory.CreateDirectory($"./logging/characters/failures");
					bm.Save($"./logging/characters/failures/name_fail_{DateTime.Now:yyyyMMddHHmmss}.png");
				}
				attempts++;
				_inputSimulator.SystemWait(ScanDelay.Fast);
			} while ( attempts < maxAttempts );

			Logger.Error("Failed to scan character name after {0} attempts", maxAttempts);
			name = null;
			element = null;
		}

		private string ScanTravelerElement()
		{
			// Use original left panel region that captures "element / name" format
			// This region was working reliably for element detection
			RECT elementRegion = new RECT(
				Left:   (int)( 85  / 1280.0 * _screenCapture.GetWidth() ),
				Top:    (int)( 10  / 720.0 * _screenCapture.GetHeight() ),
				Right:  (int)( 305 / 1280.0 * _screenCapture.GetWidth() ),
				Bottom: (int)( 55  / 720.0 * _screenCapture.GetHeight() ));

			using (Image<Rgba32> bm = _screenCapture.CaptureRegion(elementRegion))
			{
				Image<Rgba32> n = _imageProcessor.SetGrayscale(bm);
				var thresholded = _imageProcessor.SetThreshold(n, 110);
				n.Dispose();
				n = thresholded;
				var invertedN = _imageProcessor.SetInvert(n);
				n.Dispose();
				n = invertedN;

				var resizedN = _imageProcessor.ResizeImage(n, n.Width * 2, n.Height * 2);
				n.Dispose();
				n = resizedN;
				string block = _ocrEngine.AnalyzeText(n, (PageSegmentationMode)(int)TesseractOCR.Enums.PageSegMode.Auto).ToLower().Trim();
				string line = _ocrEngine.AnalyzeText(n, (PageSegmentationMode)(int)TesseractOCR.Enums.PageSegMode.SingleLine).ToLower().Trim();

				// Use line if it has a slash, otherwise use block
				string nameAndElement = line.Contains("/") ? line : block;

				Logger.Debug("Traveler element+name OCR: '{0}'", nameAndElement);

				// Extract element from "element / name" format
				if (nameAndElement.Contains("/"))
				{
					var split = nameAndElement.Split('/');
					string elementText = split[0].Trim();
					string element = _gameDataService.FindElementByName(elementText);

					n.Dispose();

					if (!string.IsNullOrWhiteSpace(element))
					{
						Logger.Debug("Traveler element identified as: '{0}'", element);
						return element;
					}
				}

				n.Dispose();
			}

			Logger.Debug("Could not determine Traveler element from left panel");
			return "";
		}

		private int ScanLevel(ref bool ascended)
		{
            int attempt = 0;

            var xRef = 1280.0;
			var yRef = 720.0;
			if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
			{
				yRef = 800.0;
			}

			RECT region =  new RECT(
				Left:   (int)( 960  / xRef * _screenCapture.GetWidth() ),
				Top:    (int)( 135  / yRef * _screenCapture.GetHeight() ),
				Right:  (int)( 1125 / xRef * _screenCapture.GetWidth() ),
				Bottom: (int)( 163  / yRef * _screenCapture.GetHeight() ));

			do
			{
				Image<Rgba32> bm = _screenCapture.CaptureRegion(region);

				var resized = _imageProcessor.ResizeImage(bm, bm.Width * 2, bm.Height * 2);
				bm.Dispose();
				bm = resized;
				Image<Rgba32> n = _imageProcessor.SetGrayscale(bm);
				var inverted = _imageProcessor.SetInvert(n);
				n.Dispose();
				n = inverted;
				var contrasted = _imageProcessor.SetContrast(bm, 30.0);
				bm.Dispose();
				bm = contrasted;

				string text = _ocrEngine.AnalyzeText(n).Trim();
				Logger.Debug("Scanned character level as {0}", text);

				if (CharacterLevelReader.TryParse(text, out int level, out int maxLevel, out bool isAscended))
				{
					ascended = isAscended;
					_userInterface.SetCharacter_Level(bm, level, maxLevel);
					n.Dispose();
					bm.Dispose();
					Logger.Debug("Parsed character level as {0}/{1}", level, maxLevel);
					return level;
				}
				Logger.Debug("Failed to parse character level and ascension from {0} (text), retrying", text);

				attempt++;

                n.Dispose();
                bm.Dispose();
                _inputSimulator.SystemWait(ScanDelay.Fast);
			} while (attempt < 50);

			return -1;
		}

		private int ScanConstellations(Character character)
		{
			Logger.Debug("Starting constellation scan for character: {0}", character.NameGOOD);
			double yReference = 720.0;
			int constellation;

			if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
			{
				yReference = 800.0;
			}

			RECT constActivate =  new RECT(
				Left:   (int)( 70 / 1280.0 * _screenCapture.GetWidth() ),
				Top:    (int)( 665 / 720.0 * _screenCapture.GetHeight() ),
				Right:  (int)( 100 / 1280.0 * _screenCapture.GetWidth() ),
				Bottom: (int)( 695 / 720.0 * _screenCapture.GetHeight() ));

			// Fast path (16:9): select the first constellation, then read all six nodes from one
			// screenshot. Clicking through each one took ~1.6 s per activated constellation.
			if (_screenCapture.GetAspectRatio() == new ISSize(16, 9))
			{
				_inputSimulator.SetCursor((int)( 1130 / 1280.0 * _screenCapture.GetWidth() ), (int)( 180 / yReference * _screenCapture.GetHeight() ));
				_inputSimulator.Click();
				_inputSimulator.SystemWaitMs(700);

				using (var screenshot = _screenCapture.CaptureWindow())
				{
					if ((_settings != null ? _settings.LogScreenshots : false))
					{
						Directory.CreateDirectory($"./logging/characters/{character.NameGOOD}");
						screenshot.Save($"./logging/characters/{character.NameGOOD}/constellation.png");
					}

					int? counted = ConstellationReader.CountActivated(screenshot);
					if (counted.HasValue)
					{
						Logger.Debug("Completed constellation scan for {0}: {1} constellations activated (single screenshot)", character.NameGOOD, counted.Value);
						Navigation.sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.ESCAPE);
						_userInterface.SetCharacter_Constellation(counted.Value);
						return counted.Value;
					}

					Logger.Info("Constellation screenshot for {0} was inconclusive; checking each constellation instead", character.NameGOOD);
				}
			}

			for (constellation = 0; constellation < 6; constellation++)
			{
				// Select Constellation
				int yOffset = (int)( ( 180 + ( constellation * 75 ) ) / yReference * _screenCapture.GetHeight() );

				if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
				{
					yOffset = (int)( ( 225 + ( constellation * 75 ) ) / yReference * _screenCapture.GetHeight() );
				}

				_inputSimulator.SetCursor((int)( 1130 / 1280.0 * _screenCapture.GetWidth() ), yOffset);
				_inputSimulator.Click();

				var pause = constellation == 0 ? 700 : 550;
				_inputSimulator.SystemWaitMs(pause);

				if ((_settings != null ? _settings.LogScreenshots : false))
				{
					var screenshot = _screenCapture.CaptureWindow();
					Directory.CreateDirectory($"./logging/characters/{character.NameGOOD}");
					screenshot.Save($"./logging/characters/{character.NameGOOD}/constellation_{constellation + 1}.png");
				}

				// Grab Color
				using (Image<Rgba32> region = _screenCapture.CaptureRegion(constActivate))
				{
					// Check a small region next to the text "Activate"
					// for a mostly white backround
					Rgba32 statistics = _imageProcessor.GetAverageColor(region);
					Logger.Debug("Constellation {0} color check - R: {1:F1}, G: {2:F1}, B: {3:F1}",
						constellation + 1, statistics.R, statistics.G, statistics.B);

					if (statistics.R >= 190 && statistics.G >= 190 && statistics.B >= 190)
					{
						Logger.Debug("Constellation {0} is not activated (found 'Activate' button). Total constellations: {1}",
							constellation + 1, constellation);
						break;
					}

				}
			}

			Logger.Debug("Completed constellation scan for {0}: {1} constellations activated", character.NameGOOD, constellation);
			Navigation.sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.ESCAPE);
			_userInterface.SetCharacter_Constellation(constellation);
			return constellation;
		}

		private Dictionary<string, int> ScanTalents(Character character)
		{
			Logger.Debug("Starting talent scan for character: {0}", character.NameGOOD);
			var talents = new Dictionary<string, int>
			{
				{ "auto" , -1 },
				{ "skill", -1 },
				{ "burst", -1 }
			};

			int specialOffset = 0;

			// Check if character has a movement talent like
			// Mona or Ayaka
			if (character.NameGOOD.Contains("Mona") || character.NameGOOD.Contains("Ayaka")) specialOffset = 1;

			var xRef = 1280.0;
			var yRef = 720.0;

			if (_screenCapture.GetAspectRatio() == new ISSize(8, 5)) yRef = 800.0;

			int rx = (int)((_screenCapture.IsNormal ? 0.0003 : 0) * _screenCapture.GetWidth());
			int ry = (int)((_screenCapture.IsNormal ? 0.1278 : 0) * _screenCapture.GetHeight());
			int rw = (int)((_screenCapture.IsNormal ? 0.2913 : 0) * _screenCapture.GetWidth());
			int rh = (int)((_screenCapture.IsNormal ? 0.0711 : 0) * _screenCapture.GetHeight());
			RECT region = new RECT(Left: rx, Top: ry, Right: rx + rw, Bottom: ry + rh);

			for (int i = 0; i < 3; i++)
			{
				string talent;
				// Change y-offset for talent clicking
				int yOffset = (int)( 110 / yRef * _screenCapture.GetHeight() ) + ( i + ( ( i == 2 ) ? specialOffset : 0 ) ) * (int)(60 / yRef * _screenCapture.GetHeight() );

				_inputSimulator.SetCursor((int)(1130 / xRef * _screenCapture.GetWidth()), yOffset);
				_inputSimulator.Click();
				int pause = i == 0 ? 700 : 550;
				_inputSimulator.SystemWaitMs(pause);
                switch (i)
                {
					default:
						talent = "auto";
						break;
					case 1:
						talent = "skill";
						break;
					case 2:
						talent = "burst";
						break;
                }

                while (talents[talent] < 1 || talents[talent] > 15)
				{
					Image<Rgba32> talentLevel = _screenCapture.CaptureRegion(region);

					talentLevel = _imageProcessor.ResizeImage(talentLevel, talentLevel.Width * 2, talentLevel.Height * 2);

					Image<Rgba32> n = _imageProcessor.SetGrayscale(talentLevel);
					var oldN = n;
					n = _imageProcessor.SetContrast(n, 60);
					if (!ReferenceEquals(oldN, n)) oldN.Dispose();
					n = _imageProcessor.SetInvert(n);

					var text = _ocrEngine.AnalyzeText(n, (PageSegmentationMode)(int)TesseractOCR.Enums.PageSegMode.SingleBlock).Trim().Split('\n').ToList();
					Logger.Debug("Talent '{0}' OCR raw text: '{1}'", talent, string.Join(" | ", text));

					if (int.TryParse(Regex.Replace(text.Last(), @"\D", string.Empty), out int level))
					{
						Logger.Debug("Parsed talent '{0}' level: {1}", talent, level);

						if (level >= 1 && level <= 15)
						{
							talents[talent] = level;
							_userInterface.SetCharacter_Talent(talentLevel, level.ToString(), i);

							// Save screenshot if LogScreenshots is enabled
							if ((_settings != null ? _settings.LogScreenshots : false))
							{
								Directory.CreateDirectory($"./logging/characters/{character.NameGOOD}");
								talentLevel.Save($"./logging/characters/{character.NameGOOD}/talent_{talent}.png");
							}

							Logger.Debug("Successfully scanned talent '{0}' with level {1} for character {2}", talent, level, character.NameGOOD);
						}
						else
						{
							Logger.Debug("Talent '{0}' level {1} out of valid range (1-15), retrying", talent, level);
						}
					}
					else
					{
						Logger.Debug("Failed to parse talent '{0}' level from text, retrying", talent);
					}

					n.Dispose();
					talentLevel.Dispose();
				}
			}

			Navigation.sim.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.ESCAPE);
			return talents;
		}
	}
}
