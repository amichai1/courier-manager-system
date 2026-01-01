using System;
using System.Globalization;
using System.Windows.Data;

namespace PL.Converters
{
    /// <summary>
    /// Converts boolean to availability text (Available/Unavailable)
    /// </summary>
    public class BoolToAvailabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "Available" : "Unavailable";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
