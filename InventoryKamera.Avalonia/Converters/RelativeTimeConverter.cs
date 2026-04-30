using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InventoryKamera.Avalonia.Converters
{
    /// <summary>
    /// Converts a <see cref="DateTime"/> to a local-time string in "HH:mm:ss" format
    /// for display in the scan console.
    /// </summary>
    public class RelativeTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("RelativeTimeConverter is one-way only.");
        }
    }
}
