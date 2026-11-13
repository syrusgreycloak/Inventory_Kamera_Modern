using InventoryKamera.Configuration;
using InventoryKamera.Infrastructure.Scraping; // GenshinProcesor
using InventoryKamera.Infrastructure.Windows; // Navigation
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using DrawingColor = System.Drawing.Color;
using DrawingRect  = System.Drawing.Rectangle;
using DrawingSize  = System.Drawing.Size;
using ISRect       = SixLabors.ImageSharp.Rectangle;
using ISSize       = SixLabors.ImageSharp.Size;

namespace InventoryKamera
{

    public enum InventoryPage
    {
        Weapons,
        Artifacts,
        CharacterDevelopmentItems,
        Food,
        Materials,
        Gadget,
        Quest,
        PreciousItems,
        Furnishings,
    }

    public enum Quality
    {
        INVALID,
        ONESTAR,
        TWOSTAR,
        THREESTAR,
        FOURSTAR,
        FIVESTAR
    }

    internal static class InventoryPageExtension
    {
        public static string ToString(this InventoryPage page)
        {
            switch (page)
            {
                case InventoryPage.CharacterDevelopmentItems:
                    return "CharDevItems";
                default:
                    return page.ToString();
            }
        }
    }

    internal class InventoryScraper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected InventoryPage inventoryPage;

        protected bool SortByLevel = false;

        protected readonly List<InventoryPage> materialPages;

        private List<ISRect> prevRect;
        private int prevColumn = 0;
        private int prevRow = 0;

        protected IScreenCapture _screenCapture;
        protected IOcrEngine _ocrEngine;
        protected IImageProcessor _imageProcessor;
        protected IUserInterface _userInterface;
        protected IGameDataService _gameDataService;
        protected IInputSimulator _inputSimulator;
        protected IScanProfileService _scanProfile;
        // Settings snapshot — null on WinForms path (uses Properties.Settings.Default fallback)
        protected ScanSettings _settings;

        protected InventoryScraper(IScreenCapture screenCapture, IOcrEngine ocrEngine, IImageProcessor imageProcessor, IUserInterface userInterface, IGameDataService gameDataService, IInputSimulator inputSimulator, IScanProfileService scanProfile, ScanSettings settings = null)
            : this()
        {
            _screenCapture = screenCapture;
            _ocrEngine = ocrEngine;
            _imageProcessor = imageProcessor;
            _userInterface = userInterface;
            _gameDataService = gameDataService;
            _inputSimulator = inputSimulator;
            _scanProfile = scanProfile;
            _settings = settings;
        }

        public InventoryScraper()
        {
            materialPages = new List<InventoryPage>();

            materialPages.AddRange(Enum.GetValues(typeof(InventoryPage)).Cast<InventoryPage>());
            materialPages.Remove(InventoryPage.Weapons);
            materialPages.Remove(InventoryPage.Artifacts);
        }

        internal bool StopScanning { get; set; }

        /// <summary>
        /// Gets the item card on the right side of the in-game screen
        /// </summary>
        /// <returns>An image of the in-game item card</returns>
        internal Image<Rgba32> GetItemCard()
        {
            using (var window = _screenCapture.CaptureWindow())
            {
                var cardRectangle = new ISRect(
                    x: (int)(window.Width * 0.6807),
                    y: (int)(window.Height * (_screenCapture.IsNormal ? 0.1102 : 0.0989)),
                    width: (int)(window.Width * 0.2573),
                    height: (int)(window.Height * (_screenCapture.IsNormal ? 0.7787 : 0.8022)));

                return _imageProcessor.Crop(window, cardRectangle);
            }
        }

        /// <summary>
        /// Parses the item name from an item card's nameplate
        /// </summary>
        /// <param name="nameplate">The item nameplate to parse</param>
        /// <returns>String parsed from nameplate</returns>
        internal string ScanItemName(Image<Rgba32> nameplate)
        {
            // The caller owns `nameplate` — do NOT dispose it. SetGamma/SetGrayscale/SetInvert
            // each return a new Image<Rgba32>; we own those intermediates and dispose them here.
            // Disposing the input parameter (the previous behavior) caused later GetQuality calls
            // on the same shared list reference to throw ObjectDisposedException on the worker.
            var gamma = _imageProcessor.SetGamma(nameplate, 0.2, 0.2, 0.2);
            try
            {
                var gray = _imageProcessor.SetGrayscale(gamma);
                try
                {
                    var inverted = _imageProcessor.SetInvert(gray);
                    try
                    {
                        return Regex.Replace(
                            _ocrEngine.AnalyzeText(inverted, (PageSegmentationMode)(int)TesseractOCR.Enums.PageSegMode.SingleBlock).ToLower(),
                            @"[\W]", string.Empty);
                    }
                    finally { if (!ReferenceEquals(inverted, gray)) inverted.Dispose(); }
                }
                finally { if (!ReferenceEquals(gray, gamma)) gray.Dispose(); }
            }
            finally { if (!ReferenceEquals(gamma, nameplate)) gamma.Dispose(); }
        }

        /// <summary>
        /// Parses the number of items from for the current inventory page.
        /// </summary>
        /// <remarks>Note: This is primarily for the Weapons, Artifacts, and Furnishing inventories since they have
        /// specific inventory item counts and capacities.
        /// All other inventory pages have a "shared" inventory item counts and capacities.</remarks>
        /// <param name="inventoryPage">The current inventory page</param>
        /// <returns>The number of unique items for the current inventory</returns>
        /// <exception cref="FormatException">The inventory item count could not be found where it is expected</exception>
        internal int ScanItemCount()
        {
            //Find weapon count
            var region = new RECT(
                Left:   (int)(1030 / 1280.0 * _screenCapture.GetWidth()),
                Top:    (int)(20 / 720.0 * _screenCapture.GetHeight()),
                Right:  (int)((1030 + 175) / 1280.0 * _screenCapture.GetWidth()),
                Bottom: (int)((20 + 25) / 720.0 * _screenCapture.GetHeight()));

            using (Image<Rgba32> countBitmap = _screenCapture.CaptureRegion(region))
            {
                _userInterface.SetNavigation_Image(countBitmap);

                Image<Rgba32> n = _imageProcessor.SetGrayscale(countBitmap);
                var oldN = n;
                n = _imageProcessor.SetContrast(n, 60.0);
                if (!ReferenceEquals(oldN, n)) oldN.Dispose();
                n = _imageProcessor.SetInvert(n);

                string text = _ocrEngine.AnalyzeText(n).Trim();
                n.Dispose();

                // Remove any non-numeric and '/' characters
                text = Regex.Replace(text, @"[^0-9/]", string.Empty);

                if (string.IsNullOrWhiteSpace(text) || (_settings != null ? _settings.LogScreenshots : false))
                {
                    SaveInventoryBitmap(countBitmap, "ItemCount.png");
                    using (var wnd = _screenCapture.CaptureWindow())
                        SaveInventoryBitmap(wnd, $"InventoryWindow_{_screenCapture.GetWidth()}x{_screenCapture.GetHeight()}.png");
                    if (string.IsNullOrWhiteSpace(text)) throw new FormatException($"Unable to locate {inventoryPage} item count.");
                }

                int count;
                string pageCapacity;
                switch (inventoryPage)
                {
                    case InventoryPage.Artifacts:
                        pageCapacity = "1800";
                        break;
                    default:
                        pageCapacity = "2000";
                        break;
                }

                if (Regex.IsMatch(text, "/")) // Check for slash
                {
                    count = int.Parse(text.Split('/')[0]);
                }
                else if (Regex.Matches(text, pageCapacity).Count == 1) // Remove the inventory capacity from number
                {
                    text = text.Replace(pageCapacity, string.Empty);
                    count = int.Parse(text);
                }
                else // Extreme worst case
                {
                    count = 2000;
                    Logger.Debug("Defaulted to 2000 for inventory page capacity");
                }

                return count;
            }
        }

        /// <summary>
        /// Parses the sorting method for the current inventory
        /// </summary>
        /// <param name="inventoryPage">The current inventory page</param>
        /// <returns>The inventory page's current sorting method</returns>
        internal string CurrentSortingMethod()
        {
            RECT region;
            switch (inventoryPage)
            {
                case InventoryPage.Weapons:
                    region = new RECT(
                        Left:   (int)(100.0 / 1280.0 * _screenCapture.GetWidth()),
                        Top:    (int)(660.0 / 720.0 * _screenCapture.GetHeight()),
                        Right:  (int)((100.0 + 175.0) / 1280.0 * _screenCapture.GetWidth()),
                        Bottom: (int)((660.0 + 40.0) / 720.0 * _screenCapture.GetHeight()));
                    break;
                case InventoryPage.Artifacts:
                    // TODO: Update this
                    region = new RECT(
                        Left:   (int)(140.0 / 1280.0 * _screenCapture.GetWidth()),
                        Top:    (int)(660.0 / 720.0 * _screenCapture.GetHeight()),
                        Right:  (int)((140.0 + 175.0) / 1280.0 * _screenCapture.GetWidth()),
                        Bottom: (int)((660.0 + 40.0) / 720.0 * _screenCapture.GetHeight()));
                    break;
                default:
                    throw new NotImplementedException($"{inventoryPage} cannot be sorted");
            }

            using (var bm = _screenCapture.CaptureRegion(region))
            {
                var g = _imageProcessor.SetGrayscale(bm);
                var mode = _ocrEngine.AnalyzeText(g).Trim().ToLower();
                return mode.Contains("level") ? "level" : mode.Contains("quality") ? "quality" : null;
            }
        }

        internal (List<ISRect> rectangles, int cols, int rows) ProcessScreenshot(Image<Rgba32> screenshot, double weight = 0)
        {
            // Size of an item card is the same in 16:10 and 16:9. Also accounts for character icon and resolution size.
            double base_aspect_width = 1280.0;
            double base_aspect_height = 720.0;
            var icon = new ISRect(
                x: 0,
                y: 0,
                width: (int)(screenshot.Width * 0.0651),
                height: (int)(screenshot.Height * (_screenCapture.IsNormal ? 0.1417: 0.1289)));

            if (_screenCapture.GetAspectRatio() == new ISSize(8, 5))
            {
                base_aspect_height = 800.0;
            }

            // Filter for relative size of items in inventory, give or take a few pixels
            int iconMinHeight = icon.Height - ((int)(icon.Height * 0.15));
            int iconMaxHeight = icon.Height + ((int)(icon.Height * 0.15));
            int iconMinWidth = icon.Width - ((int)(icon.Width * 0.15));
            int iconMaxWidth = icon.Width + ((int)(icon.Width * 0.15));
            {
                // Image pre-processing — work on a separate clone so the caller's bitmap stays valid for retries
                Image<Rgba32> processedForBlobs;
                using (var kirschEdges = _imageProcessor.KirschEdgeDetect(screenshot))
                using (var grayscale = _imageProcessor.SetGrayscale(kirschEdges))
                {
                    processedForBlobs = grayscale.Clone();
                    processedForBlobs = _imageProcessor.SetThreshold(processedForBlobs, 75);
                }
                // Note: Processing won't always detect all item rectangles on screen. Since the
                // background isn't a solid color it's a bit trickier to filter out.

                // Don't save overlapping blobs
                List<ISRect> rectangles = new List<ISRect>();
                List<ISRect> blobRects;
                using (processedForBlobs)
                    blobRects = FindBlobs(processedForBlobs,
                        (int)(iconMinWidth * (1 - weight)),
                        (int)(iconMaxWidth * (1 + weight)),
                        (int)(iconMinHeight * (1 - weight)),
                        (int)(iconMaxHeight * (1 + weight)));

                if (blobRects.Count == 0)
                    return (new List<ISRect>(), 0, 0);

                int minWidth = blobRects[0].Width;
                int minHeight = blobRects[0].Height;
                foreach (var rect in blobRects)
                {
                    bool add = true;
                    foreach (var item in rectangles)
                    {
                        ISRect r1 = rect;
                        ISRect r2 = item;
                        ISRect intersect = ISRect.Intersect(r1, r2);
                        if (intersect.Width > r1.Width * .1)
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        minWidth = Math.Min(minWidth, rect.Width);
                        minHeight = Math.Min(minHeight, rect.Height);
                        rectangles.Add(rect);
                    }
                }

                // Determine X and Y coordinates for columns and rows, respectively
                var colCoords = new List<int>();
                var rowCoords = new List<int>();

                foreach (var item in rectangles)
                {
                    bool addX = true;
                    bool addY = true;
                    foreach (var x in colCoords)
                    {
                        var xC = item.Center().X;
                        if (x - 75 / base_aspect_width * screenshot.Width <= xC && xC <= x + 75 / base_aspect_width * screenshot.Width)
                        {
                            addX = false;
                            break;
                        }
                    }
                    foreach (var y in rowCoords)
                    {
                        var yC = item.Center().Y;
                        if (y - 100 / base_aspect_height * screenshot.Height <= yC && yC <= y + 100 / base_aspect_height * screenshot.Height)
                        {
                            addY = false;
                            break;
                        }
                    }
                    if (addX)
                    {
                        colCoords.Add(item.Center().X);
                    }
                    if (addY)
                    {
                        rowCoords.Add(item.Center().Y);
                    }
                }

                // Going to use X,Y coordinate pairings to build rectangles around. Items that might have been missed
                // This is quite accurate and algorithmically puts rectangles over all items on the screen that were missed.
                // The center of each of these rectangles should be a good enough spot to click.
                rectangles.Clear();
                colCoords.Sort();
                rowCoords.Sort();

                colCoords.RemoveAll(col => col > screenshot.Width * 0.65);

                foreach (var row in rowCoords)
                {
                    foreach (var col in colCoords)
                    {
                        int x = (int)(col - (minWidth * .5));
                        int y = (int)(row - (minHeight * .5));

                        rectangles.Add(new ISRect(x, y, minWidth, minHeight));
                    }
                }

                // Remove some rectangles that somehow overlap each other. Don't think this happens
                // but it doesn't hurt to double check.
                for (int i = 0; i < rectangles.Count - 1; i++)
                {
                    for (int j = i + 1; j < rectangles.Count; j++)
                    {
                        ISRect r1 = rectangles[i];
                        ISRect r2 = rectangles[j];
                        ISRect intersect = ISRect.Intersect(r1, r2);
                        if (intersect.Width > r1.Width * .2)
                        {
                            rectangles.RemoveAt(j);
                        }
                    }
                }

                // Sort by row then by column within each row
                rectangles = rectangles.OrderBy(r => r.Top).ThenBy(r => r.Left).ToList();

                var avgWidth = rectangles.Average(r => r.Width);
                var avgHeight = rectangles.Average(r => r.Height);

                rectangles.ForEach(r =>
                {
                    r.Width = (int)avgWidth;
                    r.Height = (int)avgHeight;
                });

                return (rectangles, colCoords.Count, rowCoords.Count);
            }
        }

        private static List<ISRect> FindBlobs(Image<Rgba32> binaryImage, int minWidth, int maxWidth, int minHeight, int maxHeight)
        {
            int width = binaryImage.Width;
            int height = binaryImage.Height;
            bool[,] visited = new bool[width, height];
            var results = new List<ISRect>();

            // Extract pixel data via ImageSharp
            byte[] pixels = new byte[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = binaryImage[x, y].R; // grayscale: R == G == B

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (visited[x, y]) continue;
                    bool isWhite = pixels[y * width + x] > 128;
                    if (!isWhite) continue;

                    var queue = new Queue<(int px, int py)>();
                    queue.Enqueue((x, y));
                    visited[x, y] = true;
                    int minX = x, maxX = x, minY = y, maxY = y;

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();
                        if (cx < minX) minX = cx; if (cx > maxX) maxX = cx;
                        if (cy < minY) minY = cy; if (cy > maxY) maxY = cy;

                        int[] dxs = { -1, 1, 0, 0 };
                        int[] dys = { 0, 0, -1, 1 };
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + dxs[d], ny = cy + dys[d];
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height || visited[nx, ny]) continue;
                            if (pixels[ny * width + nx] > 128)
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }

                    int blobW = maxX - minX + 1;
                    int blobH = maxY - minY + 1;
                    if (blobW >= minWidth && blobW <= maxWidth && blobH >= minHeight && blobH <= maxHeight)
                        results.Add(new ISRect(minX, minY, blobW, blobH));
                }
            }

            return results;
        }

        internal (List<ISRect> rectangles, int cols, int rows) GetPageOfItems(int pageNum, bool acceptLess = false)
        {
            // Screenshot of inventory
            using (Image<Rgba32> screenshot = _screenCapture.CaptureWindow())
            {
                // Use GDI+ to mask out irrelevant regions (UI chrome) before blob detection
                Image<Rgba32> processedScreenshot;
                using (var bmp = GenshinProcesor.ImageSharpToBitmap(screenshot))
                {
                    using (var processed = new Bitmap(bmp))
                    using (Graphics g = Graphics.FromImage(processed))
                    using (var brush = new SolidBrush(DrawingColor.Black))
                    {
                        // Fill Top region
                        switch (inventoryPage)
                        {
                            case InventoryPage.Artifacts:
                                g.FillRectangle(brush, 0, 0, processed.Width, (int)(processed.Height * 0.143));
                                break;
                            default:
                                g.FillRectangle(brush, 0, 0, processed.Width, (int)(processed.Height * 0.09));
                                break;
                        }

                        // Fill Left region
                        g.FillRectangle(brush, 0, 0, (int)(processed.Width * 0.05), processed.Height);

                        // Fill Right region
                        g.FillRectangle(brush, (int)(processed.Width * 0.7), 0, processed.Width, processed.Height);

                        // Fill Bottom Region
                        g.FillRectangle(brush, 0, (int)(processed.Height * 0.9), processed.Width, processed.Height);

                        processedScreenshot = GenshinProcesor.BitmapToImageSharp(processed);
                    }
                }

                try
                {
                    List<ISRect> rectangles;
                    int cols, rows, itemCount, counter = 0;
                    double weight = 0;
                    int itemPerPage = inventoryPage == InventoryPage.Artifacts ? 32 : 40;
                    do
                    {
                        (rectangles, cols, rows) = ProcessScreenshot(processedScreenshot, weight);
                        itemCount = rows * cols;
                        if (itemCount != itemPerPage && !acceptLess)
                        {
                            Logger.Warn("Unable to locate full page of weapons with weight {0}", weight);
                            Logger.Warn("Detected {0} rows and {1} columns of items", rows, cols);

                            // Generate debug image with annotated rectangles
                            using (var copyBmp = GenshinProcesor.ImageSharpToBitmap(screenshot))
                            {
                                SaveInventoryBitmapGdi(copyBmp, $"{inventoryPage}Inventory{pageNum}_{cols}x{rows}.png");
                                using (Graphics g = Graphics.FromImage(copyBmp))
                                    rectangles.ForEach(r => g.DrawRectangle(new Pen(DrawingColor.Green, 2), r.X, r.Y, r.Width, r.Height));
                                SaveInventoryBitmapGdi(copyBmp, $"{inventoryPage}Inventory{pageNum}_{cols}x{rows} - weight {weight}.png");
                            }
                        }
                        else break;

                        if (itemCount <= 32)
                            weight += 0.125;
                        else
                        { weight -= 0.095; ++counter; }
                        weight = Math.Min(weight, 1);
                        rectangles = null;
                    }
                    while (itemCount != itemPerPage && weight < 1 && counter < 25);

                    if ((_settings != null ? _settings.LogScreenshots : false))
                    {
                        SaveInventoryBitmap(screenshot, $"{inventoryPage}Inventory.png");
                        if (rectangles != null)
                        {
                            using (var annotateBmp = GenshinProcesor.ImageSharpToBitmap(screenshot))
                            {
                                using (Graphics g = Graphics.FromImage(annotateBmp))
                                    rectangles.ForEach(r => g.DrawRectangle(new Pen(DrawingColor.Green, 2), r.X, r.Y, r.Width, r.Height));
                                SaveInventoryBitmapGdi(annotateBmp, $"{inventoryPage}Inventory{pageNum}_{cols}x{rows} - weight {weight}.png");
                            }
                        }
                    }
                    processedScreenshot.Dispose();

                    if (rectangles == null)
                    {
                        Logger.Warn("Could not find {0} items in inventory. Re-using previous item page.", itemPerPage);

                        return prevRect == null ?
                            throw new ArgumentNullException("Could not find first page of items!")
                            :
                            (prevRect, prevColumn, prevRow);
                    }
                    else
                    {
                        prevRect = rectangles; prevColumn = cols; prevRow = rows;
                        return (rectangles, cols, rows);
                    }

                }
                catch (Exception)
                {
                    processedScreenshot.Dispose();
                    SaveInventoryBitmap(screenshot, $"{inventoryPage}Inventory.png");
                    throw;
                }
            }

        }

        /// <summary>
        /// Determines the quality of an item based on it's nameplate
        /// </summary>
        /// <param name="nameplate">The an item card's nameplate</param>
        /// <returns>An integer representing quality from 0 - 5 (invalid - 5 star)</returns>
        internal static int GetQuality(Image<Rgba32> nameplate)
        {
            var averageColor = GenshinProcesor.GetAverageColor(nameplate);

            var fiveStar  = new Rgba32(188, 105,  50, 255);
            var fourStar  = new Rgba32(161,  86, 224, 255);
            var threeStar = new Rgba32( 81, 127, 203, 255);
            var twoStar   = new Rgba32( 42, 143, 114, 255);
            var oneStar   = new Rgba32(114, 119, 138, 255);
            var black     = new Rgba32(  0,   0,   0, 255);

            var colors = new List<Rgba32> { black, oneStar, twoStar, threeStar, fourStar, fiveStar };

            var c = GenshinProcesor.ClosestColor(colors, averageColor);

            return colors.IndexOf(c);
        }

        /// <summary>
        /// Extracts an image copy of an item card's nameplate
        /// </summary>
        /// <param name="card">Image of the item card</param>
        /// <returns>A copy of the item card's nameplate</returns>
        internal static Image<Rgba32> GetItemNameBitmap(Image<Rgba32> card)
        {
            return GenshinProcesor.CopyBitmap(card,
                new ISRect(
                    x: 0,
                    y: 0,
                    width: card.Width,
                    height: (int)(card.Height * (Navigation.IsNormal ? 0.07 : 0.06))));
        }

        /// <summary>
        /// Extracts an image copy of an item card's lock status icon
        /// </summary>
        /// <param name="card">Image of the item card</param>
        /// <returns>A copy of the item card's lock status icon</returns>
        internal static Image<Rgba32> GetLockedBitmap(Image<Rgba32> card)
        {
            return GenshinProcesor.CopyBitmap(card,
                new ISRect(
                    x: (int)(card.Width * 0.75),
                    y: (int)(card.Height * (Navigation.IsNormal ? 0.353 : 0.309)),
                    width: (int)(card.Width * 0.0955),
                    height: (int)(card.Height * (Navigation.IsNormal ? 0.055 : 0.0495))));
        }

        /// <summary>
        /// Extracts an image copy of an item card's equipped character status
        /// </summary>
        /// <param name="card">Image of the item card</param>
        /// <remarks>Note: This method is only useful for equippable items (artifacts and weapons)</remarks>
        /// <returns>A copy of the item card's equipped character status.</returns>
        internal static Image<Rgba32> GetEquippedBitmap(Image<Rgba32> card)
        {
            return GenshinProcesor.CopyBitmap(card,
                new ISRect(
                    x: (int)(card.Width * 0.15),
                    y: (int)(double)(card.Height * (double)(Navigation.IsNormal ? 0.938 : 0.943)),
                    width: card.Width,
                    height: card.Height));
        }

        internal void SaveInventoryBitmap(Image<Rgba32> image, string filename)
        {
            var path = "./logging/";

            if (materialPages.Contains(inventoryPage))
                path += "Materials/";
            else path += inventoryPage.ToString() + "/";

            image.SaveAsPng(path + filename);
        }

        /// <summary>
        /// Reports a count the OCR couldn't settle (see MaterialCountReader.ReadCount), so a wrong
        /// number stands out in the console instead of going into the export unnoticed. The caller
        /// saves the count image as {name}_Quantity.png.
        /// </summary>
        internal void FlagUncertainCount(string name, int count)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn("Uncertain count for {0}: read {1}", name, count);
            _userInterface.AddError($"Uncertain count for {name}: read {count}. Please check it in-game (image: logging, {name}_Quantity.png).");
        }

        internal void SaveInventoryBitmapGdi(Bitmap bmp, string filename)
        {
            var path = "./logging/";

            if (materialPages.Contains(inventoryPage))
                path += "Materials/";
            else path += inventoryPage.ToString() + "/";

            bmp.Save(path + filename);
        }

        /// <summary>
        /// Reads the materials shown after the last item on the weapon or artifact page: enhancement
        /// ores after the weapons, artifact EXP items (Sanctifying Essence, Sanctifying Unction) after
        /// the artifacts. Call after the page's own scan.
        /// </summary>
        /// <remarks>
        /// One card per material you have (so 0 to <paramref name="names"/>.Length cards), and the
        /// count the game shows for the page doesn't include them, so the item scan never reaches
        /// them. Scroll to the end, then read the last cards backwards (skipping empty cells) until
        /// one isn't one of <paramref name="names"/>. A weapon or artifact can't be taken for one of
        /// them: its name has to match a material name closely (95%).
        /// </remarks>
        /// <param name="names">GOOD keys of the materials that can follow the items.</param>
        /// <param name="itemCount">Items on the page (the game's count), for scrolling to the end.</param>
        internal List<Material> ScanTrailingMaterials(IReadOnlyList<string> names, int itemCount)
        {
            var log = NLog.LogManager.GetCurrentClassLogger();
            var found = new List<Material>();

            // Scroll to the end; scrolling past it does nothing. 10 notches per row, 8 columns.
            _inputSimulator.SetCursor(
                (int)(_screenCapture.GetWidth() * 0.35),
                (int)(_screenCapture.GetHeight() * 0.5));
            int rows = (int)Math.Ceiling((itemCount + names.Count) / 8.0) + 1;
            for (int i = 0; i < 10 * rows; i++)
            {
                _inputSimulator.MouseVerticalScroll(-1);
                _inputSimulator.Wait(1);
            }
            _inputSimulator.SystemWait(ScanDelay.Normal);

            var (rectangles, _, _) = GetPageOfItems(999, acceptLess: true);
            log.Debug("{0}: {1} items on the last page, looking for {2}", inventoryPage, rectangles.Count, string.Join(", ", names));

            // The boxes are a full grid, so the last row can end in empty cells; clicking one doesn't
            // change the selection. Skip cells without an item card.
            foreach (var rectangle in Enumerable.Reverse(rectangles))
            {
                if (found.Count == names.Count) break;
                if (!HasItemCard(rectangle)) continue;

                _inputSimulator.SetCursor(rectangle.Center().X, rectangle.Center().Y);
                _inputSimulator.Click();
                _inputSimulator.SystemWait(ScanDelay.SelectNextInventoryItem);

                string read;
                using (var card = GetItemCard())
                using (var nameplate = GetItemNameBitmap(card))
                    read = _gameDataService.FindClosestMaterialName(ScanItemName(nameplate), minConfidence: 95);

                string name = names.FirstOrDefault(n => string.Equals(n, read, StringComparison.OrdinalIgnoreCase));
                if (name == null)
                {
                    log.Debug("{0}: trailing materials stopped at '{1}'", inventoryPage, read ?? "(no match)");
                    break;
                }

                int quantity = MaterialScraper.ScanMaterialCount(rectangle, out Bitmap countImage, out bool confident);
                using (countImage)
                {
                    if ((_settings != null && _settings.LogScreenshots) || quantity == 0 || !confident)
                        SaveInventoryBitmapGdi(countImage, $"{name}_Quantity.png");
                    using (var preview = GenshinProcesor.BitmapToImageSharp(countImage))
                        _userInterface.SetMaterial(preview, name, quantity);
                }

                if (quantity == 0)
                {
                    _userInterface.AddError($"Failed to parse quantity for {name}");
                    continue;
                }

                if (!confident) FlagUncertainCount(name, quantity);
                log.Info("{0}: {1}", name, quantity);
                found.Add(new Material(name, quantity));
            }

            return found;
        }

        /// <summary>True if an item card fills this grid cell: its light label strip is there.</summary>
        private bool HasItemCard(ISRect cell)
        {
            double scale = _screenCapture.GetHeight() / 1080.0;
            int top = cell.Bottom - (int)(45 * scale);
            using (var strip = _screenCapture.CaptureRegion(cell.X, top, cell.Width, (int)(75 * scale)))
            {
                return MaterialCountReader.FindLabelBand(strip,
                    minRows: (int)(18 * scale),
                    maxRows: (int)Math.Ceiling(MaterialCountReader.LabelHeight1080 * scale)) != null;
            }
        }

    }
}
