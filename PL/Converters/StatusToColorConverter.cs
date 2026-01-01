using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PL.Converters
{
    /// <summary>
    /// Converts order/courier status to appropriate color
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string paramStr = parameter?.ToString() ?? "";

            // Handle courier status (bool IsActive)
            if (paramStr == "CourierStatus" && value is bool isActive)
            {
                return isActive 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // Green - Available
                    : new SolidColorBrush(Color.FromRgb(255, 193, 7));  // Yellow - On Delivery/Unavailable
            }

            // Handle order status
            string status = value?.ToString() ?? "";

            return status switch
            {
                "Open" or "New" => new SolidColorBrush(Color.FromRgb(52, 152, 219)),     // Blue
                "InProgress" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),        // Yellow
                "Delivered" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),         // Green
                "OrderRefused" or "Canceled" => new SolidColorBrush(Color.FromRgb(231, 76, 60)), // Red
                _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))                   // Gray
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts ScheduleStatus enum to a background color.
    /// </summary>
    public class ScheduleStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BO.ScheduleStatus status)
            {
                return status switch
                {
                    BO.ScheduleStatus.OnTime => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                    BO.ScheduleStatus.InRisk => new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                    BO.ScheduleStatus.Late => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
                };
            }

            return new SolidColorBrush(Color.FromRgb(149, 165, 166));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Returns true if value is greater than zero.
    /// Used for DataTrigger conditions.
    /// </summary>
    public class GreaterThanZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue > 0;
            if (value is double doubleValue)
                return doubleValue > 0;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to visibility with inverse option.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            bool invert = parameter?.ToString() == "Invert";

            if (invert)
                boolValue = !boolValue;

            return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
