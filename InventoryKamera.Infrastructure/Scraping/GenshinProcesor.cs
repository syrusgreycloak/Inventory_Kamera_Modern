using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using TesseractOCR.Enums;
using OcrPool = InventoryKamera.Infrastructure.OcrEnginePool;
using InventoryKamera.Infrastructure.Compat;
using InventoryKamera.Infrastructure.Windows; // Navigation
using DrawingColor     = System.Drawing.Color;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSize      = System.Drawing.Size;

namespace InventoryKamera.Infrastructure.Scraping
{
    public static class GenshinProcesor
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public static Dictionary<string, string> Stats = new Dictionary<string, string>
		{
			["hp"] = "hp",
			["hp%"] = "hp_",
			["atk"] = "atk",
			["atk%"] = "atk_",
			["def"] = "def",
			["def%"] = "def_",
			["energyrecharge"] = "enerRech_",
			["elementalmastery"] = "eleMas",
			["healingbonus"] = "heal_",
			["critrate"] = "critRate_",
			["critdmg"] = "critDMG_",
			["physicaldmgbonus"] = "physical_dmg_",
		};

		public static readonly List<string> gearSlots = new List<string>
		{
			"flower",
			"plume",
			"sands",
			"goblet",
			"circlet",
		};

		private static readonly List<string> elements = new List<string>
		{
			"pyro",
			"hydro",
			"dendro",
			"electro",
			"anemo",
			"cryo",
			"geo",
		};

		public static readonly HashSet<string> enhancementMaterials = new HashSet<string>
		{
			"enhancementore",
			"fineenhancementore",
			"mysticenhancementore",
			"sanctifyingunction",
			"sanctifyingessence",
		};

		public static readonly List<string> customNames = new List<string>
		{
			"Traveler",
			"Wanderer",
			"Manequin1",
			"Manequin2"
		};

		private static readonly OcrPool _ocrPool;

		public static Dictionary<string, string> Weapons, DevItems, Materials, Elements;

		public static Dictionary<string, JObject> Characters, Artifacts;

		private static GameDataService _gameData;

		private static GameDataService GameData
		{
			get
			{
				if (_gameData == null)
				{
					_gameData = new GameDataService(new DatabaseManager());
					Elements = _gameData.Elements;
					Characters = _gameData.Characters;
					Artifacts = _gameData.Artifacts;
					Weapons = _gameData.Weapons;
					DevItems = _gameData.DevItems;
					Materials = _gameData.Materials;
					Stats = _gameData.Stats;
				}
				return _gameData;
			}
		}

		static GenshinProcesor()
        {
            _ocrPool = new OcrPool();
            Logger.Info("Scraper initialized");
        }

		public static void ReloadData()
        {
            GameData.ReloadData();
            Characters = _gameData.Characters;
            Artifacts = _gameData.Artifacts;
            Weapons = _gameData.Weapons;
            DevItems = _gameData.DevItems;
            Materials = _gameData.Materials;
            Stats = _gameData.Stats;
		}

		public static void UpdateCharacterName(string target, string name) =>
            GameData.UpdateCharacterName(target, name);

		// TODO Task 9: replace with characterScraper.ScanMainCharacterName() — CharacterScraper is now an instance class
		// internal static void AssignTravelerName(string name)
		// {
		// 	name = string.IsNullOrWhiteSpace(name) ? CharacterScraper.ScanMainCharacterName() : name.ToLower();
		// 	if (!string.IsNullOrWhiteSpace(name))
		// 	{
		// 		UpdateCharacterName("traveler", name);
		// 		UserInterface.SetMainCharacterName(name);
		// 	}
		// 	else
		// 	{
		// 		UserInterface.AddError("Could not parse Traveler's username");
		// 	}
		// }

		#region OCR

		public static void RestartEngines() => _ocrPool.RestartEngines();

		/// <summary> Use Tesseract OCR to find words on picture to string (ImageSharp overload). </summary>
		public static string AnalyzeText(Image<Rgba32> image, PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false)
		{
			var coreMode = (PageSegmentationMode)(int)pageMode;
			return _ocrPool.AnalyzeText(image, coreMode, numbersOnly);
		}

		/// <summary> Use Tesseract OCR to find words on picture to string (GDI+ overload — used by internal Bitmap-based scan methods). </summary>
		public static string AnalyzeText(Bitmap bitmap, PageSegMode pageMode = PageSegMode.SingleLine, bool numbersOnly = false)
		{
			var coreMode = (PageSegmentationMode)(int)pageMode;
			using (var ms = new MemoryStream())
			{
				bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				ms.Position = 0;
				var img = SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
				return _ocrPool.AnalyzeText(img, coreMode, numbersOnly);
			}
		}

		#endregion OCR

		#region Check valid parameters

		public static bool IsValidSetName(string s) => GameData.IsValidSetName(s);
		public static bool IsValidMaterial(string s) => GameData.IsValidMaterial(s);
		public static bool IsValidStat(string s) => GameData.IsValidStat(s);
		public static bool IsValidSlot(string s) => GameData.IsValidSlot(s);
		public static bool IsValidCharacter(string s) => GameData.IsValidCharacter(s);
		public static bool IsValidElement(string s) => GameData.IsValidElement(s);
		public static bool IsEnhancementMaterial(string s) => GameData.IsEnhancementMaterial(s);
		public static bool IsValidWeapon(string s) => GameData.IsValidWeapon(s);

		#endregion Check valid parameters

		#region Element Searching

		public static string FindClosestGearSlot(string input) => GameData.FindClosestGearSlot(input);
		public static string FindClosestStat(string stat, int minConfidence = 90) => GameData.FindClosestStat(stat, minConfidence);
		public static string FindElementByName(string name, int minConfidence = 90) => GameData.FindElementByName(name, minConfidence);
		public static string FindClosestWeapon(string name, int minConfidence = 90) => GameData.FindClosestWeapon(name, minConfidence);
		public static string FindClosestSetName(string name, int minConfidence = 90) => GameData.FindClosestSetName(name, minConfidence);
		public static string FindClosestArtifactSetFromArtifactName(string name, int minConfidence = 90) => GameData.FindClosestArtifactSetFromArtifactName(name, minConfidence);
		public static string FindClosestCharacterName(string name, int minConfidence = 90) => GameData.FindClosestCharacterName(name, minConfidence);
		public static string FindClosestDevelopmentName(string name, int minConfidence = 90) => GameData.FindClosestDevelopmentName(name, minConfidence);
		public static string FindClosestMaterialName(string name, int minConfidence = 90) => GameData.FindClosestMaterialName(name, minConfidence);
		public static bool CharacterMatchesElement(string name, string element) => GameData.CharacterMatchesElement(name, element);
		public static List<string> GetCharactersElements(string name) => GameData.GetCharactersElements(name);
		public static string GetElementForCharacter(string name) => GameData.GetElementForCharacter(name);

        #endregion Element Searching


        public static void FindDelay(List<DrawingRectangle> rectangles)
		{
			Navigation.SetDelay(180);
			int delayOffset = 20;
			bool bStoppedOnce = false;
			Bitmap card1; Bitmap card2; Bitmap card3;
			DrawingRectangle item1 = rectangles[0];

			RECT reference; int width = Navigation.GetWidth(); int height = Navigation.GetHeight();

			#region Get first card

			if (Navigation.GetAspectRatio() == new DrawingSize(16, 9))
			{
				reference = new DrawingRectangle(862, 80, 327, 560).ToRECT();

				int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
				int top = (int)Math.Round(reference.Top / 720.0 * height, MidpointRounding.AwayFromZero);
				int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
				int bottom = (int)Math.Round(reference.Bottom / 720.0 * height, MidpointRounding.AwayFromZero);

				card1 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
			}
			else // if (Navigation.GetAspectRatio() == new DrawingSize(8, 5))
			{
				reference = new DrawingRectangle(862, 80, 327, 640).ToRECT();

				int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
				int top = (int)Math.Round(reference.Top / 800.0 * height, MidpointRounding.AwayFromZero);
				int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
				int bottom = (int)Math.Round(reference.Bottom / 800.0 * height, MidpointRounding.AwayFromZero);

				card1 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
			}

			#endregion Get first card

			do
			{
				if (bStoppedOnce)
					delayOffset = 10;

				// Do mouse movement to first and second UI element in Inventory
				Navigation.SetCursor(item1.Center().X, item1.Center().Y);
				Navigation.Click();
				Navigation.Wait(((int)Navigation.GetDelay()) - delayOffset);

				DrawingRectangle item2 = rectangles[1];
				Navigation.SetCursor(item2.Center().X, item2.Center().Y);
				Navigation.Click();
				Navigation.Wait(((int)Navigation.GetDelay()) - delayOffset);

				// Take image after second click
				if (Navigation.GetAspectRatio() == new DrawingSize(16, 9))
				{
					int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
					int top = (int)Math.Round(reference.Top / 720.0 * height, MidpointRounding.AwayFromZero);
					int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
					int bottom = (int)Math.Round(reference.Bottom / 720.0 * height, MidpointRounding.AwayFromZero);

					card2 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
				}
				else
				{
					int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
					int top = (int)Math.Round(reference.Top / 800.0 * height, MidpointRounding.AwayFromZero);
					int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
					int bottom = (int)Math.Round(reference.Bottom / 800.0 * height, MidpointRounding.AwayFromZero);

					card2 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
				}

				DrawingRectangle item3 = rectangles[2];
				Navigation.SetCursor(item3.Center().X, item3.Center().Y);
				Navigation.Click();
				Navigation.Wait(((int)Navigation.GetDelay()) - delayOffset);

				// Take image after third click
				if (Navigation.GetAspectRatio() == new DrawingSize(16, 9))
				{
					int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
					int top = (int)Math.Round(reference.Top / 720.0 * height, MidpointRounding.AwayFromZero);
					int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
					int bottom = (int)Math.Round(reference.Bottom / 720.0 * height, MidpointRounding.AwayFromZero);

					card3 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
				}
				else
				{
					int left = (int)Math.Round(reference.Left / 1280.0 * width, MidpointRounding.AwayFromZero);
					int top = (int)Math.Round(reference.Top / 800.0 * height, MidpointRounding.AwayFromZero);
					int right = (int)Math.Round(reference.Right / 1280.0 * width, MidpointRounding.AwayFromZero);
					int bottom = (int)Math.Round(reference.Bottom / 800.0 * height, MidpointRounding.AwayFromZero);

					card3 = Navigation.CaptureRegion(new RECT(left, top, right, bottom));
				}

				// logic for continuing to run
				if (!CompareBitmapsFast(card1, card2) && !CompareBitmapsFast(card2, card3))
				{
					Navigation.SetDelay(Navigation.GetDelay() - delayOffset);
					//Navigation.SystemRandomWait();

					Navigation.SetCursor(item1.Center().X, item1.Center().Y);
					Navigation.Click();
					//Navigation.SystemRandomWait();
				}
				else
				{
					if (bStoppedOnce)
					{
                    }
                    bStoppedOnce = true;
				}
			} while (!bStoppedOnce && ( Navigation.GetDelay() - delayOffset > 0 ));

			// delay of compare function
			Navigation.SetDelay(Navigation.GetDelay() + 7);
			Debug.WriteLine($"Delay found:  {Navigation.GetDelay()}");
			card1.Dispose(); card2.Dispose();

			// set back to first element
			Navigation.SystemWait(Navigation.Speed.Slowest);
			Navigation.SetCursor(item1.Center().X, item1.Center().Y);
			Navigation.Click();
			Navigation.SystemWait(Navigation.Speed.Slower);
		}

        #region Image Operations

        public static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
		{
			var destRect = new DrawingRectangle(0, 0, width, height);
			var destImage = new Bitmap(width, height);

			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}

			return destImage;
		}

		public static Bitmap ResizeImage(Bitmap image, DrawingSize tSize)
		{
			int targetWidth = (int)Math.Round((double)image.Width * (tSize.Height / tSize.Width));
			int targetHeight = (int)Math.Round((double)image.Height * (tSize.Height / tSize.Width));
			using (var reSized = new Bitmap(targetWidth, targetHeight))
			using (var g = Graphics.FromImage(reSized))
			{
				g.DrawImage(image, 0,0, targetWidth, targetHeight);
				return (Bitmap)reSized.Clone();
			}
		}

		public static Bitmap ScaleImage(System.Drawing.Image image, double factor)
		{
			return ResizeImage(image, (int)( image.Width * factor ), (int)( image.Height * factor ));
		}

		public static bool CompareColors(DrawingColor a, DrawingColor b)
		{
			int[] diff = new int[3];
			diff[0] = Math.Abs(a.R - b.R);
			diff[1] = Math.Abs(a.G - b.G);
			diff[2] = Math.Abs(a.B - b.B);

			return diff[0] < 10 && diff[1] < 10 && diff[2] < 10;
		}

		public static DrawingColor ClosestColor(List<DrawingColor> colors, DrawingColor color)
		{
			var diff = colors.Select(x => new { Value = x, Diff = GetColorDifference(x, color) }).ToList();

			foreach (var c in colors)
            {
                if (CompareColors(c, color)) return c;
            }

            return diff.Find(x=> x.Diff == diff.Min(y=>y.Diff)).Value;
		}

		public static DrawingColor GetAverageColor(Bitmap bitmap)
		{
			long r = 0, g = 0, b = 0;
			int count = 0;
			BitmapData data = bitmap.LockBits(new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadOnly, bitmap.PixelFormat);
			int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
			int stride = data.Stride;
			byte[] pixels = new byte[stride * bitmap.Height];
			Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
			bitmap.UnlockBits(data);
			for (int y = 0; y < bitmap.Height; y++)
				for (int x = 0; x < bitmap.Width; x++)
				{
					int pos = y * stride + x * bytesPerPixel;
					if (bytesPerPixel >= 3)
					{
						b += pixels[pos];
						g += pixels[pos + 1];
						r += pixels[pos + 2];
						count++;
					}
				}
			if (count == 0) return DrawingColor.Black;
			return DrawingColor.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
		}

        private static int GetColorDifference(DrawingColor c, DrawingColor c2)
        {
			int r = c.R - c2.R, g = c.G - c2.G, b = c.B - c2.B;
			return r*r + g*g + b*b;
		}

        public static Bitmap ConvertToGrayscale(Bitmap bitmap)
		{
			Bitmap grayscale = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(grayscale))
			{
				System.Drawing.Imaging.ColorMatrix cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
				{
					new float[] { 0.2125f, 0.2125f, 0.2125f, 0, 0 },
					new float[] { 0.7154f, 0.7154f, 0.7154f, 0, 0 },
					new float[] { 0.0721f, 0.0721f, 0.0721f, 0, 0 },
					new float[] { 0,       0,       0,       1, 0 },
					new float[] { 0,       0,       0,       0, 1 }
				});
				using (ImageAttributes ia = new ImageAttributes())
				{
					ia.SetColorMatrix(cm);
					g.DrawImage(bitmap, new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height),
						0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, ia);
				}
			}
			return grayscale;
		}

		public static void SetContrast(double contrast, ref Bitmap bitmap)
		{
			int c = (int)contrast;
			double factor = (259.0 * (c + 255)) / (255.0 * (259 - c));
			byte[] lut = new byte[256];
			for (int i = 0; i < 256; i++)
				lut[i] = (byte)Math.Max(0, Math.Min(255, (int)(factor * (i - 128) + 128)));

			BitmapData data = bitmap.LockBits(new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, bitmap.PixelFormat);
			try
			{
				int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
				int stride = data.Stride;
				byte[] pixels = new byte[stride * bitmap.Height];
				Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
				for (int y = 0; y < bitmap.Height; y++)
					for (int x = 0; x < bitmap.Width; x++)
					{
						int pos = y * stride + x * bytesPerPixel;
						int channels = Math.Min(bytesPerPixel, 3);
						for (int ch = 0; ch < channels; ch++)
							pixels[pos + ch] = lut[pixels[pos + ch]];
					}
				Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		public static void SetGamma(double red, double green, double blue, ref Bitmap bitmap)
		{
			// LockBits is ~100× faster than per-pixel GetPixel/SetPixel and avoids the
			// "Generic GDI+ error" that the original implementation hit on bitmaps cloned
			// from PNG-decoded streams (worker thread, ImageSharp bridge path).
			byte[] redGamma   = CreateGammaArray(red);
			byte[] greenGamma = CreateGammaArray(green);
			byte[] blueGamma  = CreateGammaArray(blue);

			var rect = new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			try
			{
				int byteCount = Math.Abs(data.Stride) * data.Height;
				byte[] pixels = new byte[byteCount];
				Marshal.Copy(data.Scan0, pixels, 0, byteCount);

				// Format32bppArgb laid out as B, G, R, A in memory.
				for (int i = 0; i + 3 < pixels.Length; i += 4)
				{
					pixels[i]     = blueGamma[pixels[i]];
					pixels[i + 1] = greenGamma[pixels[i + 1]];
					pixels[i + 2] = redGamma[pixels[i + 2]];
					// alpha (pixels[i + 3]) preserved
				}

				Marshal.Copy(pixels, 0, data.Scan0, byteCount);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		private static byte[] CreateGammaArray(double color)
		{
			byte[] gammaArray = new byte[256];
			for (int i = 0; i < 256; ++i)
			{
				gammaArray[i] = (byte)Math.Min(255,
		(int)( ( 255.0 * Math.Pow(i / 255.0, 1.0 / color) ) + 0.5 ));
			}
			return gammaArray;
		}

		public static void SetInvert(ref Bitmap bitmap)
		{
			BitmapData data = bitmap.LockBits(new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, bitmap.PixelFormat);
			try
			{
				int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
				int stride = data.Stride;
				byte[] pixels = new byte[stride * bitmap.Height];
				Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
				for (int y = 0; y < bitmap.Height; y++)
					for (int x = 0; x < bitmap.Width; x++)
					{
						int pos = y * stride + x * bytesPerPixel;
						int channels = Math.Min(bytesPerPixel, 3);
						for (int ch = 0; ch < channels; ch++)
							pixels[pos + ch] = (byte)(255 - pixels[pos + ch]);
					}
				Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		public static void SetColor(string colorFilterType, ref Bitmap bitmap)
		{
			// LockBits-based in-place implementation. Same rationale as SetGamma/SetBrightness.
			var rect = new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			try
			{
				int byteCount = Math.Abs(data.Stride) * data.Height;
				byte[] pixels = new byte[byteCount];
				Marshal.Copy(data.Scan0, pixels, 0, byteCount);

				for (int i = 0; i + 3 < pixels.Length; i += 4)
				{
					// Format32bppArgb: B, G, R, A
					int B = pixels[i];
					int G = pixels[i + 1];
					int R = pixels[i + 2];
					int nR = 0, nG = 0, nB = 0;

					if (colorFilterType == "red")        { nR = R;        nG = G - 255; nB = B - 255; }
					else if (colorFilterType == "green") { nR = R - 255;  nG = G;       nB = B - 255; }
					else if (colorFilterType == "blue")  { nR = R - 255;  nG = G - 255; nB = B; }

					if (nR < 0) nR = 0; else if (nR > 255) nR = 255;
					if (nG < 0) nG = 0; else if (nG > 255) nG = 255;
					if (nB < 0) nB = 0; else if (nB > 255) nB = 255;

					pixels[i]     = (byte)nB;
					pixels[i + 1] = (byte)nG;
					pixels[i + 2] = (byte)nR;
				}

				Marshal.Copy(pixels, 0, data.Scan0, byteCount);
			}
			finally { bitmap.UnlockBits(data); }
		}

		public static void SetBrightness(int brightness, ref Bitmap bitmap)
		{
			if (brightness < -255) brightness = -255;
			if (brightness > 255) brightness = 255;

			// LockBits-based in-place implementation. Replaces the per-pixel SetPixel +
			// Clone-on-output pattern that hit "Generic GDI+ error" on bridge-converted bitmaps.
			var rect = new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			try
			{
				int byteCount = Math.Abs(data.Stride) * data.Height;
				byte[] pixels = new byte[byteCount];
				Marshal.Copy(data.Scan0, pixels, 0, byteCount);

				for (int i = 0; i + 3 < pixels.Length; i += 4)
				{
					// Format32bppArgb: B, G, R, A
					int cB = pixels[i]     + brightness;
					int cG = pixels[i + 1] + brightness;
					int cR = pixels[i + 2] + brightness;

					if (cR < 0) cR = 1; else if (cR > 255) cR = 255;
					if (cG < 0) cG = 1; else if (cG > 255) cG = 255;
					if (cB < 0) cB = 1; else if (cB > 255) cB = 255;

					pixels[i]     = (byte)cB;
					pixels[i + 1] = (byte)cG;
					pixels[i + 2] = (byte)cR;
				}

				Marshal.Copy(pixels, 0, data.Scan0, byteCount);
			}
			finally { bitmap.UnlockBits(data); }
		}

		public static void SetThreshold(int threshold, ref Bitmap bitmap)
		{
			BitmapData data = bitmap.LockBits(new DrawingRectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, bitmap.PixelFormat);
			try
			{
				int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
				int stride = data.Stride;
				byte[] pixels = new byte[stride * bitmap.Height];
				Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
				for (int y = 0; y < bitmap.Height; y++)
					for (int x = 0; x < bitmap.Width; x++)
					{
						int pos = y * stride + x * bytesPerPixel;
						byte gray = pixels[pos]; // B channel (grayscale B=G=R)
						byte val = gray < threshold ? (byte)0 : (byte)255;
						int channels = Math.Min(bytesPerPixel, 3);
						for (int ch = 0; ch < channels; ch++)
							pixels[pos + ch] = val;
					}
				Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			}
			finally
			{
				bitmap.UnlockBits(data);
			}
		}

		public static void FilterColors(ref Bitmap bm, IntRange red, IntRange green, IntRange blue)
		{
			BitmapData data = bm.LockBits(new DrawingRectangle(0, 0, bm.Width, bm.Height),
				ImageLockMode.ReadWrite, bm.PixelFormat);
			try
			{
			int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bm.PixelFormat) / 8;
			int stride = data.Stride;
			byte[] pixels = new byte[stride * bm.Height];
			Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
			for (int y = 0; y < bm.Height; y++)
				for (int x = 0; x < bm.Width; x++)
				{
					int pos = y * stride + x * bytesPerPixel;
					if (bytesPerPixel >= 3)
					{
						byte b = pixels[pos];
						byte g = pixels[pos + 1];
						byte r = pixels[pos + 2];
						if (r < red.Min || r > red.Max || g < green.Min || g > green.Max || b < blue.Min || b > blue.Max)
						{
							pixels[pos] = 255;
							pixels[pos + 1] = 255;
							pixels[pos + 2] = 255;
							if (bytesPerPixel == 4) pixels[pos + 3] = 255;
						}
					}
				}
			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			}
			finally
			{
				bm.UnlockBits(data);
			}
		}

		public static bool CompareBitmapsFast(Bitmap bmp1, Bitmap bmp2)
		{
			if (bmp1 == null || bmp2 == null)
				return false;
			if (object.Equals(bmp1, bmp2))
				return true;
			if (!bmp1.Size.Equals(bmp2.Size) || !bmp1.PixelFormat.Equals(bmp2.PixelFormat))
				return false;

			int bytes = bmp1.Width * bmp1.Height * (System.Drawing.Image.GetPixelFormatSize(bmp1.PixelFormat) / 8);

			bool result = true;
			byte[] b1bytes = new byte[bytes];
			byte[] b2bytes = new byte[bytes];

			BitmapData bitmapData1 = bmp1.LockBits(new DrawingRectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
			BitmapData bitmapData2 = bmp2.LockBits(new DrawingRectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, bmp2.PixelFormat);

			Marshal.Copy(bitmapData1.Scan0, b1bytes, 0, bytes);
			Marshal.Copy(bitmapData2.Scan0, b2bytes, 0, bytes);

			for (int n = 0; n <= bytes - 1; n++)
			{
				if (b1bytes[n] != b2bytes[n])
				{
					result = false;
					break;
				}
			}

			bmp1.UnlockBits(bitmapData1);
			bmp2.UnlockBits(bitmapData2);

			return result;
		}

		public static Bitmap PreProcessImage(Bitmap image)
		{
			using (var edges = KirschEdgeDetect(image))
			using (var grayscale = ConvertToGrayscale(edges))
			{
				Bitmap result = (Bitmap)grayscale.Clone();
				SetThreshold(70, ref result);
				return result;
			}
		}

		public static Bitmap KirschEdgeDetect(Bitmap source)
		{
			int[][][] kernels = new int[][][]
			{
				new int[][] { new int[] {  5,  5,  5 }, new int[] { -3,  0, -3 }, new int[] { -3, -3, -3 } },
				new int[][] { new int[] {  5,  5, -3 }, new int[] {  5,  0, -3 }, new int[] { -3, -3, -3 } },
				new int[][] { new int[] {  5, -3, -3 }, new int[] {  5,  0, -3 }, new int[] {  5, -3, -3 } },
				new int[][] { new int[] { -3, -3, -3 }, new int[] {  5,  0, -3 }, new int[] {  5,  5, -3 } },
				new int[][] { new int[] { -3, -3, -3 }, new int[] { -3,  0, -3 }, new int[] {  5,  5,  5 } },
				new int[][] { new int[] { -3, -3, -3 }, new int[] { -3,  0,  5 }, new int[] { -3,  5,  5 } },
				new int[][] { new int[] { -3, -3,  5 }, new int[] { -3,  0,  5 }, new int[] { -3, -3,  5 } },
				new int[][] { new int[] { -3,  5,  5 }, new int[] { -3,  0,  5 }, new int[] { -3, -3, -3 } }
			};

			Bitmap src = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage(src)) g.DrawImage(source, 0, 0);
			Bitmap result = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			BitmapData srcData = src.LockBits(new DrawingRectangle(0, 0, src.Width, src.Height),
				ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			BitmapData dstData = result.LockBits(new DrawingRectangle(0, 0, result.Width, result.Height),
				ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			try
			{
				int stride = srcData.Stride;
				byte[] srcPixels = new byte[stride * src.Height];
				byte[] dstPixels = new byte[stride * result.Height];
				Marshal.Copy(srcData.Scan0, srcPixels, 0, srcPixels.Length);

				for (int y = 1; y < src.Height - 1; y++)
				{
					for (int x = 1; x < src.Width - 1; x++)
					{
						int maxResponse = 0;
						foreach (var kernel in kernels)
						{
							int response = 0;
							for (int ky = 0; ky < 3; ky++)
								for (int kx = 0; kx < 3; kx++)
								{
									int pixPos = (y + ky - 1) * stride + (x + kx - 1) * 4;
									int gray = (srcPixels[pixPos + 2] + srcPixels[pixPos + 1] + srcPixels[pixPos]) / 3;
									response += kernel[ky][kx] * gray;
								}
							if (response > maxResponse) maxResponse = response;
						}
						byte edge = (byte)Math.Min(255, Math.Max(0, maxResponse));
						int pos = y * stride + x * 4;
						dstPixels[pos] = edge;
						dstPixels[pos + 1] = edge;
						dstPixels[pos + 2] = edge;
						dstPixels[pos + 3] = 255;
					}
				}

				Marshal.Copy(dstPixels, 0, dstData.Scan0, dstPixels.Length);
			}
			finally
			{
				src.UnlockBits(srcData);
				result.UnlockBits(dstData);
			}
			src.Dispose();
			return result;
		}

		public static Bitmap CopyBitmap(Bitmap source, DrawingRectangle region)
		{
			ClipToSource(source, ref region);
            return source.Clone(region, source.PixelFormat);

			void ClipToSource(Bitmap s, ref DrawingRectangle r)
			{
				if (r.X + r.Width > source.Width) { r.Width = s.Width - r.X; }
				if (r.Y + r.Height > source.Height) { r.Height = s.Height - r.Y; }
			}
        }

		#endregion Image Operations

		#region ImageSharp Bridge

		// Conversion helpers — keep allocation minimal by going through PNG streams.

		public static Bitmap ImageSharpToBitmap(Image<Rgba32> img)
		{
			// new Bitmap(stream) keeps a reference to the stream — disposing the stream
			// invalidates the bitmap. Clone into a stream-independent Format32bppArgb
			// bitmap so SetPixel and LockBits work correctly downstream.
			using (var ms = new MemoryStream())
			{
				img.Save(ms, new PngEncoder());
				ms.Position = 0;
				using (var temp = new Bitmap(ms))
				{
					return temp.Clone(
						new System.Drawing.Rectangle(0, 0, temp.Width, temp.Height),
						System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				}
			}
		}

		public static Image<Rgba32> BitmapToImageSharp(Bitmap bmp)
		{
			using (var ms = new MemoryStream())
			{
				bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
				ms.Position = 0;
				return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
			}
		}

		// Image<Rgba32> overloads — used by scraper code that has already been migrated to ImageSharp types.

		public static Image<Rgba32> ConvertToGrayscale(Image<Rgba32> image)
		{
			using (var bmp = ImageSharpToBitmap(image))
			using (var result = ConvertToGrayscale(bmp))
				return BitmapToImageSharp(result);
		}

		public static void SetContrast(double contrast, ref Image<Rgba32> image)
		{
			var bmp = ImageSharpToBitmap(image);
			try
			{
				SetContrast(contrast, ref bmp);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static void SetGamma(double red, double green, double blue, ref Image<Rgba32> image)
		{
			// SetGamma now modifies the bitmap in place via LockBits — no ref swap, single
			// allocation, single disposal.
			Bitmap bmp = ImageSharpToBitmap(image);
			try
			{
				SetGamma(red, green, blue, ref bmp);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static void SetInvert(ref Image<Rgba32> image)
		{
			var bmp = ImageSharpToBitmap(image);
			try
			{
				SetInvert(ref bmp);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static void SetThreshold(int threshold, ref Image<Rgba32> image)
		{
			var bmp = ImageSharpToBitmap(image);
			try
			{
				SetThreshold(threshold, ref bmp);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static void SetBrightness(int brightness, ref Image<Rgba32> image)
		{
			var bmp = ImageSharpToBitmap(image);
			try
			{
				SetBrightness(brightness, ref bmp);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static void FilterColors(ref Image<Rgba32> image, IntRange red, IntRange green, IntRange blue)
		{
			var bmp = ImageSharpToBitmap(image);
			try
			{
				FilterColors(ref bmp, red, green, blue);
				image = BitmapToImageSharp(bmp);
			}
			finally { bmp.Dispose(); }
		}

		public static Rgba32 GetAverageColor(Image<Rgba32> image)
		{
			using (var bmp = ImageSharpToBitmap(image))
			{
				var c = GetAverageColor(bmp);
				return new Rgba32(c.R, c.G, c.B, c.A);
			}
		}

		public static Rgba32 ClosestColor(List<Rgba32> colors, Rgba32 target)
		{
			var gdiColors = colors.Select(c => DrawingColor.FromArgb(c.A, c.R, c.G, c.B)).ToList();
			var gdiTarget = DrawingColor.FromArgb(target.A, target.R, target.G, target.B);
			var result = ClosestColor(gdiColors, gdiTarget);
			return new Rgba32(result.R, result.G, result.B, result.A);
		}

		public static bool CompareColors(Rgba32 a, Rgba32 b) =>
			CompareColors(DrawingColor.FromArgb(a.A, a.R, a.G, a.B), DrawingColor.FromArgb(b.A, b.R, b.G, b.B));

		public static Image<Rgba32> CopyBitmap(Image<Rgba32> source, SixLabors.ImageSharp.Rectangle region)
		{
			using (var bmp = ImageSharpToBitmap(source))
			using (var cropped = CopyBitmap(bmp, new DrawingRectangle(region.X, region.Y, region.Width, region.Height)))
				return BitmapToImageSharp(cropped);
		}

		public static Image<Rgba32> ResizeImage(Image<Rgba32> image, int width, int height)
		{
			using (var bmp = ImageSharpToBitmap(image))
			using (var result = ResizeImage(bmp, width, height))
				return BitmapToImageSharp(result);
		}

		public static Image<Rgba32> ScaleImage(Image<Rgba32> image, double factor)
		{
			using (var bmp = ImageSharpToBitmap(image))
			using (var result = ScaleImage(bmp, factor))
				return BitmapToImageSharp(result);
		}

		public static Image<Rgba32> KirschEdgeDetect(Image<Rgba32> source)
		{
			using (var bmp = ImageSharpToBitmap(source))
			using (var result = KirschEdgeDetect(bmp))
				return BitmapToImageSharp(result);
		}

		public static Image<Rgba32> PreProcessImage(Image<Rgba32> image)
		{
			using (var bmp = ImageSharpToBitmap(image))
			using (var result = PreProcessImage(bmp))
				return BitmapToImageSharp(result);
		}

		public static bool CompareBitmapsFast(Image<Rgba32> img1, Image<Rgba32> img2)
		{
			using (var b1 = ImageSharpToBitmap(img1))
			using (var b2 = ImageSharpToBitmap(img2))
				return CompareBitmapsFast(b1, b2);
		}

		#endregion ImageSharp Bridge

		public static string ConvertToGood(this string text)
		{
            text = text.ToLower();
            var pascal = CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase(text);
            return Regex.Replace(pascal, @"[\W]", string.Empty);
        }

    }

}