using System;
using System.Globalization;
using System.Windows.Data;

namespace PL.Converters
{
    /// <summary>
    /// Converts delivery/order type to icon representation
    /// </summary>
    public class DeliveryTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string typeString = value?.ToString() ?? "";

            return typeString switch
            {
                "Car" or "Retail" => "🚗",
                "Motorcycle" or "Express" => "🏍️",
                "Van" or "Wholesale" => "🚐",
                "Bicycle" => "🚲",
                "OnFoot" => "🚶",
                _ => "📦"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
