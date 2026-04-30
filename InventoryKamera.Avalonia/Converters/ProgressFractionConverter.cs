using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InventoryKamera.Avalonia.Converters
{
    /// <summary>
    /// Multi-value converter for ProgressBar <c>Value</c>.
    /// Inputs: [scanned (int/double), total (int/double)].
    /// Output: double in range [0, 1] (fraction complete). Returns 0 if total &lt;= 0.
    /// </summary>
    public class ProgressFractionConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2)
                return 0.0;

            double scanned = ToDouble(values[0]);
            double total   = ToDouble(values[1]);

            if (total <= 0)
                return 0.0;

            double fraction = scanned / total;
            return Math.Max(0.0, Math.Min(1.0, fraction));
        }

        private static double ToDouble(object value)
        {
            if (value is int    i) return i;
            if (value is double d) return d;
            if (value is float  f) return f;
            if (value is long   l) return l;
            return 0.0;
        }
    }
}
