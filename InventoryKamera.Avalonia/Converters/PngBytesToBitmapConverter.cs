using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace InventoryKamera.Avalonia.Converters
{
    /// <summary>
    /// Converts a <c>byte[]</c> containing PNG-encoded image data to an Avalonia
    /// <see cref="Bitmap"/> suitable for use as an <c>Image.Source</c>.
    ///
    /// Stateless: re-decodes on every call. The OCR preview throttle is 100 ms
    /// so re-decoding ~10 frames/sec is acceptable, and this avoids any cross-talk
    /// when the same converter type is referenced from multiple bindings.
    /// </summary>
    public class PngBytesToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0)
                return null;

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PngBytesToBitmapConverter is one-way only.");
        }
    }
}
