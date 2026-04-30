using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InventoryKamera.Avalonia.Converters
{
    /// <summary>
    /// Converts a <c>bool?</c> validation state to a Unicode icon string.
    /// <c>true</c>  → "✓"
    /// <c>false</c> → "✗"
    /// <c>null</c>  → ""
    /// </summary>
    public class ValidationIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "✓" : "✗";
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ValidationIconConverter is one-way only.");
        }
    }
}
