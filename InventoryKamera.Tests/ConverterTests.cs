using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using InventoryKamera.Avalonia.Converters;
using NLog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace InventoryKamera.Tests
{
    /// <summary>
    /// Unit tests for the value converters in InventoryKamera.Avalonia.Converters.
    /// All converters are pure functions — no Avalonia runtime required.
    /// </summary>
    public class ConverterTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // ValidationIconConverter
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ValidationIcon_True_Returns_Checkmark()
        {
            var c = new ValidationIconConverter();
            Assert.Equal("✓", c.Convert(true, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ValidationIcon_False_Returns_Cross()
        {
            var c = new ValidationIconConverter();
            Assert.Equal("✗", c.Convert(false, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ValidationIcon_Null_Returns_Empty()
        {
            var c = new ValidationIconConverter();
            Assert.Equal(string.Empty, c.Convert(null, typeof(string), null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ValidationIcon_ConvertBack_Throws()
        {
            var c = new ValidationIconConverter();
            Assert.Throws<NotSupportedException>(() =>
                c.ConvertBack("✓", typeof(bool), null, CultureInfo.InvariantCulture));
        }

        // ─────────────────────────────────────────────────────────────────────
        // RelativeTimeConverter
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void RelativeTime_DateTime_Returns_HHmmss()
        {
            var c = new RelativeTimeConverter();
            // Use a UTC time with a known local representation
            var utcTime = new DateTime(2026, 4, 29, 12, 34, 56, DateTimeKind.Utc);
            var result = (string)c.Convert(utcTime, typeof(string), null, CultureInfo.InvariantCulture);

            // Verify format is HH:mm:ss (6 digits separated by colons)
            Assert.Matches(@"^\d{2}:\d{2}:\d{2}$", result);
        }

        [Fact]
        public void RelativeTime_LocalTime_Returns_LocalHHmmss()
        {
            var c = new RelativeTimeConverter();
            var localTime = new DateTime(2026, 4, 29, 8, 5, 3, DateTimeKind.Local);
            var result = (string)c.Convert(localTime, typeof(string), null, CultureInfo.InvariantCulture);

            Assert.Equal("08:05:03", result);
        }

        [Fact]
        public void RelativeTime_Null_Returns_Empty()
        {
            var c = new RelativeTimeConverter();
            var result = (string)c.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void RelativeTime_ConvertBack_Throws()
        {
            var c = new RelativeTimeConverter();
            Assert.Throws<NotSupportedException>(() =>
                c.ConvertBack("12:00:00", typeof(DateTime), null, CultureInfo.InvariantCulture));
        }

        // ─────────────────────────────────────────────────────────────────────
        // ProgressFractionConverter
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ProgressFraction_Returns_Correct_Fraction()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 25, 100 };
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.25, result, precision: 10);
        }

        [Fact]
        public void ProgressFraction_ZeroTotal_Returns_Zero()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 10, 0 };
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void ProgressFraction_NegativeTotal_Returns_Zero()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 10, -5 };
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        [Fact]
        public void ProgressFraction_ScannedGreaterThanTotal_Clamps_To_One()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 110, 100 };
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(1.0, result);
        }

        [Fact]
        public void ProgressFraction_FullProgress_Returns_One()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 100, 100 };
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(1.0, result);
        }

        [Fact]
        public void ProgressFraction_NullValues_Returns_Zero()
        {
            var c = new ProgressFractionConverter();
            var result = (double)c.Convert(null, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.0, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // LogLevelToBrushConverter — tested via the internal ColorForLevel()
        // helper which is pure (no Avalonia UI thread required).
        // The Convert() method itself allocates SolidColorBrush and requires the
        // Avalonia UI thread; that path is exercised by the XAML compiler in build.
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void LogLevelToBrush_Info_Returns_TextPrimary_Colour()
        {
            Assert.Equal("#ECE5D8", LogLevelToBrushConverter.ColorForLevel(LogLevel.Info));
        }

        [Fact]
        public void LogLevelToBrush_Warn_Returns_Warning_Colour()
        {
            Assert.Equal("#F9B959", LogLevelToBrushConverter.ColorForLevel(LogLevel.Warn));
        }

        [Fact]
        public void LogLevelToBrush_Error_Returns_Error_Colour()
        {
            Assert.Equal("#E74C3C", LogLevelToBrushConverter.ColorForLevel(LogLevel.Error));
        }

        [Fact]
        public void LogLevelToBrush_Fatal_Returns_Same_As_Error_Colour()
        {
            Assert.Equal(
                LogLevelToBrushConverter.ColorForLevel(LogLevel.Error),
                LogLevelToBrushConverter.ColorForLevel(LogLevel.Fatal));
        }

        [Fact]
        public void LogLevelToBrush_Debug_And_Warn_Return_Different_Colours()
        {
            Assert.NotEqual(
                LogLevelToBrushConverter.ColorForLevel(LogLevel.Debug),
                LogLevelToBrushConverter.ColorForLevel(LogLevel.Warn));
        }

        [Fact]
        public void LogLevelToBrush_Null_Returns_DefaultSecondary_Colour()
        {
            Assert.Equal("#A69F92", LogLevelToBrushConverter.ColorForLevel(null));
        }

        [Fact]
        public void LogLevelToBrush_ConvertBack_Throws()
        {
            var c = new LogLevelToBrushConverter();
            Assert.Throws<NotSupportedException>(() =>
                c.ConvertBack(null, typeof(LogLevel), null, CultureInfo.InvariantCulture));
        }

        // ─────────────────────────────────────────────────────────────────────
        // PngBytesToBitmapConverter — null/empty guard
        // (Actual PNG decode tested separately; Avalonia.Headless needed for bitmap)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void PngBytesToBitmap_Null_Returns_Null()
        {
            var c = new PngBytesToBitmapConverter();
            var result = c.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void PngBytesToBitmap_EmptyArray_Returns_Null()
        {
            var c = new PngBytesToBitmapConverter();
            var result = c.Convert(Array.Empty<byte>(), typeof(object), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void PngBytesToBitmap_NonByteArray_Returns_Null()
        {
            var c = new PngBytesToBitmapConverter();
            var result = c.Convert("not bytes", typeof(object), null, CultureInfo.InvariantCulture);
            Assert.Null(result);
        }

        [Fact]
        public void PngBytesToBitmap_ConvertBack_Throws()
        {
            var c = new PngBytesToBitmapConverter();
            Assert.Throws<NotSupportedException>(() =>
                c.ConvertBack(null, typeof(byte[]), null, CultureInfo.InvariantCulture));
        }

        // ─────────────────────────────────────────────────────────────────────
        // ProgressFractionConverter — additional ToDouble branches
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void ProgressFraction_Double_Values_Work()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 0.5, 1.0 };  // double inputs
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.5, result, precision: 10);
        }

        [Fact]
        public void ProgressFraction_Float_Values_Work()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 0.25f, 1.0f };  // float inputs
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.25, result, precision: 5);
        }

        [Fact]
        public void ProgressFraction_Long_Values_Work()
        {
            var c = new ProgressFractionConverter();
            var values = new List<object> { 50L, 200L };  // long inputs
            var result = (double)c.Convert(values, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.Equal(0.25, result, precision: 10);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Headless converter tests — require Avalonia UI context for brush/bitmap.
    // ─────────────────────────────────────────────────────────────────────────

    [Collection("AvaloniaHeadless")]
    public class ConverterHeadlessTests
    {
        private readonly HeadlessUnitTestSession _session;

        public ConverterHeadlessTests(AvaloniaHeadlessFixture fixture)
        {
            _session = fixture.Session;
        }

        private void Run(Action action)
        {
            _session.Dispatch(() =>
            {
                action();
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None)
                // A deadlock with the headless UI thread fails the test instead of hanging the run
                .WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }

        // LogLevelToBrushConverter.Convert() requires SolidColorBrush allocation on UI thread

        [Fact]
        public void LogLevelToBrush_Convert_Info_Returns_Brush()
        {
            IBrush result = null;
            Run(() =>
            {
                var c = new LogLevelToBrushConverter();
                result = (IBrush)c.Convert(LogLevel.Info, typeof(IBrush), null, CultureInfo.InvariantCulture);
            });
            Assert.NotNull(result);
        }

        [Fact]
        public void LogLevelToBrush_Convert_Warn_Returns_Brush()
        {
            IBrush result = null;
            Run(() =>
            {
                var c = new LogLevelToBrushConverter();
                result = (IBrush)c.Convert(LogLevel.Warn, typeof(IBrush), null, CultureInfo.InvariantCulture);
            });
            Assert.NotNull(result);
        }

        [Fact]
        public void LogLevelToBrush_Convert_Error_Returns_Brush()
        {
            IBrush result = null;
            Run(() =>
            {
                var c = new LogLevelToBrushConverter();
                result = (IBrush)c.Convert(LogLevel.Error, typeof(IBrush), null, CultureInfo.InvariantCulture);
            });
            Assert.NotNull(result);
        }

        [Fact]
        public void LogLevelToBrush_Convert_Debug_Returns_DefaultBrush()
        {
            IBrush resultDebug = null;
            IBrush resultNull = null;

            Run(() =>
            {
                var c = new LogLevelToBrushConverter();
                resultDebug = (IBrush)c.Convert(LogLevel.Debug, typeof(IBrush), null, CultureInfo.InvariantCulture);
                resultNull  = (IBrush)c.Convert(null,           typeof(IBrush), null, CultureInfo.InvariantCulture);
            });

            Assert.NotNull(resultDebug);
            Assert.NotNull(resultNull);
        }

        // PngBytesToBitmapConverter — actual decode path with valid PNG bytes

        [Fact]
        public void PngBytesToBitmap_ValidPng_Returns_Bitmap()
        {
            object result = null;

            Run(() =>
            {
                // Create a small PNG in-memory.
                using var img = new Image<Rgba32>(4, 4);
                using var ms = new MemoryStream();
                img.Save(ms, new PngEncoder());
                var pngBytes = ms.ToArray();

                var c = new PngBytesToBitmapConverter();
                result = c.Convert(pngBytes, typeof(object), null, CultureInfo.InvariantCulture);
            });

            Assert.NotNull(result);
        }

        // ObservableCollectionLogTarget — on-UI-thread path (CheckAccess() == true)

        [Fact]
        public async Task ObservableCollectionLogTarget_OnUIThread_AppendsEntry()
        {
            var collection = new System.Collections.ObjectModel.ObservableCollection<InventoryKamera.Avalonia.Services.ConsoleEntry>();
            var target = new InventoryKamera.Avalonia.Services.ObservableCollectionLogTarget(collection)
            {
                Name = "UIThreadTarget_" + Guid.NewGuid()
            };

            var loggerName = "UIThreadLogger_" + Guid.NewGuid().ToString("N");
            var cfg = new NLog.Config.LoggingConfiguration();
            cfg.AddTarget(target);
            cfg.AddRuleForAllLevels(target, loggerName);
            LogManager.Configuration = cfg;
            LogManager.ReconfigExistingLoggers();

            var logger = LogManager.GetLogger(loggerName);

            // Log from inside the session.Dispatch callback — this IS the UI thread.
            await _session.Dispatch(() =>
            {
                logger.Info("On-UI-thread entry");
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None);

            Assert.NotEmpty(collection);
        }

        // ObservableCollectionLogTarget — MaxEntries trimming (line 44)

        [Fact]
        public async Task ObservableCollectionLogTarget_TrimsAtMaxEntries()
        {
            var collection = new System.Collections.ObjectModel.ObservableCollection<InventoryKamera.Avalonia.Services.ConsoleEntry>();
            var target = new InventoryKamera.Avalonia.Services.ObservableCollectionLogTarget(collection)
            {
                Name    = "TrimTarget_" + Guid.NewGuid(),
                MaxEntries = 2   // Very small limit to trigger trimming.
            };

            var loggerName = "TrimLogger_" + Guid.NewGuid().ToString("N");
            var cfg = new NLog.Config.LoggingConfiguration();
            cfg.AddTarget(target);
            cfg.AddRuleForAllLevels(target, loggerName);
            LogManager.Configuration = cfg;
            LogManager.ReconfigExistingLoggers();

            var logger = LogManager.GetLogger(loggerName);

            // Write 3 entries on the UI thread — third should trigger trimming.
            await _session.Dispatch(() =>
            {
                logger.Info("Entry 1");
                logger.Info("Entry 2");
                logger.Info("Entry 3"); // triggers RemoveAt(0), then Add
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None);

            // Collection should have at most MaxEntries items.
            Assert.True(collection.Count <= 2);
        }

        // ObservableCollectionLogTarget — Dispatcher.Post path (off-UI-thread)
        // Use NLog's full logging pipeline with a dedicated config so Write() is called
        // from a background thread, exercising the Dispatcher.Post branch.

        [Fact]
        public async Task ObservableCollectionLogTarget_PostPath_AppendsEntry()
        {
            var collection = new System.Collections.ObjectModel.ObservableCollection<InventoryKamera.Avalonia.Services.ConsoleEntry>();
            var target = new InventoryKamera.Avalonia.Services.ObservableCollectionLogTarget(collection)
            {
                Name = "PostPathTarget_" + Guid.NewGuid()
            };

            // Configure NLog with this target so the logger chain works.
            var bgLoggerName = "BgLoggerTest_" + Guid.NewGuid().ToString("N");
            var cfg = new NLog.Config.LoggingConfiguration();
            cfg.AddTarget(target);
            cfg.AddRuleForAllLevels(target, bgLoggerName);
            LogManager.Configuration = cfg;
            LogManager.ReconfigExistingLoggers();

            var bgLogger = LogManager.GetLogger(bgLoggerName);

            // Log from a background thread — CheckAccess() will be false there,
            // so ObservableCollectionLogTarget will Post to the UI thread.
            var logged = new System.Threading.ManualResetEventSlim(false);
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                bgLogger.Warn("Background post test");
                logged.Set();
            });
            logged.Wait(TimeSpan.FromSeconds(5));

            // Flush: the Post should arrive on the UI thread now.
            await _session.Dispatch(() =>
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Background);
            }, CancellationToken.None);

            Assert.NotEmpty(collection);
        }
    }
}
