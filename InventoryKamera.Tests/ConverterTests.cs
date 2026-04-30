using System;
using System.Collections.Generic;
using System.Globalization;
using InventoryKamera.Avalonia.Converters;
using NLog;
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
    }
}
