using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace InventoryKamera
{
    public static class CharacterScraper
	{
		private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static void ScanCharacters(ref List<Character> Characters)
		{
			int viewed = 0;
			string first = null;
			HashSet<string> scanned = new HashSet<string>();

			UserInterface.ResetCharacterDisplay();

			while (true)
			{
				var character = ScanCharacter(first);

				// Skip mannequins - Genshin Optimizer doesn't support them and they cause crashes
				if (character.NameGOOD.ToLower() == "manequin1" || character.NameGOOD.ToLower() == "manequin2")
				{
					Logger.Info("Skipping mannequin: {0}", character.NameGOOD);
					Navigation.SelectNextCharacter();
					UserInterface.ResetCharacterDisplay();
					continue;
				}

				if (Characters.Count > 0 && character.NameGOOD == Characters.ElementAt(0).NameGOOD) break;
				if (character.IsValid())
				{
                    if (!scanned.Contains(character.NameGOOD))
					{
						Characters.Add(character);
						UserInterface.IncrementCharacterCount();
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

				Navigation.SelectNextCharacter();
				UserInterface.ResetCharacterDisplay();

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

		private static Character ScanCharacter(string firstCharacter)
		{
			var character = new Character();
			Navigation.SelectCharacterAttributes();
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
				if (string.IsNullOrWhiteSpace(name)) UserInterface.AddError("Could not determine character's name");
				if (string.IsNullOrWhiteSpace(element)) UserInterface.AddError("Could not determine character's element");
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
					UserInterface.AddError($"Could not determine {character.NameGOOD}'s level. Setting to 1.");
					level = 1;
					ascended = false;
				}
				character.Level = level;
				character.Ascended = ascended;

				Logger.Info("{0} Level: {1}", character.NameGOOD, character.Level);
				Logger.Info("{0} Ascended: {1}", character.NameGOOD, character.Ascended);

				// Scan Experience
				//experience = ScanExperience();
				//Navigation.SystemRandomWait(Navigation.Speed.Normal);

				// Scan Constellation
				Navigation.SelectCharacterConstellation();
				character.Constellation = ScanConstellations(character);
				Logger.Info("{0} Constellation: {1}", character.NameGOOD, character.Constellation);
				Navigation.SystemWait(Navigation.Speed.Normal);

				// Scan Talents
				Navigation.SelectCharacterTalents();
				character.Talents = ScanTalents(character);
				Logger.Info("{0} Talents: {1}", character.NameGOOD, "{" + string.Join(", ", character.Talents.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}");
				Navigation.SystemWait(Navigation.Speed.Normal);

				// Scale down talents due to constellations
				if (character.Constellation >= 3)
				{
					if (GenshinProcesor.Characters.ContainsKey(name.ToLower()))
					{
						string talentLeveledAtConst3 = character.NameGOOD.Contains("Traveler")
                            ? (string)GenshinProcesor.Characters[name.ToLower()]["ConstellationOrder"][character.Element.ToLower()][0]
                            : (string)GenshinProcesor.Characters[name.ToLower()]["ConstellationOrder"][0];

                        // Scale down talents (constellations give +3 bonus in-game)
						// Ensure talents don't go below 1 for low-level characters
                        if (character.Constellation >= 5)
						{
							string talentLeveledAtConst5 = character.NameGOOD.Contains("Traveler")
                                ? (string)GenshinProcesor.Characters[name.ToLower()]["ConstellationOrder"][character.Element.ToLower()][1]
                                : (string)GenshinProcesor.Characters[name.ToLower()]["ConstellationOrder"][1];

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
					else
						return character;
				}


				return character;
			}
			Logger.Info("Repeat character {0} detected. Finishing character scan...", name);
			return character;
		}

		public static string ScanMainCharacterName()
		{
			var xReference = 1280.0;
			var yReference = 720.0;
			if (Navigation.GetAspectRatio() == new Size(8, 5))
			{
				yReference = 800.0;
			}

			RECT region = new RECT(
				Left:   (int)(185 / xReference * Navigation.GetWidth()),
				Top:    (int)(26  / yReference * Navigation.GetHeight()),
				Right:  (int)(460 / xReference * Navigation.GetWidth()),
				Bottom: (int)(60  / yReference * Navigation.GetHeight()));

			Bitmap nameBitmap = Navigation.CaptureRegion(region);

			//Image Operations
			GenshinProcesor.SetGamma(0.2, 0.2, 0.2, ref nameBitmap);
			GenshinProcesor.SetInvert(ref nameBitmap);
			Bitmap n = GenshinProcesor.ConvertToGrayscale(nameBitmap);

			UserInterface.SetNavigation_Image(nameBitmap);

			string text = GenshinProcesor.AnalyzeText(n).Trim();
			if (text != "")
			{
				// Only keep a-Z and 0-9
				text = Regex.Replace(text, @"[\W_]", string.Empty).ToLower();

				// Only keep text up until first space
				text = Regex.Replace(text, @"\s+\w*", string.Empty);

			}
			else
			{
				UserInterface.AddError(text);
			}
			n.Dispose();
			nameBitmap.Dispose();
			return text;
		}

		private static void ScanNameAndElement(ref string name, ref string element)
		{
			int attempts = 0;
			int maxAttempts = 75;

			var xRef = 1280.0;
			var yRef = 720.0;
			if (Navigation.GetAspectRatio() == new Size(8, 5))
			{
				yRef = 800.0;
			}

			// Capture character name from right stats panel (above "Level" text)
			// Uses same left coordinate as Level region, positioned higher and ~66% wider
			// Name sits just above the star row, which is ~20-30px above Level (Top: 135)
			// Tightened to 85-120 to capture descenders without hitting star tops
			Rectangle nameRegion = new RECT(
				Left:   (int)( 960  / xRef * Navigation.GetWidth() ),
				Top:    (int)( 85   / yRef * Navigation.GetHeight() ),
				Right:  (int)( 1234 / xRef * Navigation.GetWidth() ),
				Bottom: (int)( 120  / yRef * Navigation.GetHeight() ));

			do
			{
				Navigation.SystemWait(Navigation.Speed.Fast);
				using (Bitmap bm = Navigation.CaptureRegion(nameRegion))
				{
					Bitmap n = GenshinProcesor.ConvertToGrayscale(bm);
					// Increase contrast to make white text stand out from lighter backgrounds (fog effects)
					GenshinProcesor.SetContrast(60, ref n);
					// High threshold to capture only pure white text, filtering out light backgrounds
					GenshinProcesor.SetThreshold(200, ref n);
					GenshinProcesor.SetInvert(ref n);

					n = GenshinProcesor.ResizeImage(n, n.Width * 2, n.Height * 2);
					string text = GenshinProcesor.AnalyzeText(n, Tesseract.PageSegMode.SingleLine).ToLower().Trim();

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
						string travelerName = Properties.Settings.Default.TravelerName?.ToLower()?.Trim();
						string wandererName = Properties.Settings.Default.WandererName?.ToLower()?.Trim();

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
							name = GenshinProcesor.FindClosestCharacterName(text);
						}

						if (!string.IsNullOrWhiteSpace(name))
						{
							Logger.Debug("Matched character name: '{0}'", name);

							// Look up element from characters.json
							element = GenshinProcesor.GetElementForCharacter(name);

							// Special case: Traveler can be any element, need to check left panel for active element
							if (name.Contains("Traveler"))
							{
								Logger.Debug("Traveler detected, scanning left panel for active element");
								element = ScanTravelerElement();
							}

							if (!string.IsNullOrWhiteSpace(element))
							{
								Logger.Debug("Scanned character name as {0} with element {1}", name, element);
								UserInterface.SetCharacter_NameAndElement(bm, name, element);

								// Save screenshot if LogScreenshots is enabled
								if (Properties.Settings.Default.LogScreenshots)
								{
									Directory.CreateDirectory($"./logging/characters/temp");
									bm.Save($"./logging/characters/temp/name_{name}_{DateTime.Now:yyyyMMddHHmmss}.png");
								}

								n.Dispose();
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

					n.Dispose();

					Logger.Debug("Could not parse character name (Attempt {0}/{1}). Retrying...", attempts+1, maxAttempts);

					// Always save screenshot on failure for debugging
					Directory.CreateDirectory($"./logging/characters/failures");
					bm.Save($"./logging/characters/failures/name_fail_{DateTime.Now:yyyyMMddHHmmss}.png");
				}
				attempts++;
				Navigation.SystemWait(Navigation.Speed.Fast);
			} while ( attempts < maxAttempts );

			Logger.Error("Failed to scan character name after {0} attempts", maxAttempts);
			name = null;
			element = null;
		}

		private static string ScanTravelerElement()
		{
			// Use original left panel region that captures "element / name" format
			// This region was working reliably for element detection
			Rectangle elementRegion = new RECT(
				Left:   (int)( 85  / 1280.0 * Navigation.GetWidth() ),
				Top:    (int)( 10  / 720.0 * Navigation.GetHeight() ),
				Right:  (int)( 305 / 1280.0 * Navigation.GetWidth() ),
				Bottom: (int)( 55  / 720.0 * Navigation.GetHeight() ));

			using (Bitmap bm = Navigation.CaptureRegion(elementRegion))
			{
				Bitmap n = GenshinProcesor.ConvertToGrayscale(bm);
				GenshinProcesor.SetThreshold(110, ref n);
				GenshinProcesor.SetInvert(ref n);

				n = GenshinProcesor.ResizeImage(n, n.Width * 2, n.Height * 2);
				string block = GenshinProcesor.AnalyzeText(n, Tesseract.PageSegMode.Auto).ToLower().Trim();
				string line = GenshinProcesor.AnalyzeText(n, Tesseract.PageSegMode.SingleLine).ToLower().Trim();

				// Use line if it has a slash, otherwise use block
				string nameAndElement = line.Contains("/") ? line : block;

				Logger.Debug("Traveler element+name OCR: '{0}'", nameAndElement);

				// Extract element from "element / name" format
				if (nameAndElement.Contains("/"))
				{
					var split = nameAndElement.Split('/');
					string elementText = split[0].Trim();
					string element = GenshinProcesor.FindElementByName(elementText);

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

		private static int ScanLevel(ref bool ascended)
		{
            int attempt = 0;

            var xRef = 1280.0;
			var yRef = 720.0;
			if (Navigation.GetAspectRatio() == new Size(8, 5))
			{
				yRef = 800.0;
			}

			Rectangle region =  new RECT(
				Left:   (int)( 960  / xRef * Navigation.GetWidth() ),
				Top:    (int)( 135  / yRef * Navigation.GetHeight() ),
				Right:  (int)( 1125 / xRef * Navigation.GetWidth() ),
				Bottom: (int)( 163  / yRef * Navigation.GetHeight() ));

			do
			{
				Bitmap bm = Navigation.CaptureRegion(region);

				bm = GenshinProcesor.ResizeImage(bm, bm.Width * 2, bm.Height * 2);
				Bitmap n = GenshinProcesor.ConvertToGrayscale(bm);
				GenshinProcesor.SetInvert(ref n);
				GenshinProcesor.SetContrast(30.0, ref bm);

				string text = GenshinProcesor.AnalyzeText(n).Trim();
				Logger.Debug("Scanned character level as {0}", text);

				text = Regex.Replace(text, @"(?![0-9/]).", string.Empty);
				Logger.Debug("Filtered scanned text to {0}", text);
				if (text.Contains("/"))
				{
					var values = text.Split('/');
                    if (int.TryParse(values[0], out int level) && int.TryParse(values[1], out int maxLevel))
                    {
                        maxLevel = (int)Math.Round(maxLevel / 10.0, MidpointRounding.AwayFromZero) * 10;
                        ascended = 20 <= level && level < maxLevel;
                        UserInterface.SetCharacter_Level(bm, level, maxLevel);
                        n.Dispose();
                        bm.Dispose();
                        Logger.Debug("Parsed character level as {0}", level);
                        return level;
                    }
				}
				Logger.Debug("Failed to parse character level and ascension from {0} (text), retrying", text);

				attempt++;

                n.Dispose();
                bm.Dispose();
                Navigation.SystemWait(Navigation.Speed.Fast);
			} while (attempt < 50);

			return -1;
		}

		private static int ScanExperience()
		{
			int experience = 0;

			int xOffset = 1117;
			int yOffset = 151;
			Bitmap bm = new Bitmap(90, 10);
			Graphics g = Graphics.FromImage(bm);
			int screenLocation_X = Navigation.GetPosition().Left + xOffset;
			int screenLocation_Y = Navigation.GetPosition().Top + yOffset;
			g.CopyFromScreen(screenLocation_X, screenLocation_Y, 0, 0, bm.Size);

			//Image Operations
			bm = GenshinProcesor.ResizeImage(bm, bm.Width * 6, bm.Height * 6);
			//Scraper.ConvertToGrayscale(ref bm);
			//Scraper.SetInvert(ref bm);
			GenshinProcesor.SetContrast(30.0, ref bm);

			string text = GenshinProcesor.AnalyzeText(bm);
			text = text.Trim();
			text = Regex.Replace(text, @"(?![0-9\s/]).", string.Empty);

			if (Regex.IsMatch(text, "/"))
			{
				string[] temp = text.Split('/');
				experience = Convert.ToInt32(temp[0]);
			}
			else
			{
				Debug.Print("Error: Found " + experience + " instead of experience");
				UserInterface.AddError("Found " + experience + " instead of experience");
			}

			return experience;
		}

		private static int ScanConstellations(Character character)
		{
			Logger.Debug("Starting constellation scan for character: {0}", character.NameGOOD);
			double yReference = 720.0;
			int constellation;

			if (Navigation.GetAspectRatio() == new Size(8, 5))
			{
				yReference = 800.0;
			}

			Rectangle constActivate =  new RECT(
				Left:   (int)( 70 / 1280.0 * Navigation.GetWidth() ),
				Top:    (int)( 665 / 720.0 * Navigation.GetHeight() ),
				Right:  (int)( 100 / 1280.0 * Navigation.GetWidth() ),
				Bottom: (int)( 695 / 720.0 * Navigation.GetHeight() ));

			for (constellation = 0; constellation < 6; constellation++)
			{
				// Select Constellation
				int yOffset = (int)( ( 180 + ( constellation * 75 ) ) / yReference * Navigation.GetHeight() );

				if (Navigation.GetAspectRatio() == new Size(8, 5))
				{
					yOffset = (int)( ( 225 + ( constellation * 75 ) ) / yReference * Navigation.GetHeight() );
				}

				Navigation.SetCursor((int)( 1130 / 1280.0 * Navigation.GetWidth() ), yOffset);
				Navigation.Click();

				var pause = constellation == 0 ? 700 : 550;
				Navigation.SystemWait(pause);

				if (Properties.Settings.Default.LogScreenshots)
				{
					var screenshot = Navigation.CaptureWindow();
					Directory.CreateDirectory($"./logging/characters/{character.NameGOOD}");
					screenshot.Save($"./logging/characters/{character.NameGOOD}/constellation_{constellation + 1}.png");
				}

				// Grab Color
				using (Bitmap region = Navigation.CaptureRegion(constActivate))
				{
					// Check a small region next to the text "Activate"
					// for a mostly white backround
					Color statistics = GenshinProcesor.GetAverageColor(region);
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
			UserInterface.SetCharacter_Constellation(constellation);
			return constellation;
		}

		private static Dictionary<string, int> ScanTalents(Character character)
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

			if (Navigation.GetAspectRatio() == new Size(8, 5)) yRef = 800.0;

			Rectangle region =  new Rectangle(
				x:		(int)((Navigation.IsNormal ? 0.0003 : 0) * Navigation.GetWidth() ),
				y:		(int)((Navigation.IsNormal ? 0.1278 : 0) * Navigation.GetHeight() ),
				width:	(int)((Navigation.IsNormal ? 0.2913 : 0) * Navigation.GetWidth() ),
				height:	(int)((Navigation.IsNormal ? 0.0711 : 0) * Navigation.GetHeight() ));

			for (int i = 0; i < 3; i++)
			{
				string talent;
				// Change y-offset for talent clicking
				int yOffset = (int)( 110 / yRef * Navigation.GetHeight() ) + ( i + ( ( i == 2 ) ? specialOffset : 0 ) ) * (int)(60 / yRef * Navigation.GetHeight() );

				Navigation.SetCursor((int)(1130 / xRef * Navigation.GetWidth()), yOffset);
				Navigation.Click();
				int pause = i == 0 ? 700 : 550;
				Navigation.SystemWait(pause);
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
					Bitmap talentLevel = Navigation.CaptureRegion(region);

					talentLevel = GenshinProcesor.ResizeImage(talentLevel, talentLevel.Width * 2, talentLevel.Height * 2);

					Bitmap n = GenshinProcesor.ConvertToGrayscale(talentLevel);
					GenshinProcesor.SetContrast(60, ref n);
					GenshinProcesor.SetInvert(ref n);

					var text = GenshinProcesor.AnalyzeText(n, Tesseract.PageSegMode.SingleBlock).Trim().Split('\n').ToList();
					Logger.Debug("Talent '{0}' OCR raw text: '{1}'", talent, string.Join(" | ", text));

					if (int.TryParse(Regex.Replace(text.Last(), @"\D", string.Empty), out int level))
					{
						Logger.Debug("Parsed talent '{0}' level: {1}", talent, level);

						if (level >= 1 && level <= 15)
						{
							talents[talent] = level;
							UserInterface.SetCharacter_Talent(talentLevel, level.ToString(), i);

							// Save screenshot if LogScreenshots is enabled
							if (Properties.Settings.Default.LogScreenshots)
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