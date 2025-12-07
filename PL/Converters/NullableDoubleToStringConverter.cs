using System;
using System.Globalization;
using System.Windows.Data;

namespace PL.Converters
{
    // Converts between string (TextBox.Text) and double? (Configuration.MaxDeliveryDistance).
    // Empty string => null. Invalid numeric format => throws FormatException (Binding shows validation).
    public class NullableDoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            if (value is double d) return d.ToString(culture);
            return value.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (value as string)?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(s)) return null;
            if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var d))
                return (double?)d;

            // Throwing leads to Validation error which we handle in UI (user-friendly).
            throw new FormatException("Please enter a valid number (for example: 12.5) or leave blank for unlimited.");
        }
    }
}
