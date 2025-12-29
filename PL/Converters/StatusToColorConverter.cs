using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PL.Converters
{
    /// <summary>
    /// Converts OrderStatus enum to a SolidColorBrush for UI visualization.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BO.OrderStatus status)
            {
                return status switch
                {
                    BO.OrderStatus.Open => new SolidColorBrush(Color.FromRgb(127, 140, 141)),
                    BO.OrderStatus.Confirmed => new SolidColorBrush(Color.FromRgb(52, 152, 219)),
                    BO.OrderStatus.AssociatedToCourier => new SolidColorBrush(Color.FromRgb(155, 89, 182)),
                    BO.OrderStatus.InProgress => new SolidColorBrush(Color.FromRgb(241, 196, 15)),
                    BO.OrderStatus.Delivered => new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                    BO.OrderStatus.OrderRefused => new SolidColorBrush(Color.FromRgb(231, 76, 60)),
                    BO.OrderStatus.Canceled => new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                    _ => new SolidColorBrush(Color.FromRgb(127, 140, 141))
                };
            }

            return new SolidColorBrush(Color.FromRgb(127, 140, 141));
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
}
