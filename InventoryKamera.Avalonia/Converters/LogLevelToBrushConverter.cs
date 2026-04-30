using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using NLog;

namespace InventoryKamera.Avalonia.Converters
{
    /// <summary>
    /// Converts an NLog <see cref="LogLevel"/> to an <see cref="IBrush"/> using the
    /// GenshinTheme palette colours.
    ///
    /// Mapping:
    ///   Info                            → #ECE5D8  (TextPrimary — informational messages)
    ///   Warn                            → #F9B959  (Warning)
    ///   Error / Fatal                   → #E74C3C  (Error)
    ///   Debug / Trace / everything else → #A69F92  (TextSecondary)
    ///
    /// Brushes are allocated lazily per-instance on first Convert() call so that the
    /// type can be instantiated outside the Avalonia UI thread (e.g. in unit tests).
    /// </summary>
    public class LogLevelToBrushConverter : IValueConverter
    {
        // Lazily-initialised per-instance brushes — allocated on first Convert() call
        // which happens on the Avalonia UI thread at runtime.  In unit tests the tests
        // that call Convert() must provide an Avalonia app context, or the tests verify
        // only the colour strings produced by ColorForLevel() instead.
        private IBrush _infoBrush;
        private IBrush _warningBrush;
        private IBrush _errorBrush;
        private IBrush _defaultBrush;

        // ─── testable pure function: maps level → hex colour string ───────────────
        public static string ColorForLevel(LogLevel level)
        {
            if (level == null)         return "#A69F92";
            if (level == LogLevel.Info)  return "#ECE5D8";
            if (level == LogLevel.Warn)  return "#F9B959";
            if (level >= LogLevel.Error) return "#E74C3C";
            return "#A69F92"; // Debug / Trace / Off
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lazily allocate brushes on first use (must be on UI thread at runtime).
            _infoBrush    = _infoBrush    ?? new SolidColorBrush(Color.Parse("#ECE5D8"));
            _warningBrush = _warningBrush ?? new SolidColorBrush(Color.Parse("#F9B959"));
            _errorBrush   = _errorBrush   ?? new SolidColorBrush(Color.Parse("#E74C3C"));
            _defaultBrush = _defaultBrush ?? new SolidColorBrush(Color.Parse("#A69F92"));

            if (value is LogLevel level)
            {
                if (level == LogLevel.Info)  return _infoBrush;
                if (level == LogLevel.Warn)  return _warningBrush;
                if (level >= LogLevel.Error) return _errorBrush;
            }
            return _defaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("LogLevelToBrushConverter is one-way only.");
        }
    }
}
